using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TransactionApi.Data.Models;
using System.Text.Json;

namespace TransactionApi.Data;

public static class TestDataGenerator
{
    public static async Task SeedTestDataAsync(AppDbContext context, ILogger logger)
    {
        try
        {
            logger.LogInformation("Starting to seed test data...");
            
            // Check if we already have data
            if (await context.Accounts.AnyAsync() || await context.Categories.AnyAsync() || await context.Transactions.AnyAsync())
            {
                logger.LogInformation("Test data already exists. Skipping seeding.");
                return;
            }
            
            // Create accounts
            var accounts = new List<Account>
            {
                new Account { Id = Guid.NewGuid(), Name = "Checking Account", AccountNumber = "CH-1234567" },
                new Account { Id = Guid.NewGuid(), Name = "Savings Account", AccountNumber = "SA-7654321" },
                new Account { Id = Guid.NewGuid(), Name = "Investment Account", AccountNumber = "IN-9876543" }
            };
            
            await context.Accounts.AddRangeAsync(accounts);
            await context.SaveChangesAsync();
            logger.LogInformation("Added {Count} accounts", accounts.Count);
            
            // Create categories
            var categories = new List<Category>
            {
                new Category { Id = Guid.NewGuid(), Name = "Groceries" },
                new Category { Id = Guid.NewGuid(), Name = "Utilities" },
                new Category { Id = Guid.NewGuid(), Name = "Entertainment" },
                new Category { Id = Guid.NewGuid(), Name = "Transportation" },
                new Category { Id = Guid.NewGuid(), Name = "Dining" },
                new Category { Id = Guid.NewGuid(), Name = "Healthcare" },
                new Category { Id = Guid.NewGuid(), Name = "Income" }
            };
            
            await context.Categories.AddRangeAsync(categories);
            await context.SaveChangesAsync();
            logger.LogInformation("Added {Count} categories", categories.Count);
            
            // Create transactions
            var random = new Random();
            var transactions = new List<Transaction>();
            var paymentMethods = new[] { "credit_card", "debit_card", "bank_transfer", "cash" };
            var locations = new[] { "Online", "Store", "ATM", "Mobile App" };
            
            // Get the income category for deposits
            var incomeCategory = categories.First(c => c.Name == "Income");
            
            // Generate transactions for the past 90 days
            var startDate = DateTime.UtcNow.AddDays(-90);
            var endDate = DateTime.UtcNow;
            
            foreach (var account in accounts)
            {
                // Generate 5-15 transactions per day for each account
                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    var transactionsPerDay = random.Next(5, 16);
                    
                    for (int i = 0; i < transactionsPerDay; i++)
                    {
                        // Randomly decide if this is a deposit (income) or expense
                        bool isDeposit = random.Next(10) < 2; // 20% chance of being a deposit
                        
                        var category = isDeposit 
                            ? incomeCategory 
                            : categories.Where(c => c.Name != "Income").ElementAt(random.Next(categories.Count - 1));
                        
                        // Generate random time within the day
                        var hours = random.Next(24);
                        var minutes = random.Next(60);
                        var seconds = random.Next(60);
                        var transactionTime = date.AddHours(hours).AddMinutes(minutes).AddSeconds(seconds);
                        
                        // Generate amount (deposits are positive, expenses are negative)
                        var amount = isDeposit 
                            ? random.Next(500, 5001) 
                            : -random.Next(10, 501);
                        
                        // Create metadata
                        var metadata = JsonDocument.Parse(JsonSerializer.Serialize(new
                        {
                            payment_method = paymentMethods[random.Next(paymentMethods.Length)],
                            location = locations[random.Next(locations.Length)],
                            notes = isDeposit ? "Monthly income" : $"Purchase at {DateTime.Now:yyyy-MM-dd}"
                        }));
                        
                        transactions.Add(new Transaction
                        {
                            Id = Guid.NewGuid(),
                            AccountId = account.Id,
                            CategoryId = category.Id,
                            CreatedAt = transactionTime,
                            Amount = amount,
                            Description = isDeposit 
                                ? "Deposit" 
                                : $"Payment for {category.Name}",
                            Metadata = metadata
                        });
                    }
                }
            }
            
            // Add transactions in batches to avoid memory issues
            const int batchSize = 1000;
            for (int i = 0; i < transactions.Count; i += batchSize)
            {
                var batch = transactions.Skip(i).Take(batchSize).ToList();
                await context.Transactions.AddRangeAsync(batch);
                await context.SaveChangesAsync();
                logger.LogInformation("Added batch of {Count} transactions", batch.Count);
            }
            
            logger.LogInformation("Successfully seeded {Count} transactions", transactions.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding test data.");
            throw;
        }
    }
}