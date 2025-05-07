using Ledgerly.Application.TaxCodes;
using Microsoft.AspNetCore.Mvc;

namespace Ledgerly.Api.Controllers;

[ApiController]
[Route("api/organizations/{organizationId:guid}/tax-codes")]
public sealed class TaxCodesController : ControllerBase
{
    private readonly TaxCodeService _taxCodeService;
    private readonly TaxLiabilityReportService _taxLiabilityReportService;

    public TaxCodesController(
        TaxCodeService taxCodeService,
        TaxLiabilityReportService taxLiabilityReportService)
    {
        _taxCodeService = taxCodeService;
        _taxLiabilityReportService = taxLiabilityReportService;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        Guid organizationId,
        [FromQuery] bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _taxCodeService.ListByOrganizationAsync(
            organizationId,
            includeArchived,
            cancellationToken);

        return Ok(result.Value);
    }

    [HttpGet("{taxCodeId:guid}")]
    public async Task<IActionResult> GetById(
        Guid organizationId,
        Guid taxCodeId,
        CancellationToken cancellationToken = default)
    {
        var result = await _taxCodeService.GetByIdAsync(organizationId, taxCodeId, cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        Guid organizationId,
        [FromBody] CreateTaxCodeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.OrganizationId != organizationId)
        {
            return BadRequest(new { error = "OrganizationId in route and body must match." });
        }

        var result = await _taxCodeService.CreateAsync(request, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }

        return CreatedAtAction(
            nameof(GetById),
            new { organizationId, taxCodeId = result.Value!.Id },
            result.Value);
    }

    [HttpPut("{taxCodeId:guid}")]
    public async Task<IActionResult> Update(
        Guid organizationId,
        Guid taxCodeId,
        [FromBody] UpdateTaxCodeRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _taxCodeService.UpdateAsync(
            organizationId,
            taxCodeId,
            request,
            cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == "Tax code not found.")
            {
                return NotFound(new { error = result.Error });
            }

            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpPost("{taxCodeId:guid}/archive")]
    public async Task<IActionResult> Archive(
        Guid organizationId,
        Guid taxCodeId,
        CancellationToken cancellationToken = default)
    {
        var result = await _taxCodeService.ArchiveAsync(organizationId, taxCodeId, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == "Tax code not found.")
            {
                return NotFound(new { error = result.Error });
            }

            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpGet("report")]
    public async Task<IActionResult> Report(
        Guid organizationId,
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        var result = await _taxLiabilityReportService.GenerateAsync(
            organizationId,
            new TaxLiabilityReportRequest(startDate, endDate),
            cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }
}
