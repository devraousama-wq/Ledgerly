using Ledgerly.Application.Abstractions;
using Ledgerly.Application.Accounts;
using Ledgerly.Application.Journals;
using Ledgerly.Domain.Common;
using Ledgerly.Domain.Entities;
using Ledgerly.Domain.Enums;

namespace Ledgerly.Application.Tests;

public class JournalServiceTests
{
    private static readonly Guid OrganizationId = Guid.NewGuid();

    [Fact]
    public async Task CreateDraftAsync_returns_draft_entry()
    {
        var (journalService, _, _) = CreateServices();

        var result = await journalService.CreateDraftAsync(new CreateJournalRequest(
            OrganizationId,
            new DateOnly(2026, 2, 1),
            "JE-001",
            "Office supplies",
            "USD"));

        Assert.True(result.IsSuccess);
        Assert.Equal(JournalEntryStatus.Draft, result.Value!.Status);
        Assert.Equal("Office supplies", result.Value.Description);
        Assert.Empty(result.Value.Lines);
    }

    [Fact]
    public async Task AddLineAsync_adds_line_to_draft_entry()
    {
        var (journalService, accountService, _) = CreateServices();
        var cash = await CreateAccountAsync(accountService, "1000", "Cash");

        var draft = await journalService.CreateDraftAsync(new CreateJournalRequest(
            OrganizationId,
            new DateOnly(2026, 2, 1),
            null,
            "Cash receipt",
            "USD"));

        var result = await journalService.AddLineAsync(
            OrganizationId,
            draft.Value!.Id,
            new AddJournalLineRequest(cash.Value!.Id, 500m, 0m, "Deposit"));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Lines);
        Assert.Equal(500m, result.Value.Lines[0].DebitAmount);
    }

    [Fact]
    public async Task AddLineAsync_fails_when_account_missing()
    {
        var (journalService, _, _) = CreateServices();

        var draft = await journalService.CreateDraftAsync(new CreateJournalRequest(
            OrganizationId,
            new DateOnly(2026, 2, 1),
            null,
            "Cash receipt",
            "USD"));

        var result = await journalService.AddLineAsync(
            OrganizationId,
            draft.Value!.Id,
            new AddJournalLineRequest(Guid.NewGuid(), 100m, 0m, null));

        Assert.True(result.IsFailure);
        Assert.Equal("Account not found.", result.Error);
    }

    [Fact]
    public async Task AddLineAsync_fails_when_account_currency_differs()
    {
        var (journalService, accountService, _) = CreateServices();
        var eurAccount = await CreateAccountAsync(accountService, "2000", "EUR Cash", "EUR");

        var draft = await journalService.CreateDraftAsync(new CreateJournalRequest(
            OrganizationId,
            new DateOnly(2026, 2, 1),
            null,
            "Cash receipt",
            "USD"));

        var result = await journalService.AddLineAsync(
            OrganizationId,
            draft.Value!.Id,
            new AddJournalLineRequest(eurAccount.Value!.Id, 100m, 0m, null));

        Assert.True(result.IsFailure);
        Assert.Contains("currency", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PostAsync_posts_balanced_entry()
    {
        var (journalService, accountService, _) = CreateServices();
        var cash = await CreateAccountAsync(accountService, "1000", "Cash");
        var revenue = await CreateAccountAsync(accountService, "4000", "Revenue", accountType: AccountType.Income);

        var draft = await journalService.CreateDraftAsync(new CreateJournalRequest(
            OrganizationId,
            new DateOnly(2026, 2, 1),
            "JE-100",
            "Sale",
            "USD"));

        await journalService.AddLineAsync(
            OrganizationId,
            draft.Value!.Id,
            new AddJournalLineRequest(cash.Value!.Id, 300m, 0m, null));

        await journalService.AddLineAsync(
            OrganizationId,
            draft.Value.Id,
            new AddJournalLineRequest(revenue.Value!.Id, 0m, 300m, null));

        var result = await journalService.PostAsync(OrganizationId, draft.Value.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(JournalEntryStatus.Posted, result.Value!.Status);
    }

    [Fact]
    public async Task PostAsync_fails_when_unbalanced()
    {
        var (journalService, accountService, _) = CreateServices();
        var cash = await CreateAccountAsync(accountService, "1000", "Cash");
        var revenue = await CreateAccountAsync(accountService, "4000", "Revenue", accountType: AccountType.Income);

        var draft = await journalService.CreateDraftAsync(new CreateJournalRequest(
            OrganizationId,
            new DateOnly(2026, 2, 1),
            null,
            "Sale",
            "USD"));

        await journalService.AddLineAsync(
            OrganizationId,
            draft.Value!.Id,
            new AddJournalLineRequest(cash.Value!.Id, 300m, 0m, null));

        await journalService.AddLineAsync(
            OrganizationId,
            draft.Value.Id,
            new AddJournalLineRequest(revenue.Value!.Id, 0m, 200m, null));

        var result = await journalService.PostAsync(OrganizationId, draft.Value.Id);

        Assert.True(result.IsFailure);
        Assert.Contains("unbalanced", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReverseAsync_creates_posted_reversal_and_marks_original_reversed()
    {
        var (journalService, accountService, journalRepository) = CreateServices();
        var cash = await CreateAccountAsync(accountService, "1000", "Cash");
        var revenue = await CreateAccountAsync(accountService, "4000", "Revenue", accountType: AccountType.Income);

        var draft = await journalService.CreateDraftAsync(new CreateJournalRequest(
            OrganizationId,
            new DateOnly(2026, 2, 1),
            "JE-200",
            "Sale",
            "USD"));

        await journalService.AddLineAsync(
            OrganizationId,
            draft.Value!.Id,
            new AddJournalLineRequest(cash.Value!.Id, 150m, 0m, null));

        await journalService.AddLineAsync(
            OrganizationId,
            draft.Value.Id,
            new AddJournalLineRequest(revenue.Value!.Id, 0m, 150m, null));

        await journalService.PostAsync(OrganizationId, draft.Value.Id);

        var result = await journalService.ReverseAsync(OrganizationId, draft.Value.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(JournalEntryStatus.Posted, result.Value!.Status);
        Assert.Equal(draft.Value.Id, result.Value.ReversalOfEntryId);

        var original = await journalRepository.GetByIdAsync(OrganizationId, draft.Value.Id);
        Assert.Equal(JournalEntryStatus.Reversed, original!.Status);
    }

    [Fact]
    public async Task ReverseAsync_fails_when_entry_is_draft()
    {
        var (journalService, _, _) = CreateServices();

        var draft = await journalService.CreateDraftAsync(new CreateJournalRequest(
            OrganizationId,
            new DateOnly(2026, 2, 1),
            null,
            "Draft only",
            "USD"));

        var result = await journalService.ReverseAsync(OrganizationId, draft.Value!.Id);

        Assert.True(result.IsFailure);
        Assert.Contains("posted", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    private static (
        JournalService JournalService,
        AccountService AccountService,
        InMemoryJournalRepository JournalRepository) CreateServices()
    {
        var accountRepository = new InMemoryAccountRepository();
        var journalRepository = new InMemoryJournalRepository();
        var accountService = new AccountService(accountRepository);
        var journalService = new JournalService(journalRepository, accountRepository);
        return (journalService, accountService, journalRepository);
    }

    private static async Task<Result<AccountDto>> CreateAccountAsync(
        AccountService accountService,
        string code,
        string name,
        string currency = "USD",
        AccountType accountType = AccountType.Asset)
    {
        return await accountService.CreateAsync(new CreateAccountRequest(
            OrganizationId,
            code,
            name,
            accountType,
            currency,
            0m,
            null));
    }

    private sealed class InMemoryJournalRepository : IJournalRepository
    {
        private readonly List<JournalEntry> _entries = new();

        public Task<JournalEntry?> GetByIdAsync(
            Guid organizationId,
            Guid journalEntryId,
            CancellationToken cancellationToken = default)
        {
            var entry = _entries.FirstOrDefault(e =>
                e.OrganizationId == organizationId && e.Id == journalEntryId);

            return Task.FromResult(entry);
        }

        public Task<IReadOnlyList<JournalEntry>> ListByOrganizationAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<JournalEntry> results = _entries
                .Where(e => e.OrganizationId == organizationId)
                .OrderByDescending(e => e.EntryDate)
                .ThenByDescending(e => e.CreatedAt)
                .ToList();

            return Task.FromResult(results);
        }

        public Task AddAsync(JournalEntry entry, CancellationToken cancellationToken = default)
        {
            _entries.Add(entry);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(JournalEntry entry, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryAccountRepository : IAccountRepository
    {
        private readonly List<Account> _accounts = new();

        public Task<Account?> GetByIdAsync(
            Guid organizationId,
            Guid accountId,
            CancellationToken cancellationToken = default)
        {
            var account = _accounts.FirstOrDefault(a =>
                a.OrganizationId == organizationId && a.Id == accountId);

            return Task.FromResult(account);
        }

        public Task<Account?> GetByCodeAsync(
            Guid organizationId,
            string code,
            CancellationToken cancellationToken = default)
        {
            var account = _accounts.FirstOrDefault(a =>
                a.OrganizationId == organizationId &&
                string.Equals(a.Code, code, StringComparison.Ordinal));

            return Task.FromResult(account);
        }

        public Task<IReadOnlyList<Account>> ListByOrganizationAsync(
            Guid organizationId,
            bool includeArchived = false,
            CancellationToken cancellationToken = default)
        {
            var query = _accounts.Where(a => a.OrganizationId == organizationId);

            if (!includeArchived)
            {
                query = query.Where(a => !a.IsArchived);
            }

            IReadOnlyList<Account> results = query.OrderBy(a => a.Code).ToList();
            return Task.FromResult(results);
        }

        public Task<IReadOnlyList<Account>> GetChildrenAsync(
            Guid organizationId,
            Guid parentAccountId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Account> results = _accounts
                .Where(a =>
                    a.OrganizationId == organizationId &&
                    a.ParentAccountId == parentAccountId)
                .OrderBy(a => a.Code)
                .ToList();

            return Task.FromResult(results);
        }

        public Task<bool> HasActiveChildrenAsync(
            Guid organizationId,
            Guid accountId,
            CancellationToken cancellationToken = default)
        {
            var hasChildren = _accounts.Any(a =>
                a.OrganizationId == organizationId &&
                a.ParentAccountId == accountId &&
                !a.IsArchived);

            return Task.FromResult(hasChildren);
        }

        public Task AddAsync(Account account, CancellationToken cancellationToken = default)
        {
            _accounts.Add(account);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Account account, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
