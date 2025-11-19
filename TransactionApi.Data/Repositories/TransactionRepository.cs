using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using System.Text.Json;
using TransactionApi.Data.Models;

namespace TransactionApi.Data.Repositories;

public class TransactionRepository : Repository<Transaction>, ITransactionRepository
{
    public TransactionRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Transaction>> GetTransactionsAsync(
        DateTime? from = null, 
        DateTime? to = null,
        Guid? accountId = null,
        Guid? categoryId = null,
        int? limit = null)
    {
        IQueryable<Transaction> query = _dbSet;

        // Apply filters
        if (from.HasValue)
            query = query.Where(t => t.CreatedAt >= from.Value);
            
        if (to.HasValue)
            query = query.Where(t => t.CreatedAt <= to.Value);
            
        if (accountId.HasValue)
            query = query.Where(t => t.AccountId == accountId.Value);
            
        if (categoryId.HasValue)
            query = query.Where(t => t.CategoryId == categoryId.Value);

        // Order by most recent first
        query = query.OrderByDescending(t => t.CreatedAt);
        
        // Apply limit if specified
        if (limit.HasValue)
            query = query.Take(limit.Value);

        return await query.ToListAsync();
    }

    public async Task<IEnumerable<object>> GetDailySummaryAsync(
        DateTime? from = null,
        DateTime? to = null,
        Guid? accountId = null)
    {
        var conn = (NpgsqlConnection)_context.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync();

        var sql = @"
                SELECT 
                    bucket::date as date,
                    account_id as account_id,
                    transaction_count,
                    total_amount
                FROM daily_transaction_summary
                WHERE 1=1";

        var parameters = new List<NpgsqlParameter>();

        if (from.HasValue)
        {
            sql += " AND bucket >= @from";
            parameters.Add(new NpgsqlParameter("from", NpgsqlDbType.TimestampTz) { Value = from.Value });
        }

        if (to.HasValue)
        {
            sql += " AND bucket <= @to";
            parameters.Add(new NpgsqlParameter("to", NpgsqlDbType.TimestampTz) { Value = to.Value });
        }

        if (accountId.HasValue)
        {
            sql += " AND account_id = @accountId";
            parameters.Add(new NpgsqlParameter("accountId", NpgsqlDbType.Uuid) { Value = accountId.Value });
        }

        sql += " ORDER BY bucket DESC, account_id";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddRange(parameters.ToArray());

        using var reader = await cmd.ExecuteReaderAsync();
        var results = new List<object>();

        while (await reader.ReadAsync())
        {
            results.Add(new
            {
                Date = reader.GetDateTime(0),
                AccountId = reader.GetGuid(1),
                TransactionCount = reader.GetInt64(2),
                TotalAmount = reader.GetDecimal(3)
            });
        }

        return results;
    }

    public async Task<IEnumerable<object>> GetCategorySummaryAsync(
        DateTime? from = null,
        DateTime? to = null,
        Guid? accountId = null)
    {
        var query = _dbSet.AsQueryable();
        
        if (from.HasValue)
            query = query.Where(t => t.CreatedAt >= from.Value);
            
        if (to.HasValue)
            query = query.Where(t => t.CreatedAt <= to.Value);
            
        if (accountId.HasValue)
            query = query.Where(t => t.AccountId == accountId.Value);

        var result = await query
            .GroupBy(t => t.CategoryId)
            .Select(g => new
            {
                CategoryId = g.Key,
                TransactionCount = g.Count(),
                TotalAmount = g.Sum(t => t.Amount)
            })
            .OrderByDescending(r => Math.Abs((double)r.TotalAmount))
            .ToListAsync();

        return result;
    }
}