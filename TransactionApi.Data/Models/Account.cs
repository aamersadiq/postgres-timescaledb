namespace TransactionApi.Data.Models;

public class Account
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public ICollection<Transaction>? Transactions { get; set; }
}