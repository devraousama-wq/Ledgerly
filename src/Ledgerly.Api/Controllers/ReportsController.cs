using Ledgerly.Application.Reporting;
using Ledgerly.Application.Reporting.Export;
using Microsoft.AspNetCore.Mvc;

namespace Ledgerly.Api.Controllers;

[ApiController]
[Route("api/organizations/{organizationId:guid}/reports")]
public sealed class ReportsController : ControllerBase
{
    private readonly TrialBalanceReportService _trialBalanceReportService;
    private readonly ProfitAndLossReportService _profitAndLossReportService;
    private readonly BalanceSheetReportService _balanceSheetReportService;
    private readonly AgedReceivablesReportService _agedReceivablesReportService;
    private readonly ReportPdfExporter _reportPdfExporter;
    private readonly ReportExcelExporter _reportExcelExporter;

    public ReportsController(
        TrialBalanceReportService trialBalanceReportService,
        ProfitAndLossReportService profitAndLossReportService,
        BalanceSheetReportService balanceSheetReportService,
        AgedReceivablesReportService agedReceivablesReportService,
        ReportPdfExporter reportPdfExporter,
        ReportExcelExporter reportExcelExporter)
    {
        _trialBalanceReportService = trialBalanceReportService;
        _profitAndLossReportService = profitAndLossReportService;
        _balanceSheetReportService = balanceSheetReportService;
        _agedReceivablesReportService = agedReceivablesReportService;
        _reportPdfExporter = reportPdfExporter;
        _reportExcelExporter = reportExcelExporter;
    }

    [HttpGet("trial-balance")]
    public async Task<IActionResult> TrialBalance(
        Guid organizationId,
        [FromQuery] DateOnly asOfDate,
        [FromQuery] string format = "json",
        CancellationToken cancellationToken = default)
    {
        var result = await _trialBalanceReportService.GenerateAsync(
            organizationId,
            new TrialBalanceReportRequest(asOfDate),
            cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }

        return ExportResult(
            format,
            result.Value!,
            "trial-balance",
            _reportPdfExporter.ExportTrialBalance,
            _reportExcelExporter.ExportTrialBalance,
            report => Ok(report));
    }

    [HttpGet("profit-and-loss")]
    public async Task<IActionResult> ProfitAndLoss(
        Guid organizationId,
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        [FromQuery] string format = "json",
        CancellationToken cancellationToken = default)
    {
        var result = await _profitAndLossReportService.GenerateAsync(
            organizationId,
            new ProfitAndLossReportRequest(startDate, endDate),
            cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }

        return ExportResult(
            format,
            result.Value!,
            "profit-and-loss",
            _reportPdfExporter.ExportProfitAndLoss,
            _reportExcelExporter.ExportProfitAndLoss,
            report => Ok(report));
    }

    [HttpGet("balance-sheet")]
    public async Task<IActionResult> BalanceSheet(
        Guid organizationId,
        [FromQuery] DateOnly asOfDate,
        [FromQuery] string format = "json",
        CancellationToken cancellationToken = default)
    {
        var result = await _balanceSheetReportService.GenerateAsync(
            organizationId,
            new BalanceSheetReportRequest(asOfDate),
            cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }

        return ExportResult(
            format,
            result.Value!,
            "balance-sheet",
            _reportPdfExporter.ExportBalanceSheet,
            _reportExcelExporter.ExportBalanceSheet,
            report => Ok(report));
    }

    [HttpGet("aged-receivables")]
    public async Task<IActionResult> AgedReceivables(
        Guid organizationId,
        [FromQuery] DateOnly asOfDate,
        [FromQuery] string format = "json",
        CancellationToken cancellationToken = default)
    {
        var result = await _agedReceivablesReportService.GenerateAsync(
            organizationId,
            new AgedReceivablesReportRequest(asOfDate),
            cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }

        return ExportResult(
            format,
            result.Value!,
            "aged-receivables",
            _reportPdfExporter.ExportAgedReceivables,
            _reportExcelExporter.ExportAgedReceivables,
            report => Ok(report));
    }

    private static IActionResult ExportResult<T>(
        string format,
        T report,
        string fileName,
        Func<T, byte[]> exportPdf,
        Func<T, byte[]> exportExcel,
        Func<T, IActionResult> exportJson)
    {
        if (string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase))
        {
            return new FileContentResult(exportPdf(report), "application/pdf")
            {
                FileDownloadName = $"{fileName}.pdf"
            };
        }

        if (string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(format, "excel", StringComparison.OrdinalIgnoreCase))
        {
            return new FileContentResult(
                exportExcel(report),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            {
                FileDownloadName = $"{fileName}.xlsx"
            };
        }

        return exportJson(report);
    }
}
