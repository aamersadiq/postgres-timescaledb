using System.Text.Json;

namespace TransactionApi.Data.Models;

public class Transaction
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal Amount { get; set; }
    public Guid AccountId { get; set; }
    public Guid CategoryId { get; set; }
    public string? Description { get; set; }
    public JsonDocument? Metadata { get; set; }
   
    public Account? Account { get; set; }
    public Category? Category { get; set; }
}