using Ledgerly.Application.Abstractions;
using Ledgerly.Application.TaxCodes;
using Ledgerly.Domain.Entities;
using Ledgerly.Domain.Enums;
using Ledgerly.Domain.ValueObjects;

namespace Ledgerly.Application.Tests;

public class TaxCodeServiceTests
{
    private static readonly Guid OrganizationId = Guid.NewGuid();

    [Fact]
    public async Task CreateAsync_returns_tax_code_when_valid()
    {
        var accounts = new InMemoryAccountRepository();
        var taxCodes = new InMemoryTaxCodeRepository();
        var service = new TaxCodeService(taxCodes, accounts);
        var liability = new Account(
            OrganizationId,
            "2200",
            "Sales Tax Payable",
            AccountType.Liability,
            new CurrencyCode("USD"),
            new Money(0m, new CurrencyCode("USD")),
            null);

        await accounts.AddAsync(liability);

        var result = await service.CreateAsync(new CreateTaxCodeRequest(
            OrganizationId,
            "GST",
            "Goods and Services Tax",
            TaxType.Gst,
            0.1m,
            liability.Id,
            null));

        Assert.True(result.IsSuccess);
        Assert.Equal("GST", result.Value!.Code);
        Assert.Equal(0.1m, result.Value.EffectiveRate);
    }

    [Fact]
    public async Task CreateAsync_returns_compound_tax_code()
    {
        var accounts = new InMemoryAccountRepository();
        var taxCodes = new InMemoryTaxCodeRepository();
        var service = new TaxCodeService(taxCodes, accounts);
        var liability = new Account(
            OrganizationId,
            "2200",
            "Sales Tax Payable",
            AccountType.Liability,
            new CurrencyCode("USD"),
            new Money(0m, new CurrencyCode("USD")),
            null);

        await accounts.AddAsync(liability);

        var result = await service.CreateAsync(new CreateTaxCodeRequest(
            OrganizationId,
            "HST",
            "Harmonized Sales Tax",
            TaxType.SalesTax,
            0m,
            liability.Id,
            new[]
            {
                new CreateTaxCodeComponentRequest("Federal", 0.05m, 1, false),
                new CreateTaxCodeComponentRequest("Provincial", 0.08m, 2, false)
            }));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsCompound);
        Assert.Equal(2, result.Value.Components.Count);
        Assert.Equal(0.13m, result.Value.EffectiveRate);
    }

    [Fact]
    public async Task CreateAsync_fails_when_code_exists()
    {
        var accounts = new InMemoryAccountRepository();
        var taxCodes = new InMemoryTaxCodeRepository();
        var service = new TaxCodeService(taxCodes, accounts);
        var liability = new Account(
            OrganizationId,
            "2200",
            "Sales Tax Payable",
            AccountType.Liability,
            new CurrencyCode("USD"),
            new Money(0m, new CurrencyCode("USD")),
            null);

        await accounts.AddAsync(liability);

        await service.CreateAsync(new CreateTaxCodeRequest(
            OrganizationId,
            "GST",
            "Goods and Services Tax",
            TaxType.Gst,
            0.1m,
            liability.Id,
            null));

        var result = await service.CreateAsync(new CreateTaxCodeRequest(
            OrganizationId,
            "GST",
            "Duplicate",
            TaxType.Gst,
            0.05m,
            liability.Id,
            null));

        Assert.True(result.IsFailure);
        Assert.Contains("already exists", result.Error);
    }

    [Fact]
    public async Task UpdateAsync_changes_tax_code()
    {
        var accounts = new InMemoryAccountRepository();
        var taxCodes = new InMemoryTaxCodeRepository();
        var service = new TaxCodeService(taxCodes, accounts);
        var liability = new Account(
            OrganizationId,
            "2200",
            "Sales Tax Payable",
            AccountType.Liability,
            new CurrencyCode("USD"),
            new Money(0m, new CurrencyCode("USD")),
            null);

        await accounts.AddAsync(liability);

        var created = await service.CreateAsync(new CreateTaxCodeRequest(
            OrganizationId,
            "GST",
            "Goods and Services Tax",
            TaxType.Gst,
            0.1m,
            liability.Id,
            null));

        var result = await service.UpdateAsync(
            OrganizationId,
            created.Value!.Id,
            new UpdateTaxCodeRequest(
                "GST10",
                "Updated GST",
                TaxType.Gst,
                0.12m,
                liability.Id,
                null));

        Assert.True(result.IsSuccess);
        Assert.Equal("GST10", result.Value!.Code);
        Assert.Equal(0.12m, result.Value.EffectiveRate);
    }

    [Fact]
    public async Task ArchiveAsync_marks_tax_code_archived()
    {
        var accounts = new InMemoryAccountRepository();
        var taxCodes = new InMemoryTaxCodeRepository();
        var service = new TaxCodeService(taxCodes, accounts);
        var liability = new Account(
            OrganizationId,
            "2200",
            "Sales Tax Payable",
            AccountType.Liability,
            new CurrencyCode("USD"),
            new Money(0m, new CurrencyCode("USD")),
            null);

        await accounts.AddAsync(liability);

        var created = await service.CreateAsync(new CreateTaxCodeRequest(
            OrganizationId,
            "GST",
            "Goods and Services Tax",
            TaxType.Gst,
            0.1m,
            liability.Id,
            null));

        var result = await service.ArchiveAsync(OrganizationId, created.Value!.Id);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsArchived);
    }

    [Fact]
    public async Task ListByOrganizationAsync_excludes_archived_by_default()
    {
        var accounts = new InMemoryAccountRepository();
        var taxCodes = new InMemoryTaxCodeRepository();
        var service = new TaxCodeService(taxCodes, accounts);
        var liability = new Account(
            OrganizationId,
            "2200",
            "Sales Tax Payable",
            AccountType.Liability,
            new CurrencyCode("USD"),
            new Money(0m, new CurrencyCode("USD")),
            null);

        await accounts.AddAsync(liability);

        var active = await service.CreateAsync(new CreateTaxCodeRequest(
            OrganizationId,
            "GST",
            "Goods and Services Tax",
            TaxType.Gst,
            0.1m,
            liability.Id,
            null));

        var archived = await service.CreateAsync(new CreateTaxCodeRequest(
            OrganizationId,
            "OLD",
            "Old Tax",
            TaxType.None,
            0m,
            liability.Id,
            null));

        await service.ArchiveAsync(OrganizationId, archived.Value!.Id);

        var result = await service.ListByOrganizationAsync(OrganizationId);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal(active.Value!.Id, result.Value![0].Id);
    }

    private sealed class InMemoryTaxCodeRepository : ITaxCodeRepository
    {
        private readonly List<TaxCode> _taxCodes = new();

        public Task<TaxCode?> GetByIdAsync(
            Guid organizationId,
            Guid taxCodeId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_taxCodes.FirstOrDefault(t =>
                t.OrganizationId == organizationId && t.Id == taxCodeId));

        public Task<TaxCode?> GetByCodeAsync(
            Guid organizationId,
            string code,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_taxCodes.FirstOrDefault(t =>
                t.OrganizationId == organizationId &&
                string.Equals(t.Code, code, StringComparison.Ordinal)));

        public Task<IReadOnlyList<TaxCode>> ListByOrganizationAsync(
            Guid organizationId,
            bool includeArchived = false,
            CancellationToken cancellationToken = default)
        {
            var query = _taxCodes.Where(t => t.OrganizationId == organizationId);

            if (!includeArchived)
            {
                query = query.Where(t => !t.IsArchived);
            }

            IReadOnlyList<TaxCode> results = query.OrderBy(t => t.Code).ToList();
            return Task.FromResult(results);
        }

        public Task AddAsync(TaxCode taxCode, CancellationToken cancellationToken = default)
        {
            _taxCodes.Add(taxCode);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(TaxCode taxCode, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemoryAccountRepository : IAccountRepository
    {
        private readonly List<Account> _accounts = new();

        public Task<Account?> GetByIdAsync(
            Guid organizationId,
            Guid accountId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_accounts.FirstOrDefault(a =>
                a.OrganizationId == organizationId && a.Id == accountId));

        public Task<Account?> GetByCodeAsync(
            Guid organizationId,
            string code,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_accounts.FirstOrDefault(a =>
                a.OrganizationId == organizationId &&
                string.Equals(a.Code, code, StringComparison.Ordinal)));

        public Task<IReadOnlyList<Account>> ListByOrganizationAsync(
            Guid organizationId,
            bool includeArchived = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Account>>(_accounts.Where(a => a.OrganizationId == organizationId).ToList());

        public Task<IReadOnlyList<Account>> GetChildrenAsync(
            Guid organizationId,
            Guid parentAccountId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Account>>(Array.Empty<Account>());

        public Task<bool> HasActiveChildrenAsync(
            Guid organizationId,
            Guid accountId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task AddAsync(Account account, CancellationToken cancellationToken = default)
        {
            _accounts.Add(account);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Account account, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
