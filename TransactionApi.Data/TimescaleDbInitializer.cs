using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace TransactionApi.Data;

public static class TimescaleDbInitializer
{
    public static async Task InitializeTimescaleDbAsync(AppDbContext context, ILogger logger)
    {
        try
        {
            // Delete the database if it exists and recreate it
            logger.LogInformation("Deleting database if it exists...");
            await context.Database.EnsureDeletedAsync();
            
            logger.LogInformation("Creating database...");
            await context.Database.EnsureCreatedAsync();
            logger.LogInformation("Database created successfully.");
            
            // Execute the TimescaleDB setup commands
            var conn = (NpgsqlConnection)context.Database.GetDbConnection();
            
            // Check if the connection is already open
            if (conn.State != System.Data.ConnectionState.Open)
            {
                await conn.OpenAsync();
            }
            
            // Check if TimescaleDB extension is installed
            logger.LogInformation("Checking if TimescaleDB extension is installed...");
            using (var cmd = new NpgsqlCommand("SELECT extname FROM pg_extension WHERE extname = 'timescaledb';", conn))
            {
                var result = await cmd.ExecuteScalarAsync();
                if (result == null)
                {
                    logger.LogInformation("Installing TimescaleDB extension...");
                    using var createExtCmd = new NpgsqlCommand("CREATE EXTENSION IF NOT EXISTS timescaledb CASCADE;", conn);
                    await createExtCmd.ExecuteNonQueryAsync();
                }
                else
                {
                    logger.LogInformation("TimescaleDB extension is already installed.");
                }
            }
            
            // Convert transactions table to a hypertable
            logger.LogInformation("Converting Transactions table to a hypertable...");
            using (var cmd = new NpgsqlCommand(
                "SELECT hypertable_name FROM timescaledb_information.hypertables WHERE hypertable_name = 'Transactions';", conn))
            {
                var result = await cmd.ExecuteScalarAsync();
                if (result == null)
                {
                    logger.LogInformation("Creating hypertable for Transactions...");
                    using var createHypertableCmd = new NpgsqlCommand(
                        "SELECT create_hypertable('\"Transactions\"', 'CreatedAt', chunk_time_interval => INTERVAL '1 day');", conn);
                    await createHypertableCmd.ExecuteNonQueryAsync();
                    
                    // Create indexes for better query performance
                    logger.LogInformation("Creating indexes for Transactions...");
                    using var createIndexCmd1 = new NpgsqlCommand(
                        "CREATE INDEX IF NOT EXISTS idx_transactions_account_id ON \"Transactions\" (\"AccountId\");", conn);
                    await createIndexCmd1.ExecuteNonQueryAsync();
                    
                    using var createIndexCmd2 = new NpgsqlCommand(
                        "CREATE INDEX IF NOT EXISTS idx_transactions_category_id ON \"Transactions\" (\"CategoryId\");", conn);
                    await createIndexCmd2.ExecuteNonQueryAsync();
                    
                    using var createIndexCmd3 = new NpgsqlCommand(
                        "CREATE INDEX IF NOT EXISTS idx_transactions_account_created ON \"Transactions\" (\"AccountId\", \"CreatedAt\" DESC);", conn);
                    await createIndexCmd3.ExecuteNonQueryAsync();
                }
                else
                {
                    logger.LogInformation("Transactions table is already a hypertable.");
                }
            }
            
            // Create a continuous aggregate for daily transaction summaries
            logger.LogInformation("Setting up continuous aggregate for daily transaction summaries...");
            using (var cmd = new NpgsqlCommand(
                "SELECT view_name FROM timescaledb_information.continuous_aggregates WHERE view_name = 'daily_transaction_summary';", conn))
            {
                var result = await cmd.ExecuteScalarAsync();
                if (result == null)
                {
                    logger.LogInformation("Creating continuous aggregate view...");
                    using var createViewCmd = new NpgsqlCommand(@"
                        CREATE MATERIALIZED VIEW IF NOT EXISTS daily_transaction_summary
                        WITH (timescaledb.continuous) AS
                        SELECT
                            time_bucket('1 day', ""CreatedAt"") AS bucket,
                            ""AccountId"" AS account_id,
                            count(*) AS transaction_count,
                            sum(""Amount"") AS total_amount
                        FROM ""Transactions""
                        GROUP BY bucket, ""AccountId"";", conn);
                    await createViewCmd.ExecuteNonQueryAsync();
                    
                    // Set refresh policy (refresh every hour)
                    logger.LogInformation("Setting refresh policy for continuous aggregate...");
                    using var refreshPolicyCmd = new NpgsqlCommand(@"
                        SELECT add_continuous_aggregate_policy('daily_transaction_summary',
                            start_offset => INTERVAL '3 days',
                            end_offset => INTERVAL '1 hour',
                            schedule_interval => INTERVAL '1 hour');", conn);
                    await refreshPolicyCmd.ExecuteNonQueryAsync();
                }
                else
                {
                    logger.LogInformation("Continuous aggregate view already exists.");
                }
            }
            
            logger.LogInformation("TimescaleDB initialization completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing TimescaleDB.");
            throw;
        }
    }
}