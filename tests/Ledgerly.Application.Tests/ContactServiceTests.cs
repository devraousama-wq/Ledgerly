using Ledgerly.Application.Abstractions;
using Ledgerly.Application.Contacts;
using Ledgerly.Domain.Entities;
using Ledgerly.Domain.Enums;

namespace Ledgerly.Application.Tests;

public class ContactServiceTests
{
    private static readonly Guid OrganizationId = Guid.NewGuid();

    [Fact]
    public async Task CreateAsync_returns_customer_when_valid()
    {
        var contactRepository = new InMemoryContactRepository();
        var accountRepository = new InMemoryAccountRepository();
        var service = new ContactService(contactRepository, accountRepository);

        var result = await service.CreateAsync(new CreateContactRequest(
            OrganizationId,
            ContactType.Customer,
            "Acme Corp",
            "billing@acme.com",
            "+1-555-0100",
            new AddressDto("100 Main St", null, "Springfield", "IL", "62701", "US"),
            null,
            "USD",
            PaymentTerms.Net30,
            null,
            null));

        Assert.True(result.IsSuccess);
        Assert.Equal("Acme Corp", result.Value!.Name);
        Assert.Equal(ContactType.Customer, result.Value.ContactType);
        Assert.NotNull(result.Value.BillingAddress);
    }

    [Fact]
    public async Task CreateAsync_returns_vendor_with_expense_account()
    {
        var contactRepository = new InMemoryContactRepository();
        var accountRepository = new InMemoryAccountRepository();
        var expenseAccount = new Account(
            OrganizationId,
            "6000",
            "Office Supplies",
            AccountType.Expense,
            new Domain.ValueObjects.CurrencyCode("USD"),
            new Domain.ValueObjects.Money(0m, new Domain.ValueObjects.CurrencyCode("USD")),
            null);
        accountRepository.Seed(expenseAccount);

        var service = new ContactService(contactRepository, accountRepository);

        var result = await service.CreateAsync(new CreateContactRequest(
            OrganizationId,
            ContactType.Vendor,
            "Office Supplies Inc",
            null,
            null,
            null,
            null,
            "USD",
            PaymentTerms.Net30,
            "TAX-123",
            expenseAccount.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal(expenseAccount.Id, result.Value!.DefaultExpenseAccountId);
    }

    [Fact]
    public async Task CreateAsync_fails_when_customer_has_expense_account()
    {
        var service = new ContactService(
            new InMemoryContactRepository(),
            new InMemoryAccountRepository());

        var result = await service.CreateAsync(new CreateContactRequest(
            OrganizationId,
            ContactType.Customer,
            "Acme Corp",
            null,
            null,
            null,
            null,
            "USD",
            PaymentTerms.Net30,
            null,
            Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Contains("expense account", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_fails_when_expense_account_not_found()
    {
        var service = new ContactService(
            new InMemoryContactRepository(),
            new InMemoryAccountRepository());

        var result = await service.CreateAsync(new CreateContactRequest(
            OrganizationId,
            ContactType.Vendor,
            "Office Supplies Inc",
            null,
            null,
            null,
            null,
            "USD",
            PaymentTerms.Net30,
            null,
            Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetByIdAsync_returns_contact()
    {
        var contactRepository = new InMemoryContactRepository();
        var service = new ContactService(contactRepository, new InMemoryAccountRepository());

        var created = await service.CreateAsync(new CreateContactRequest(
            OrganizationId,
            ContactType.Customer,
            "Acme Corp",
            null,
            null,
            null,
            null,
            "USD",
            PaymentTerms.Net30,
            null,
            null));

        var result = await service.GetByIdAsync(OrganizationId, created.Value!.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(created.Value.Id, result.Value!.Id);
    }

    [Fact]
    public async Task ListByTypeAsync_filters_by_contact_type()
    {
        var contactRepository = new InMemoryContactRepository();
        var service = new ContactService(contactRepository, new InMemoryAccountRepository());

        await service.CreateAsync(new CreateContactRequest(
            OrganizationId,
            ContactType.Customer,
            "Acme Corp",
            null,
            null,
            null,
            null,
            "USD",
            PaymentTerms.Net30,
            null,
            null));

        await service.CreateAsync(new CreateContactRequest(
            OrganizationId,
            ContactType.Vendor,
            "Office Supplies Inc",
            null,
            null,
            null,
            null,
            "USD",
            PaymentTerms.Net30,
            null,
            null));

        var customers = await service.ListByTypeAsync(OrganizationId, ContactType.Customer);
        var vendors = await service.ListByTypeAsync(OrganizationId, ContactType.Vendor);

        Assert.True(customers.IsSuccess);
        Assert.True(vendors.IsSuccess);
        Assert.Single(customers.Value!);
        Assert.Single(vendors.Value!);
        Assert.Equal("Acme Corp", customers.Value![0].Name);
        Assert.Equal("Office Supplies Inc", vendors.Value![0].Name);
    }

    [Fact]
    public async Task ListByTypeAsync_excludes_archived_by_default()
    {
        var contactRepository = new InMemoryContactRepository();
        var service = new ContactService(contactRepository, new InMemoryAccountRepository());

        var active = await service.CreateAsync(new CreateContactRequest(
            OrganizationId,
            ContactType.Customer,
            "Acme Corp",
            null,
            null,
            null,
            null,
            "USD",
            PaymentTerms.Net30,
            null,
            null));

        var archived = await service.CreateAsync(new CreateContactRequest(
            OrganizationId,
            ContactType.Customer,
            "Old Customer",
            null,
            null,
            null,
            null,
            "USD",
            PaymentTerms.Net30,
            null,
            null));

        await service.ArchiveAsync(OrganizationId, archived.Value!.Id);

        var result = await service.ListByTypeAsync(OrganizationId, ContactType.Customer);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal(active.Value!.Id, result.Value![0].Id);
    }

    [Fact]
    public async Task UpdateAsync_changes_contact()
    {
        var contactRepository = new InMemoryContactRepository();
        var service = new ContactService(contactRepository, new InMemoryAccountRepository());

        var created = await service.CreateAsync(new CreateContactRequest(
            OrganizationId,
            ContactType.Customer,
            "Acme Corp",
            null,
            null,
            null,
            null,
            "USD",
            PaymentTerms.Net30,
            null,
            null));

        var result = await service.UpdateAsync(
            OrganizationId,
            created.Value!.Id,
            new UpdateContactRequest(
                "Acme International",
                "info@acme.com",
                null,
                null,
                null,
                "USD",
                PaymentTerms.Net60,
                null,
                null));

        Assert.True(result.IsSuccess);
        Assert.Equal("Acme International", result.Value!.Name);
        Assert.Equal(PaymentTerms.Net60, result.Value.PaymentTerms);
    }

    [Fact]
    public async Task ArchiveAsync_marks_contact_archived()
    {
        var contactRepository = new InMemoryContactRepository();
        var service = new ContactService(contactRepository, new InMemoryAccountRepository());

        var created = await service.CreateAsync(new CreateContactRequest(
            OrganizationId,
            ContactType.Customer,
            "Acme Corp",
            null,
            null,
            null,
            null,
            "USD",
            PaymentTerms.Net30,
            null,
            null));

        var result = await service.ArchiveAsync(OrganizationId, created.Value!.Id);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsArchived);
    }

    private sealed class InMemoryContactRepository : IContactRepository
    {
        private readonly List<Contact> _contacts = new();

        public Task<Contact?> GetByIdAsync(
            Guid organizationId,
            Guid contactId,
            CancellationToken cancellationToken = default)
        {
            var contact = _contacts.FirstOrDefault(c =>
                c.OrganizationId == organizationId && c.Id == contactId);

            return Task.FromResult(contact);
        }

        public Task<IReadOnlyList<Contact>> ListByTypeAsync(
            Guid organizationId,
            ContactType contactType,
            bool includeArchived = false,
            CancellationToken cancellationToken = default)
        {
            var query = _contacts
                .Where(c => c.OrganizationId == organizationId && c.ContactType == contactType);

            if (!includeArchived)
            {
                query = query.Where(c => !c.IsArchived);
            }

            IReadOnlyList<Contact> results = query
                .OrderBy(c => c.Name)
                .ToList();

            return Task.FromResult(results);
        }

        public Task AddAsync(Contact contact, CancellationToken cancellationToken = default)
        {
            _contacts.Add(contact);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Contact contact, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemoryAccountRepository : IAccountRepository
    {
        private readonly List<Account> _accounts = new();

        public void Seed(Account account) => _accounts.Add(account);

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
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Account?>(null);

        public Task<IReadOnlyList<Account>> ListByOrganizationAsync(
            Guid organizationId,
            bool includeArchived = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Account>>(Array.Empty<Account>());

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

        public Task AddAsync(Account account, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UpdateAsync(Account account, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
