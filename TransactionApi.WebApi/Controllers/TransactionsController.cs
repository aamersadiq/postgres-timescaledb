using Microsoft.AspNetCore.Mvc;
using TransactionApi.Data.Models;
using TransactionApi.Data.Repositories;

namespace TransactionApi.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(ITransactionRepository transactionRepository, ILogger<TransactionsController> logger)
    {
        _transactionRepository = transactionRepository;
        _logger = logger;
    }

    // GET: api/transactions
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Transaction>>> GetTransactions(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] Guid? accountId = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] int? limit = 100)
    {
        try
        {
            var transactions = await _transactionRepository.GetTransactionsAsync(from, to, accountId, categoryId, limit);
            return Ok(transactions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transactions");
            return StatusCode(500, "An error occurred while retrieving transactions");
        }
    }

    // GET: api/transactions/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Transaction>> GetTransaction(Guid id)
    {
        try
        {
            var transaction = await _transactionRepository.GetByIdAsync(id);

            if (transaction == null)
            {
                return NotFound();
            }

            return Ok(transaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transaction with ID {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the transaction");
        }
    }

    // POST: api/transactions
    [HttpPost]
    public async Task<ActionResult<Transaction>> CreateTransaction(Transaction transaction)
    {
        try
        {
            if (transaction.Id == Guid.Empty)
            {
                transaction.Id = Guid.NewGuid();
            }
            
            if (transaction.CreatedAt == default)
            {
                transaction.CreatedAt = DateTime.UtcNow;
            }

            var createdTransaction = await _transactionRepository.AddAsync(transaction);
            return CreatedAtAction(nameof(GetTransaction), new { id = createdTransaction.Id }, createdTransaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating transaction");
            return StatusCode(500, "An error occurred while creating the transaction");
        }
    }

    // PUT: api/transactions/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTransaction(Guid id, Transaction transaction)
    {
        try
        {
            if (id != transaction.Id)
            {
                return BadRequest("Transaction ID mismatch");
            }

            var existingTransaction = await _transactionRepository.GetByIdAsync(id);
            if (existingTransaction == null)
            {
                return NotFound();
            }

            await _transactionRepository.UpdateAsync(transaction);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating transaction with ID {Id}", id);
            return StatusCode(500, "An error occurred while updating the transaction");
        }
    }

    // DELETE: api/transactions/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTransaction(Guid id)
    {
        try
        {
            var transaction = await _transactionRepository.GetByIdAsync(id);
            if (transaction == null)
            {
                return NotFound();
            }

            await _transactionRepository.DeleteAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting transaction with ID {Id}", id);
            return StatusCode(500, "An error occurred while deleting the transaction");
        }
    }
}