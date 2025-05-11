using Ledgerly.Domain.Common;
using Ledgerly.Domain.Enums;

namespace Ledgerly.Application.Reporting;

public sealed class BalanceSheetReportService
{
    private readonly PostedJournalAggregator _aggregator;

    public BalanceSheetReportService(PostedJournalAggregator aggregator)
    {
        _aggregator = aggregator;
    }

    public async Task<Result<BalanceSheetReportDto>> GenerateAsync(
        Guid organizationId,
        BalanceSheetReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var snapshots = await _aggregator.GetBalancesAsOfAsync(
            organizationId,
            request.AsOfDate,
            cancellationToken);

        var assetLines = snapshots
            .Where(item => item.AccountType == AccountType.Asset && item.SignedBalance != 0m)
            .Select(item => new BalanceSheetLineDto(
                item.AccountId,
                item.Code,
                item.Name,
                item.AccountType,
                item.SignedBalance))
            .OrderBy(line => line.Code)
            .ToList();

        var liabilityLines = snapshots
            .Where(item => item.AccountType == AccountType.Liability && item.SignedBalance != 0m)
            .Select(item => new BalanceSheetLineDto(
                item.AccountId,
                item.Code,
                item.Name,
                item.AccountType,
                item.SignedBalance))
            .OrderBy(line => line.Code)
            .ToList();

        var equityLines = snapshots
            .Where(item => item.AccountType == AccountType.Equity && item.SignedBalance != 0m)
            .Select(item => new BalanceSheetLineDto(
                item.AccountId,
                item.Code,
                item.Name,
                item.AccountType,
                item.SignedBalance))
            .OrderBy(line => line.Code)
            .ToList();

        return Result<BalanceSheetReportDto>.Success(new BalanceSheetReportDto(
            request.AsOfDate,
            assetLines.Sum(line => line.Balance),
            liabilityLines.Sum(line => line.Balance),
            equityLines.Sum(line => line.Balance),
            assetLines,
            liabilityLines,
            equityLines));
    }
}
