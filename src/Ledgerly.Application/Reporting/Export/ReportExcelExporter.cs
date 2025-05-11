using ClosedXML.Excel;

namespace Ledgerly.Application.Reporting.Export;

public sealed class ReportExcelExporter
{
    public byte[] ExportTrialBalance(TrialBalanceReportDto report) =>
        BuildWorkbook(
            "Trial Balance",
            new[] { "Code", "Account", "Type", "Debit", "Credit" },
            report.Lines.Select(line => new object?[]
            {
                line.Code,
                line.Name,
                line.AccountType.ToString(),
                line.DebitBalance,
                line.CreditBalance
            }),
            new object?[] { string.Empty, "Totals", string.Empty, report.TotalDebits, report.TotalCredits });

    public byte[] ExportProfitAndLoss(ProfitAndLossReportDto report) =>
        BuildWorkbook(
            "Profit and Loss",
            new[] { "Section", "Code", "Account", "Amount" },
            report.IncomeLines.Select(line => new object?[] { "Income", line.Code, line.Name, line.Amount })
                .Concat(report.ExpenseLines.Select(line => new object?[] { "Expense", line.Code, line.Name, line.Amount })),
            new object?[] { "Summary", string.Empty, "Net Income", report.NetIncome });

    public byte[] ExportBalanceSheet(BalanceSheetReportDto report) =>
        BuildWorkbook(
            "Balance Sheet",
            new[] { "Section", "Code", "Account", "Balance" },
            report.AssetLines.Select(line => new object?[] { "Asset", line.Code, line.Name, line.Balance })
                .Concat(report.LiabilityLines.Select(line => new object?[] { "Liability", line.Code, line.Name, line.Balance }))
                .Concat(report.EquityLines.Select(line => new object?[] { "Equity", line.Code, line.Name, line.Balance })),
            new object?[]
            {
                "Totals",
                string.Empty,
                "Assets / Liabilities / Equity",
                $"{report.TotalAssets} / {report.TotalLiabilities} / {report.TotalEquity}"
            });

    public byte[] ExportAgedReceivables(AgedReceivablesReportDto report) =>
        BuildWorkbook(
            "Aged Receivables",
            new[]
            {
                "Invoice",
                "Customer",
                "Issue Date",
                "Due Date",
                "Total",
                "Paid",
                "Balance",
                "Current",
                "1-30",
                "31-60",
                "61-90",
                "90+"
            },
            report.Lines.Select(line => new object?[]
            {
                line.InvoiceNumber,
                line.CustomerName,
                line.IssueDate,
                line.DueDate,
                line.TotalAmount,
                line.AmountPaid,
                line.BalanceDue,
                line.Current,
                line.Days1To30,
                line.Days31To60,
                line.Days61To90,
                line.DaysOver90
            }),
            new object?[]
            {
                "Totals",
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                report.TotalOutstanding,
                report.TotalCurrent,
                report.TotalDays1To30,
                report.TotalDays31To60,
                report.TotalDays61To90,
                report.TotalDaysOver90
            });

    private static byte[] BuildWorkbook(
        string sheetName,
        string[] headers,
        IEnumerable<object?[]> rows,
        object?[] footerRow)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(sheetName);

        for (var index = 0; index < headers.Length; index++)
        {
            worksheet.Cell(1, index + 1).Value = headers[index];
            worksheet.Cell(1, index + 1).Style.Font.Bold = true;
        }

        var rowIndex = 2;

        foreach (var row in rows)
        {
            for (var columnIndex = 0; columnIndex < row.Length; columnIndex++)
            {
                worksheet.Cell(rowIndex, columnIndex + 1).Value = XLCellValue.FromObject(row[columnIndex]);
            }

            rowIndex++;
        }

        for (var columnIndex = 0; columnIndex < footerRow.Length; columnIndex++)
        {
            worksheet.Cell(rowIndex, columnIndex + 1).Value = XLCellValue.FromObject(footerRow[columnIndex]);
            worksheet.Cell(rowIndex, columnIndex + 1).Style.Font.Bold = true;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
