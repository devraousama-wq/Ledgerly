using Ledgerly.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace Ledgerly.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class HealthController : ControllerBase
{
    private readonly LedgerlyDbContext _dbContext;

    public HealthController(LedgerlyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
        return Ok(new
        {
            status = canConnect ? "healthy" : "degraded",
            service = "ledgerly-api",
            database = canConnect ? "connected" : "unavailable",
            timestamp = DateTimeOffset.UtcNow
        });
    }
}
