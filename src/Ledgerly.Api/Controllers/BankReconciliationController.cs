using Ledgerly.Application.Reconciliation;
using Microsoft.AspNetCore.Mvc;

namespace Ledgerly.Api.Controllers;

[ApiController]
[Route("api/organizations/{organizationId:guid}/bank-reconciliation")]
public sealed class BankReconciliationController : ControllerBase
{
    private readonly BankReconciliationService _bankReconciliationService;
    private readonly ReconciliationReportService _reconciliationReportService;

    public BankReconciliationController(
        BankReconciliationService bankReconciliationService,
        ReconciliationReportService reconciliationReportService)
    {
        _bankReconciliationService = bankReconciliationService;
        _reconciliationReportService = reconciliationReportService;
    }

    [HttpGet]
    public async Task<IActionResult> List(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var result = await _bankReconciliationService.ListByOrganizationAsync(organizationId, cancellationToken);
        return Ok(result.Value);
    }

    [HttpGet("{statementId:guid}")]
    public async Task<IActionResult> GetById(
        Guid organizationId,
        Guid statementId,
        CancellationToken cancellationToken = default)
    {
        var result = await _bankReconciliationService.GetByIdAsync(organizationId, statementId, cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        Guid organizationId,
        [FromBody] CreateBankStatementRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.OrganizationId != organizationId)
        {
            return BadRequest(new { error = "OrganizationId in route and body must match." });
        }

        var result = await _bankReconciliationService.CreateAsync(request, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }

        return CreatedAtAction(
            nameof(GetById),
            new { organizationId, statementId = result.Value!.Id },
            result.Value);
    }

    [HttpPost("{statementId:guid}/lines")]
    public async Task<IActionResult> AddLine(
        Guid organizationId,
        Guid statementId,
        [FromBody] AddBankStatementLineRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _bankReconciliationService.AddLineAsync(
            organizationId,
            statementId,
            request,
            cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpPost("{statementId:guid}/import/csv")]
    public async Task<IActionResult> ImportCsv(
        Guid organizationId,
        Guid statementId,
        [FromBody] ImportCsvBankStatementRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _bankReconciliationService.ImportCsvAsync(
            organizationId,
            statementId,
            request,
            cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpPost("{statementId:guid}/import/ofx")]
    public async Task<IActionResult> ImportOfx(
        Guid organizationId,
        Guid statementId,
        [FromBody] ImportOfxBankStatementRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _bankReconciliationService.ImportOfxAsync(
            organizationId,
            statementId,
            request,
            cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpPost("{statementId:guid}/auto-match")]
    public async Task<IActionResult> AutoMatch(
        Guid organizationId,
        Guid statementId,
        CancellationToken cancellationToken = default)
    {
        var result = await _bankReconciliationService.AutoMatchAsync(
            organizationId,
            statementId,
            cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpPost("{statementId:guid}/lines/{lineId:guid}/match")]
    public async Task<IActionResult> MatchLine(
        Guid organizationId,
        Guid statementId,
        Guid lineId,
        [FromBody] MatchBankStatementLineRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _bankReconciliationService.MatchLineAsync(
            organizationId,
            statementId,
            lineId,
            request,
            cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpPost("{statementId:guid}/lines/{lineId:guid}/create-entry")]
    public async Task<IActionResult> CreateEntryFromLine(
        Guid organizationId,
        Guid statementId,
        Guid lineId,
        [FromBody] CreateBankEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _bankReconciliationService.CreateEntryFromLineAsync(
            organizationId,
            statementId,
            lineId,
            request,
            cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpPost("{statementId:guid}/reconcile")]
    public async Task<IActionResult> Reconcile(
        Guid organizationId,
        Guid statementId,
        CancellationToken cancellationToken = default)
    {
        var result = await _bankReconciliationService.ReconcileAsync(
            organizationId,
            statementId,
            cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpGet("report")]
    public async Task<IActionResult> Report(
        Guid organizationId,
        [FromQuery] Guid bankAccountId,
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        var result = await _reconciliationReportService.GenerateAsync(
            organizationId,
            new ReconciliationReportRequest(bankAccountId, startDate, endDate),
            cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }
}
