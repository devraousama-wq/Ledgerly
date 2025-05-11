using Ledgerly.Domain.Enums;

namespace Ledgerly.Application.Reporting;

public sealed record TrialBalanceReportRequest(DateOnly AsOfDate);

public sealed record TrialBalanceLineDto(
    Guid AccountId,
    string Code,
    string Name,
    AccountType AccountType,
    decimal DebitBalance,
    decimal CreditBalance);

public sealed record TrialBalanceReportDto(
    DateOnly AsOfDate,
    decimal TotalDebits,
    decimal TotalCredits,
    IReadOnlyList<TrialBalanceLineDto> Lines);

public sealed record ProfitAndLossReportRequest(DateOnly StartDate, DateOnly EndDate);

public sealed record ProfitAndLossLineDto(
    Guid AccountId,
    string Code,
    string Name,
    AccountType AccountType,
    decimal Amount);

public sealed record ProfitAndLossReportDto(
    DateOnly StartDate,
    DateOnly EndDate,
    decimal TotalIncome,
    decimal TotalExpenses,
    decimal NetIncome,
    IReadOnlyList<ProfitAndLossLineDto> IncomeLines,
    IReadOnlyList<ProfitAndLossLineDto> ExpenseLines);

public sealed record BalanceSheetReportRequest(DateOnly AsOfDate);

public sealed record BalanceSheetLineDto(
    Guid AccountId,
    string Code,
    string Name,
    AccountType AccountType,
    decimal Balance);

public sealed record BalanceSheetReportDto(
    DateOnly AsOfDate,
    decimal TotalAssets,
    decimal TotalLiabilities,
    decimal TotalEquity,
    IReadOnlyList<BalanceSheetLineDto> AssetLines,
    IReadOnlyList<BalanceSheetLineDto> LiabilityLines,
    IReadOnlyList<BalanceSheetLineDto> EquityLines);

public sealed record AgedReceivablesReportRequest(DateOnly AsOfDate);

public sealed record AgedReceivableLineDto(
    Guid InvoiceId,
    string InvoiceNumber,
    Guid CustomerId,
    string CustomerName,
    DateOnly IssueDate,
    DateOnly DueDate,
    decimal TotalAmount,
    decimal AmountPaid,
    decimal BalanceDue,
    int DaysOverdue,
    decimal Current,
    decimal Days1To30,
    decimal Days31To60,
    decimal Days61To90,
    decimal DaysOver90);

public sealed record AgedReceivablesReportDto(
    DateOnly AsOfDate,
    decimal TotalOutstanding,
    decimal TotalCurrent,
    decimal TotalDays1To30,
    decimal TotalDays31To60,
    decimal TotalDays61To90,
    decimal TotalDaysOver90,
    IReadOnlyList<AgedReceivableLineDto> Lines);
