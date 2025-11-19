using Microsoft.AspNetCore.Mvc;
using TransactionApi.Data.Repositories;

namespace TransactionApi.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(ITransactionRepository transactionRepository, ILogger<AnalyticsController> logger)
    {
        _transactionRepository = transactionRepository;
        _logger = logger;
    }

    // GET: api/analytics/daily-summary
    [HttpGet("daily-summary")]
    public async Task<ActionResult<IEnumerable<object>>> GetDailySummary(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] Guid? accountId = null,
        [FromQuery] bool today = false)
    {
        try
        {
            // If today parameter is true, set from to start of today and to to current time
            if (today)
            {
                from = DateTime.Today;
                to = DateTime.Now;
            }
            
            var summary = await _transactionRepository.GetDailySummaryAsync(from, to, accountId);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving daily summary");
            return StatusCode(500, "An error occurred while retrieving the daily summary");
        }
    }

    // GET: api/analytics/category-summary
    [HttpGet("category-summary")]
    public async Task<ActionResult<IEnumerable<object>>> GetCategorySummary(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] Guid? accountId = null)
    {
        try
        {
            var summary = await _transactionRepository.GetCategorySummaryAsync(from, to, accountId);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving category summary");
            return StatusCode(500, "An error occurred while retrieving the category summary");
        }
    }

    // GET: api/analytics/time-comparison
    [HttpGet("time-comparison")]
    public async Task<ActionResult<object>> GetTimeComparison(
        [FromQuery] DateTime? currentPeriodStart,
        [FromQuery] DateTime? currentPeriodEnd,
        [FromQuery] DateTime? previousPeriodStart,
        [FromQuery] DateTime? previousPeriodEnd,
        [FromQuery] Guid? accountId = null)
    {
        try
        {
            if (!currentPeriodStart.HasValue || !currentPeriodEnd.HasValue || 
                !previousPeriodStart.HasValue || !previousPeriodEnd.HasValue)
            {
                return BadRequest("All period dates must be specified");
            }

            var currentPeriodData = await _transactionRepository.GetTransactionsAsync(
                currentPeriodStart, currentPeriodEnd, accountId);
            
            var previousPeriodData = await _transactionRepository.GetTransactionsAsync(
                previousPeriodStart, previousPeriodEnd, accountId);

            var currentTotal = currentPeriodData.Sum(t => t.Amount);
            var previousTotal = previousPeriodData.Sum(t => t.Amount);
            
            var percentageChange = previousTotal != 0 
                ? ((currentTotal - previousTotal) / Math.Abs(previousTotal)) * 100 
                : 0;

            return Ok(new
            {
                CurrentPeriod = new
                {
                    Start = currentPeriodStart,
                    End = currentPeriodEnd,
                    TransactionCount = currentPeriodData.Count(),
                    Total = currentTotal
                },
                PreviousPeriod = new
                {
                    Start = previousPeriodStart,
                    End = previousPeriodEnd,
                    TransactionCount = previousPeriodData.Count(),
                    Total = previousTotal
                },
                Comparison = new
                {
                    AbsoluteDifference = currentTotal - previousTotal,
                    PercentageChange = percentageChange
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing time comparison");
            return StatusCode(500, "An error occurred while performing the time comparison");
        }
    }
}