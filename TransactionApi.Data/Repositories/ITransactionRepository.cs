using TransactionApi.Data.Models;

namespace TransactionApi.Data.Repositories;

public interface ITransactionRepository : IRepository<Transaction>
{
    Task<IEnumerable<Transaction>> GetTransactionsAsync(
        DateTime? from = null,
        DateTime? to = null,
        Guid? accountId = null,
        Guid? categoryId = null,
        int? limit = null);
    
    Task<IEnumerable<object>> GetDailySummaryAsync(
        DateTime? from = null,
        DateTime? to = null,
        Guid? accountId = null);
    
    Task<IEnumerable<object>> GetCategorySummaryAsync(
        DateTime? from = null,
        DateTime? to = null,
        Guid? accountId = null);
}