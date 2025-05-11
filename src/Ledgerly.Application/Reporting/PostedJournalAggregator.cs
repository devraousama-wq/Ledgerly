using Ledgerly.Application.Abstractions;
using Ledgerly.Domain.Entities;

namespace Ledgerly.Application.Reporting;

public sealed class PostedJournalAggregator
{
    private readonly IAccountRepository _accountRepository;
    private readonly IJournalRepository _journalRepository;

    public PostedJournalAggregator(
        IAccountRepository accountRepository,
        IJournalRepository journalRepository)
    {
        _accountRepository = accountRepository;
        _journalRepository = journalRepository;
    }

    public async Task<IReadOnlyList<AccountBalanceSnapshot>> GetBalancesAsOfAsync(
        Guid organizationId,
        DateOnly asOfDate,
        CancellationToken cancellationToken = default)
    {
        var accounts = await _accountRepository.ListByOrganizationAsync(
            organizationId,
            includeArchived: true,
            cancellationToken);

        var postedLines = await _journalRepository.ListPostedLinesAsync(
            organizationId,
            null,
            asOfDate,
            cancellationToken);

        return BuildSnapshots(accounts, postedLines, includeOpeningBalance: true);
    }

    public async Task<IReadOnlyList<AccountPeriodActivity>> GetPeriodActivityAsync(
        Guid organizationId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        var accounts = await _accountRepository.ListByOrganizationAsync(
            organizationId,
            includeArchived: true,
            cancellationToken);

        var postedLines = await _journalRepository.ListPostedLinesAsync(
            organizationId,
            startDate,
            endDate,
            cancellationToken);

        var snapshots = BuildSnapshots(accounts, postedLines, includeOpeningBalance: false);

        return snapshots
            .Select(snapshot => new AccountPeriodActivity(
                snapshot.AccountId,
                snapshot.Code,
                snapshot.Name,
                snapshot.AccountType,
                snapshot.TotalDebits,
                snapshot.TotalCredits,
                AccountBalanceCalculator.GetPeriodAmount(
                    snapshot.AccountType,
                    snapshot.TotalDebits,
                    snapshot.TotalCredits)))
            .ToList();
    }

    private static IReadOnlyList<AccountBalanceSnapshot> BuildSnapshots(
        IReadOnlyList<Account> accounts,
        IReadOnlyList<(JournalEntry Entry, JournalLine Line)> postedLines,
        bool includeOpeningBalance)
    {
        var totals = accounts.ToDictionary(
            account => account.Id,
            _ => (Debits: 0m, Credits: 0m));

        foreach (var (_, line) in postedLines)
        {
            if (!totals.ContainsKey(line.AccountId))
            {
                continue;
            }

            var current = totals[line.AccountId];
            totals[line.AccountId] = (
                current.Debits + line.Debit.Amount,
                current.Credits + line.Credit.Amount);
        }

        var snapshots = new List<AccountBalanceSnapshot>();

        foreach (var account in accounts.OrderBy(a => a.Code))
        {
            var (debits, credits) = totals.GetValueOrDefault(account.Id);
            var opening = includeOpeningBalance ? account.OpeningBalance.Amount : 0m;
            var signedBalance = AccountBalanceCalculator.GetSignedBalance(
                account.AccountType,
                opening,
                debits,
                credits);

            if (debits == 0m && credits == 0m && opening == 0m)
            {
                continue;
            }

            snapshots.Add(new AccountBalanceSnapshot(
                account.Id,
                account.Code,
                account.Name,
                account.AccountType,
                debits,
                credits,
                signedBalance));
        }

        return snapshots;
    }
}

public sealed record AccountBalanceSnapshot(
    Guid AccountId,
    string Code,
    string Name,
    Domain.Enums.AccountType AccountType,
    decimal TotalDebits,
    decimal TotalCredits,
    decimal SignedBalance);

public sealed record AccountPeriodActivity(
    Guid AccountId,
    string Code,
    string Name,
    Domain.Enums.AccountType AccountType,
    decimal TotalDebits,
    decimal TotalCredits,
    decimal PeriodAmount);
