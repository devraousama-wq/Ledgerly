using Ledgerly.Domain.Common;
using Ledgerly.Domain.Enums;

namespace Ledgerly.Application.Reporting;

public sealed class ProfitAndLossReportService
{
    private readonly PostedJournalAggregator _aggregator;

    public ProfitAndLossReportService(PostedJournalAggregator aggregator)
    {
        _aggregator = aggregator;
    }

    public async Task<Result<ProfitAndLossReportDto>> GenerateAsync(
        Guid organizationId,
        ProfitAndLossReportRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.EndDate < request.StartDate)
        {
            return Result<ProfitAndLossReportDto>.Failure("End date must be on or after start date.");
        }

        var activity = await _aggregator.GetPeriodActivityAsync(
            organizationId,
            request.StartDate,
            request.EndDate,
            cancellationToken);

        var incomeLines = activity
            .Where(item => item.AccountType == AccountType.Income && item.PeriodAmount != 0m)
            .Select(item => new ProfitAndLossLineDto(
                item.AccountId,
                item.Code,
                item.Name,
                item.AccountType,
                item.PeriodAmount))
            .OrderBy(line => line.Code)
            .ToList();

        var expenseLines = activity
            .Where(item => item.AccountType == AccountType.Expense && item.PeriodAmount != 0m)
            .Select(item => new ProfitAndLossLineDto(
                item.AccountId,
                item.Code,
                item.Name,
                item.AccountType,
                item.PeriodAmount))
            .OrderBy(line => line.Code)
            .ToList();

        var totalIncome = incomeLines.Sum(line => line.Amount);
        var totalExpenses = expenseLines.Sum(line => line.Amount);

        return Result<ProfitAndLossReportDto>.Success(new ProfitAndLossReportDto(
            request.StartDate,
            request.EndDate,
            totalIncome,
            totalExpenses,
            totalIncome - totalExpenses,
            incomeLines,
            expenseLines));
    }
}
