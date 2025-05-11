using Ledgerly.Domain.Common;

namespace Ledgerly.Application.Reporting;

public sealed class TrialBalanceReportService
{
    private readonly PostedJournalAggregator _aggregator;

    public TrialBalanceReportService(PostedJournalAggregator aggregator)
    {
        _aggregator = aggregator;
    }

    public async Task<Result<TrialBalanceReportDto>> GenerateAsync(
        Guid organizationId,
        TrialBalanceReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var snapshots = await _aggregator.GetBalancesAsOfAsync(
            organizationId,
            request.AsOfDate,
            cancellationToken);

        var lines = snapshots
            .Select(snapshot =>
            {
                var columns = AccountBalanceCalculator.ToTrialBalanceColumns(
                    snapshot.AccountType,
                    snapshot.SignedBalance);

                return new TrialBalanceLineDto(
                    snapshot.AccountId,
                    snapshot.Code,
                    snapshot.Name,
                    snapshot.AccountType,
                    columns.DebitBalance,
                    columns.CreditBalance);
            })
            .Where(line => line.DebitBalance != 0m || line.CreditBalance != 0m)
            .OrderBy(line => line.Code)
            .ToList();

        return Result<TrialBalanceReportDto>.Success(new TrialBalanceReportDto(
            request.AsOfDate,
            lines.Sum(line => line.DebitBalance),
            lines.Sum(line => line.CreditBalance),
            lines));
    }
}
