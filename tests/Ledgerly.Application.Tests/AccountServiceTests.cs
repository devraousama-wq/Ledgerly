using Ledgerly.Application.Abstractions;
using Ledgerly.Application.Accounts;
using Ledgerly.Domain.Entities;
using Ledgerly.Domain.Enums;

namespace Ledgerly.Application.Tests;

public class AccountServiceTests
{
    private static readonly Guid OrganizationId = Guid.NewGuid();

    [Fact]
    public async Task CreateAsync_returns_account_when_valid()
    {
        var repository = new InMemoryAccountRepository();
        var service = new AccountService(repository);

        var result = await service.CreateAsync(new CreateAccountRequest(
            OrganizationId,
            "1000",
            "Cash",
            AccountType.Asset,
            "USD",
            5000m,
            null));

        Assert.True(result.IsSuccess);
        Assert.Equal("1000", result.Value!.Code);
        Assert.Equal("Cash", result.Value.Name);
        Assert.Equal(5000m, result.Value.OpeningBalanceAmount);
    }

    [Fact]
    public async Task CreateAsync_fails_when_code_exists()
    {
        var repository = new InMemoryAccountRepository();
        var service = new AccountService(repository);

        await service.CreateAsync(new CreateAccountRequest(
            OrganizationId,
            "1000",
            "Cash",
            AccountType.Asset,
            "USD",
            0m,
            null));

        var result = await service.CreateAsync(new CreateAccountRequest(
            OrganizationId,
            "1000",
            "Duplicate Cash",
            AccountType.Asset,
            "USD",
            0m,
            null));

        Assert.True(result.IsFailure);
        Assert.Contains("already exists", result.Error);
    }

    [Fact]
    public async Task CreateAsync_fails_when_parent_not_found()
    {
        var repository = new InMemoryAccountRepository();
        var service = new AccountService(repository);

        var result = await service.CreateAsync(new CreateAccountRequest(
            OrganizationId,
            "1010",
            "Petty Cash",
            AccountType.Asset,
            "USD",
            0m,
            Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Contains("Parent account not found", result.Error);
    }

    [Fact]
    public async Task CreateAsync_fails_when_parent_type_differs()
    {
        var repository = new InMemoryAccountRepository();
        var service = new AccountService(repository);

        var parent = await service.CreateAsync(new CreateAccountRequest(
            OrganizationId,
            "2000",
            "Accounts Payable",
            AccountType.Liability,
            "USD",
            0m,
            null));

        var result = await service.CreateAsync(new CreateAccountRequest(
            OrganizationId,
            "1010",
            "Operating Cash",
            AccountType.Asset,
            "USD",
            0m,
            parent.Value!.Id));

        Assert.True(result.IsFailure);
        Assert.Contains("same account type", result.Error);
    }

    [Fact]
    public async Task CreateAsync_succeeds_with_valid_parent()
    {
        var repository = new InMemoryAccountRepository();
        var service = new AccountService(repository);

        var parent = await service.CreateAsync(new CreateAccountRequest(
            OrganizationId,
            "1000",
            "Cash",
            AccountType.Asset,
            "USD",
            0m,
            null));

        var result = await service.CreateAsync(new CreateAccountRequest(
            OrganizationId,
            "1010",
            "Petty Cash",
            AccountType.Asset,
            "USD",
            100m,
            parent.Value!.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal(parent.Value.Id, result.Value!.ParentAccountId);
    }

    [Fact]
    public async Task GetByIdAsync_returns_account()
    {
        var repository = new InMemoryAccountRepository();
        var service = new AccountService(repository);

        var created = await service.CreateAsync(new CreateAccountRequest(
            OrganizationId,
            "1000",
            "Cash",
            AccountType.Asset,
            "USD",
            0m,
            null));

        var result = await service.GetByIdAsync(OrganizationId, created.Value!.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(created.Value.Id, result.Value!.Id);
    }

    [Fact]
    public async Task GetByIdAsync_fails_when_missing()
    {
        var repository = new InMemoryAccountRepository();
        var service = new AccountService(repository);

        var result = await service.GetByIdAsync(OrganizationId, Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("Account not found.", result.Error);
    }

    [Fact]
    public async Task ListByOrganizationAsync_excludes_archived_by_default()
    {
        var repository = new InMemoryAccountRepository();
        var service = new AccountService(repository);

        var active = await service.CreateAsync(new CreateAccountRequest(
            OrganizationId,
            "1000",
            "Cash",
            AccountType.Asset,
            "USD",
            0m,
            null));

        var archived = await service.CreateAsync(new CreateAccountRequest(
            OrganizationId,
            "2000",
            "Old Cash",
            AccountType.Asset,
            "USD",
            0m,
            null));

        await service.ArchiveAsync(OrganizationId, archived.Value!.Id);

        var result = await service.ListByOrganizationAsync(OrganizationId);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal(active.Value!.Id, result.Value![0].Id);
    }

    [Fact]
    public async Task ListByOrganizationAsync_includes_archived_when_requested()
    {
        var repository = new InMemoryAccountRepository();
        var service = new AccountService(repository);

        await service.CreateAsync(new CreateAccountRequest(
            OrganizationId,
            "1000",
            "Cash",
            AccountType.Asset,
            "USD",
            0m,
            null));

        var archived = await service.CreateAsync(new CreateAccountRequest(
            OrganizationId,
            "2000",
            "Old Cash",
            AccountType.Asset,
            "USD",
            0m,
            null));

        await service.ArchiveAsync(OrganizationId, archived.Value!.Id);

        var result = await service.ListByOrganizationAsync(OrganizationId, includeArchived: true);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
    }

    [Fact]
    public async Task UpdateAsync_changes_account()
    {
        var repository = new InMemoryAccountRepository();
        var service = new AccountService(repository);

        var created = await service.CreateAsync(new CreateAccountRequest(
            OrganizationId,
            "1000",
            "Cash",
            AccountType.Asset,
            "USD",
            0m,
            null));

        var result = await service.UpdateAsync(
            OrganizationId,
            created.Value!.Id,
            new UpdateAccountRequest(
                "1100",
                "Operating Cash",
                AccountType.Asset,
                "USD",
                750m,
                null));

        Assert.True(result.IsSuccess);
        Assert.Equal("1100", result.Value!.Code);
        Assert.Equal("Operating Cash", result.Value.Name);
        Assert.Equal(750m, result.Value.OpeningBalanceAmount);
    }

    [Fact]
    public async Task UpdateAsync_fails_when_self_parent()
    {
        var repository = new InMemoryAccountRepository();
        var service = new AccountService(repository);

        var created = await service.CreateAsync(new CreateAccountRequest(
            OrganizationId,
            "1000",
            "Cash",
            AccountType.Asset,
            "USD",
            0m,
            null));

        var result = await service.UpdateAsync(
            OrganizationId,
            created.Value!.Id,
            new UpdateAccountRequest(
                "1000",
                "Cash",
                AccountType.Asset,
                "USD",
                0m,
                created.Value.Id));

        Assert.True(result.IsFailure);
        Assert.Contains("own parent", result.Error);
    }

    [Fact]
    public async Task UpdateAsync_fails_when_cycle_would_be_created()
    {
        var repository = new InMemoryAccountRepository();
        var service = new AccountService(repository);

        var parent = await service.CreateAsync(new CreateAccountRequest(
            OrganizationId,
            "1000",
            "Cash",
            AccountType.Asset,
            "USD",
            0m,
            null));

        var child = await service.CreateAsync(new CreateAccountRequest(
            OrganizationId,
            "1010",
            "Petty Cash",
            AccountType.Asset,
            "USD",
            0m,
            parent.Value!.Id));

        var result = await service.UpdateAsync(
            OrganizationId,
            parent.Value.Id,
            new UpdateAccountRequest(
                "1000",
                "Cash",
                AccountType.Asset,
                "USD",
                0m,
                child.Value!.Id));

        Assert.True(result.IsFailure);
        Assert.Contains("circular hierarchy", result.Error);
    }

    [Fact]
    public async Task ArchiveAsync_marks_account_archived()
    {
        var repository = new InMemoryAccountRepository();
        var service = new AccountService(repository);

        var created = await service.CreateAsync(new CreateAccountRequest(
            OrganizationId,
            "1000",
            "Cash",
            AccountType.Asset,
            "USD",
            0m,
            null));

        var result = await service.ArchiveAsync(OrganizationId, created.Value!.Id);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsArchived);
    }

    [Fact]
    public async Task ArchiveAsync_fails_when_active_children_exist()
    {
        var repository = new InMemoryAccountRepository();
        var service = new AccountService(repository);

        var parent = await service.CreateAsync(new CreateAccountRequest(
            OrganizationId,
            "1000",
            "Cash",
            AccountType.Asset,
            "USD",
            0m,
            null));

        await service.CreateAsync(new CreateAccountRequest(
            OrganizationId,
            "1010",
            "Petty Cash",
            AccountType.Asset,
            "USD",
            0m,
            parent.Value!.Id));

        var result = await service.ArchiveAsync(OrganizationId, parent.Value.Id);

        Assert.True(result.IsFailure);
        Assert.Contains("active child accounts", result.Error);
    }

    [Fact]
    public async Task CreateAsync_fails_when_parent_is_archived()
    {
        var repository = new InMemoryAccountRepository();
        var service = new AccountService(repository);

        var parent = await service.CreateAsync(new CreateAccountRequest(
            OrganizationId,
            "1000",
            "Cash",
            AccountType.Asset,
            "USD",
            0m,
            null));

        await service.ArchiveAsync(OrganizationId, parent.Value!.Id);

        var result = await service.CreateAsync(new CreateAccountRequest(
            OrganizationId,
            "1010",
            "Petty Cash",
            AccountType.Asset,
            "USD",
            0m,
            parent.Value.Id));

        Assert.True(result.IsFailure);
        Assert.Contains("archived account as parent", result.Error);
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
            var query = _accounts
                .Where(a => a.OrganizationId == organizationId);

            if (!includeArchived)
            {
                query = query.Where(a => !a.IsArchived);
            }

            IReadOnlyList<Account> results = query
                .OrderBy(a => a.Code)
                .ToList();

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
