using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Ledgerly.Application.Reporting.Export;

public sealed class ReportPdfExporter
{
    public ReportPdfExporter()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] ExportTrialBalance(TrialBalanceReportDto report) =>
        CreateDocument(
            "Trial Balance",
            $"As of {report.AsOfDate:yyyy-MM-dd}",
            new[]
            {
                ("Code", 80f),
                ("Account", 180f),
                ("Type", 80f),
                ("Debit", 80f),
                ("Credit", 80f)
            },
            report.Lines.Select(line => new[]
            {
                line.Code,
                line.Name,
                line.AccountType.ToString(),
                FormatAmount(line.DebitBalance),
                FormatAmount(line.CreditBalance)
            }),
            new[]
            {
                string.Empty,
                "Totals",
                string.Empty,
                FormatAmount(report.TotalDebits),
                FormatAmount(report.TotalCredits)
            });

    public byte[] ExportProfitAndLoss(ProfitAndLossReportDto report) =>
        CreateDocument(
            "Profit and Loss",
            $"{report.StartDate:yyyy-MM-dd} to {report.EndDate:yyyy-MM-dd}",
            new[]
            {
                ("Section", 100f),
                ("Code", 80f),
                ("Account", 180f),
                ("Amount", 80f)
            },
            report.IncomeLines.Select(line => new[]
            {
                "Income",
                line.Code,
                line.Name,
                FormatAmount(line.Amount)
            })
            .Concat(report.ExpenseLines.Select(line => new[]
            {
                "Expense",
                line.Code,
                line.Name,
                FormatAmount(line.Amount)
            })),
            new[]
            {
                "Summary",
                string.Empty,
                "Net Income",
                FormatAmount(report.NetIncome)
            });

    public byte[] ExportBalanceSheet(BalanceSheetReportDto report) =>
        CreateDocument(
            "Balance Sheet",
            $"As of {report.AsOfDate:yyyy-MM-dd}",
            new[]
            {
                ("Section", 100f),
                ("Code", 80f),
                ("Account", 180f),
                ("Balance", 80f)
            },
            report.AssetLines.Select(line => new[]
            {
                "Asset",
                line.Code,
                line.Name,
                FormatAmount(line.Balance)
            })
            .Concat(report.LiabilityLines.Select(line => new[]
            {
                "Liability",
                line.Code,
                line.Name,
                FormatAmount(line.Balance)
            }))
            .Concat(report.EquityLines.Select(line => new[]
            {
                "Equity",
                line.Code,
                line.Name,
                FormatAmount(line.Balance)
            })),
            new[]
            {
                "Totals",
                string.Empty,
                "Assets / Liabilities / Equity",
                $"{FormatAmount(report.TotalAssets)} / {FormatAmount(report.TotalLiabilities)} / {FormatAmount(report.TotalEquity)}"
            });

    public byte[] ExportAgedReceivables(AgedReceivablesReportDto report) =>
        CreateDocument(
            "Aged Receivables",
            $"As of {report.AsOfDate:yyyy-MM-dd}",
            new[]
            {
                ("Invoice", 70f),
                ("Customer", 120f),
                ("Due", 70f),
                ("Balance", 70f),
                ("Current", 60f),
                ("1-30", 50f),
                ("31-60", 50f),
                ("61-90", 50f),
                ("90+", 50f)
            },
            report.Lines.Select(line => new[]
            {
                line.InvoiceNumber,
                line.CustomerName,
                line.DueDate.ToString("yyyy-MM-dd"),
                FormatAmount(line.BalanceDue),
                FormatAmount(line.Current),
                FormatAmount(line.Days1To30),
                FormatAmount(line.Days31To60),
                FormatAmount(line.Days61To90),
                FormatAmount(line.DaysOver90)
            }),
            new[]
            {
                "Totals",
                string.Empty,
                string.Empty,
                FormatAmount(report.TotalOutstanding),
                FormatAmount(report.TotalCurrent),
                FormatAmount(report.TotalDays1To30),
                FormatAmount(report.TotalDays31To60),
                FormatAmount(report.TotalDays61To90),
                FormatAmount(report.TotalDaysOver90)
            });

    private static byte[] CreateDocument(
        string title,
        string subtitle,
        (string Header, float Width)[] columns,
        IEnumerable<string[]> rows,
        string[] footerRow)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Size(PageSizes.A4.Landscape());
                page.DefaultTextStyle(text => text.FontSize(10));

                page.Header().Column(column =>
                {
                    column.Item().Text(title).SemiBold().FontSize(16);
                    column.Item().Text(subtitle).FontSize(11);
                });

                page.Content().PaddingVertical(10).Table(table =>
                {
                    table.ColumnsDefinition(definition =>
                    {
                        foreach (var column in columns)
                        {
                            definition.ConstantColumn(column.Width);
                        }
                    });

                    table.Header(header =>
                    {
                        foreach (var column in columns)
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text(column.Header).SemiBold();
                        }
                    });

                    foreach (var row in rows)
                    {
                        foreach (var cell in row)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(cell);
                        }
                    }

                    foreach (var cell in footerRow)
                    {
                        table.Cell().Background(Colors.Grey.Lighten4).Padding(4).Text(cell).SemiBold();
                    }
                });
            });
        });

        return document.GeneratePdf();
    }

    private static string FormatAmount(decimal amount) => amount.ToString("0.00");
}
