using System.Text.Json;
using Ledgerly.Application.Abstractions;
using Ledgerly.Application.Bills;
using Ledgerly.Application.Invoices;
using Ledgerly.Application.Journals;
using Ledgerly.Application.Recurring;
using Ledgerly.Domain.Entities;
using Ledgerly.Domain.Enums;
using Ledgerly.Domain.ValueObjects;

namespace Ledgerly.Application.Tests;

public class RecurringScheduleServiceTests
{
    static readonly Guid Org = Guid.NewGuid();

    [Fact]
    public async Task CreateAsync_sets_next_run_from_cron()
    {
        var repo = new InMemoryRecurringScheduleRepository();
        var service = new RecurringScheduleService(repo);
        var start = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero);
        var result = await service.CreateAsync(new CreateRecurringScheduleRequest(Org, "Monthly invoice", "0 0 1 * *", start, RecurringTransactionType.Invoice, "{\"lines\":[]}"));
        Assert.True(result.IsSuccess);
        Assert.Equal(new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero), result.Value!.NextRunUtc);
    }

    [Fact]
    public void PreviewAsync_returns_occurrences()
    {
        var service = new RecurringScheduleService(new InMemoryRecurringScheduleRepository());
        var from = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero);
        var result = service.PreviewAsync(new PreviewRecurringScheduleRequest("0 0 1 * *", from, 3));
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value!.Count);
    }

    [Fact]
    public async Task PauseAsync_pauses_schedule()
    {
        var repo = new InMemoryRecurringScheduleRepository();
        var service = new RecurringScheduleService(repo);
        var created = await service.CreateAsync(new CreateRecurringScheduleRequest(Org, "Monthly invoice", "0 0 1 * *", DateTimeOffset.UtcNow, RecurringTransactionType.Journal, "{\"lines\":[]}"));
        var paused = await service.PauseAsync(Org, created.Value!.Id);
        Assert.True(paused.IsSuccess);
        Assert.True(paused.Value!.IsPaused);
    }
}

public class RecurringTransactionProcessorTests
{
    static readonly Guid Org = Guid.NewGuid();
    static readonly CurrencyCode Usd = new("USD");

    [Fact]
    public async Task ProcessDueAsync_generates_invoice_idempotently()
    {
        var recurringRepo = new InMemoryRecurringScheduleRepository();
        var invoiceRepo = new RecurringTestInvoiceRepository();
        var contactRepo = new RecurringTestContactRepository();
        var accountRepo = new RecurringTestAccountRepository();
        var journalRepo = new RecurringTestJournalRepository();
        var taxRepo = new RecurringTestTaxCodeRepository();
        var customer = new Contact(Org, ContactType.Customer, "Acme", null, null, null, null, Usd, PaymentTerms.Net30, null, null);
        await contactRepo.AddAsync(customer);
        var income = new Account(Org, "4000", "Rev", AccountType.Income, Usd, new Money(0m, Usd), null);
        var receivable = new Account(Org, "1200", "AR", AccountType.Asset, Usd, new Money(0m, Usd), null);
        await accountRepo.AddAsync(income);
        await accountRepo.AddAsync(receivable);
        var template = new RecurringInvoiceTemplate(customer.Id, "REC-INV", "USD", income.Id, receivable.Id, 30, new[] { new RecurringLineTemplate("Service", 1m, 100m, null, 0m) });
        var schedule = new RecurringSchedule(Org, "Monthly", "0 0 1 * *", new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero), RecurringTransactionType.Invoice, JsonSerializer.Serialize(template));
        await recurringRepo.AddAsync(schedule);
        var journalService = new JournalService(journalRepo, accountRepo);
        var invoiceService = new InvoiceService(invoiceRepo, contactRepo, accountRepo, taxRepo, journalService);
        var billService = new BillService(new RecurringTestBillRepository(), contactRepo, accountRepo, taxRepo, journalService);
        var processor = new RecurringTransactionProcessor(recurringRepo, invoiceService, billService, journalService);
        var dueAt = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);
        var first = await processor.ProcessDueAsync(dueAt);
        var second = await processor.ProcessDueAsync(dueAt);
        Assert.Equal(1, first);
        Assert.Equal(0, second);
        Assert.Single(await invoiceRepo.ListByOrganizationAsync(Org));
        Assert.Single(recurringRepo.Runs);
    }

    [Fact]
    public async Task ProcessDueAsync_generates_journal()
    {
        var recurringRepo = new InMemoryRecurringScheduleRepository();
        var accountRepo = new RecurringTestAccountRepository();
        var journalRepo = new RecurringTestJournalRepository();
        var cash = new Account(Org, "1000", "Cash", AccountType.Asset, Usd, new Money(0m, Usd), null);
        var expense = new Account(Org, "6000", "Expense", AccountType.Expense, Usd, new Money(0m, Usd), null);
        await accountRepo.AddAsync(cash);
        await accountRepo.AddAsync(expense);
        var template = new RecurringJournalTemplate("REC-JE", "Monthly allocation", "USD", new[] { new RecurringJournalLineTemplate(expense.Id, 50m, 0m, null), new RecurringJournalLineTemplate(cash.Id, 0m, 50m, null) });
        var schedule = new RecurringSchedule(Org, "Allocation", "0 0 1 * *", new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero), RecurringTransactionType.Journal, JsonSerializer.Serialize(template));
        await recurringRepo.AddAsync(schedule);
        var journalService = new JournalService(journalRepo, accountRepo);
        var invoiceService = new InvoiceService(new RecurringTestInvoiceRepository(), new RecurringTestContactRepository(), accountRepo, new RecurringTestTaxCodeRepository(), journalService);
        var billService = new BillService(new RecurringTestBillRepository(), new RecurringTestContactRepository(), accountRepo, new RecurringTestTaxCodeRepository(), journalService);
        var processor = new RecurringTransactionProcessor(recurringRepo, invoiceService, billService, journalService);
        var processed = await processor.ProcessDueAsync(new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero));
        Assert.Equal(1, processed);
        var journals = await journalRepo.ListByOrganizationAsync(Org);
        Assert.Single(journals);
        Assert.Equal(JournalEntryStatus.Posted, journals[0].Status);
    }

    [Fact]
    public async Task ProcessDueAsync_generates_bill()
    {
        var recurringRepo = new InMemoryRecurringScheduleRepository();
        var billRepo = new RecurringTestBillRepository();
        var contactRepo = new RecurringTestContactRepository();
        var accountRepo = new RecurringTestAccountRepository();
        var journalRepo = new RecurringTestJournalRepository();
        var taxRepo = new RecurringTestTaxCodeRepository();
        var vendor = new Contact(Org, ContactType.Vendor, "Supplier", null, null, null, null, Usd, PaymentTerms.Net30, null, null);
        await contactRepo.AddAsync(vendor);
        var expense = new Account(Org, "6000", "Expense", AccountType.Expense, Usd, new Money(0m, Usd), null);
        var payable = new Account(Org, "2000", "AP", AccountType.Liability, Usd, new Money(0m, Usd), null);
        await accountRepo.AddAsync(expense);
        await accountRepo.AddAsync(payable);
        var template = new RecurringBillTemplate(vendor.Id, "REC-BILL", "USD", expense.Id, payable.Id, 14, new[] { new RecurringLineTemplate("Supplies", 1m, 75m, null, 0m) });
        var schedule = new RecurringSchedule(Org, "Monthly bill", "0 0 1 * *", new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero), RecurringTransactionType.Bill, JsonSerializer.Serialize(template));
        await recurringRepo.AddAsync(schedule);
        var journalService = new JournalService(journalRepo, accountRepo);
        var billService = new BillService(billRepo, contactRepo, accountRepo, taxRepo, journalService);
        var invoiceService = new InvoiceService(new RecurringTestInvoiceRepository(), contactRepo, accountRepo, taxRepo, journalService);
        var processor = new RecurringTransactionProcessor(recurringRepo, invoiceService, billService, journalService);
        var processed = await processor.ProcessDueAsync(new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero));
        Assert.Equal(1, processed);
        var bills = await billRepo.ListByOrganizationAsync(Org);
        Assert.Single(bills);
        Assert.Equal(BillStatus.Approved, bills[0].Status);
    }
}

sealed class InMemoryRecurringScheduleRepository : IRecurringScheduleRepository
{
    readonly List<RecurringSchedule> _schedules = new();
    public readonly List<RecurringScheduleRun> Runs = new();

    public Task<RecurringSchedule?> GetByIdAsync(Guid organizationId, Guid scheduleId, CancellationToken cancellationToken = default)
        => Task.FromResult(_schedules.FirstOrDefault(x => x.OrganizationId == organizationId && x.Id == scheduleId));

    public Task<IReadOnlyList<RecurringSchedule>> ListByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<RecurringSchedule>>(_schedules.Where(x => x.OrganizationId == organizationId).ToList());

    public Task<IReadOnlyList<RecurringSchedule>> ListDueAsync(DateTimeOffset asOfUtc, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<RecurringSchedule>>(_schedules.Where(x => !x.IsPaused && x.NextRunUtc <= asOfUtc).ToList());

    public Task AddAsync(RecurringSchedule schedule, CancellationToken cancellationToken = default)
    {
        _schedules.Add(schedule);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(RecurringSchedule schedule, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<RecurringScheduleRun?> GetRunByOccurrenceAsync(Guid scheduleId, DateTimeOffset occurrenceUtc, CancellationToken cancellationToken = default)
        => Task.FromResult(Runs.FirstOrDefault(x => x.RecurringScheduleId == scheduleId && x.OccurrenceUtc == occurrenceUtc));

    public Task AddRunAsync(RecurringScheduleRun run, CancellationToken cancellationToken = default)
    {
        Runs.Add(run);
        return Task.CompletedTask;
    }
}

sealed class RecurringTestBillRepository : IBillRepository
{
    readonly List<Bill> _items = new();

    public Task<Bill?> GetByIdAsync(Guid organizationId, Guid billId, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.FirstOrDefault(x => x.OrganizationId == organizationId && x.Id == billId));

    public Task<IReadOnlyList<Bill>> ListByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Bill>>(_items.Where(x => x.OrganizationId == organizationId).ToList());

    public Task AddAsync(Bill bill, CancellationToken cancellationToken = default)
    {
        _items.Add(bill);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Bill bill, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

sealed class RecurringTestInvoiceRepository : IInvoiceRepository
{
    readonly List<Invoice> _items = new();

    public Task<Invoice?> GetByIdAsync(Guid organizationId, Guid invoiceId, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.FirstOrDefault(x => x.OrganizationId == organizationId && x.Id == invoiceId));

    public Task<IReadOnlyList<Invoice>> ListByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Invoice>>(_items.Where(x => x.OrganizationId == organizationId).ToList());

    public Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        _items.Add(invoice);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

sealed class RecurringTestTaxCodeRepository : ITaxCodeRepository
{
    readonly List<TaxCode> _items = new();

    public Task<TaxCode?> GetByIdAsync(Guid organizationId, Guid taxCodeId, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.FirstOrDefault(x => x.OrganizationId == organizationId && x.Id == taxCodeId));

    public Task<TaxCode?> GetByCodeAsync(Guid organizationId, string code, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.FirstOrDefault(x => x.OrganizationId == organizationId && x.Code == code));

    public Task<IReadOnlyList<TaxCode>> ListByOrganizationAsync(Guid organizationId, bool includeArchived = false, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<TaxCode>>(_items.Where(x => x.OrganizationId == organizationId).ToList());

    public Task AddAsync(TaxCode taxCode, CancellationToken cancellationToken = default)
    {
        _items.Add(taxCode);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(TaxCode taxCode, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

sealed class RecurringTestContactRepository : IContactRepository
{
    readonly List<Contact> _items = new();

    public Task<Contact?> GetByIdAsync(Guid organizationId, Guid contactId, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.FirstOrDefault(x => x.OrganizationId == organizationId && x.Id == contactId));

    public Task<IReadOnlyList<Contact>> ListByTypeAsync(Guid organizationId, ContactType contactType, bool includeArchived = false, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Contact>>(_items.Where(x => x.OrganizationId == organizationId && x.ContactType == contactType).ToList());

    public Task AddAsync(Contact contact, CancellationToken cancellationToken = default)
    {
        _items.Add(contact);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Contact contact, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

sealed class RecurringTestJournalRepository : IJournalRepository
{
    readonly List<JournalEntry> _items = new();

    public Task<JournalEntry?> GetByIdAsync(Guid organizationId, Guid journalEntryId, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.FirstOrDefault(x => x.OrganizationId == organizationId && x.Id == journalEntryId));

    public Task<IReadOnlyList<JournalEntry>> ListByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<JournalEntry>>(_items.Where(x => x.OrganizationId == organizationId).ToList());

    public Task<IReadOnlyList<(JournalEntry Entry, JournalLine Line)>> ListPostedLinesByAccountAsync(Guid organizationId, Guid accountId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<(JournalEntry Entry, JournalLine Line)>>(_items.Where(x => x.OrganizationId == organizationId && x.Status == JournalEntryStatus.Posted && x.EntryDate >= startDate && x.EntryDate <= endDate).SelectMany(x => x.Lines.Where(l => l.AccountId == accountId).Select(l => (x, l))).ToList());

    public Task AddAsync(JournalEntry entry, CancellationToken cancellationToken = default)
    {
        _items.Add(entry);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(JournalEntry entry, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

sealed class RecurringTestAccountRepository : IAccountRepository
{
    readonly List<Account> _items = new();

    public Task<Account?> GetByIdAsync(Guid organizationId, Guid accountId, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.FirstOrDefault(x => x.OrganizationId == organizationId && x.Id == accountId));

    public Task<Account?> GetByCodeAsync(Guid organizationId, string code, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.FirstOrDefault(x => x.OrganizationId == organizationId && x.Code == code));

    public Task<IReadOnlyList<Account>> ListByOrganizationAsync(Guid organizationId, bool includeArchived = false, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Account>>(_items.Where(x => x.OrganizationId == organizationId).ToList());

    public Task<IReadOnlyList<Account>> GetChildrenAsync(Guid organizationId, Guid parentAccountId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Account>>(Array.Empty<Account>());

    public Task<bool> HasActiveChildrenAsync(Guid organizationId, Guid accountId, CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public Task AddAsync(Account account, CancellationToken cancellationToken = default)
    {
        _items.Add(account);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Account account, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
