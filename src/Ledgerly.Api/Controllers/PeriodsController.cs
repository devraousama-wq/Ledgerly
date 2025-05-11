using Ledgerly.Application.Periods;
using Microsoft.AspNetCore.Mvc;

namespace Ledgerly.Api.Controllers;

[ApiController]
[Route("api/organizations/{organizationId:guid}/periods")]
public sealed class PeriodsController : ControllerBase
{
    private readonly PeriodCloseService _periodCloseService;

    public PeriodsController(PeriodCloseService periodCloseService)
    {
        _periodCloseService = periodCloseService;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var result = await _periodCloseService.ListAsync(organizationId, cancellationToken);

        return Ok(result.Value);
    }

    [HttpGet("{year:int}/{month:int}")]
    public async Task<IActionResult> Get(
        Guid organizationId,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var result = await _periodCloseService.GetAsync(organizationId, year, month, cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpPost("{year:int}/{month:int}/close")]
    public async Task<IActionResult> Close(
        Guid organizationId,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var result = await _periodCloseService.CloseAsync(organizationId, year, month, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpPost("{year:int}/{month:int}/reopen")]
    public async Task<IActionResult> Reopen(
        Guid organizationId,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var result = await _periodCloseService.ReopenAsync(organizationId, year, month, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == "Fiscal period not found.")
            {
                return NotFound(new { error = result.Error });
            }

            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpGet("audit")]
    public async Task<IActionResult> ListAudit(
        Guid organizationId,
        [FromQuery] int? year = null,
        [FromQuery] int? month = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _periodCloseService.ListAuditAsync(
            organizationId,
            year,
            month,
            cancellationToken);

        return Ok(result.Value);
    }
}
