using Ledgerly.Application.Abstractions;
using Ledgerly.Application.Reporting;
using Ledgerly.Application.Reporting.Export;
using Ledgerly.Domain.Entities;
using Ledgerly.Domain.Enums;
using Ledgerly.Domain.ValueObjects;

namespace Ledgerly.Application.Tests;

public class ReportServicesTests
{
    private static readonly Guid OrganizationId = Guid.NewGuid();

    [Fact]
    public async Task TrialBalanceReportService_balances_debits_and_credits()
    {
        var accounts = new InMemoryAccountRepository();
        var journals = new InMemoryJournalRepository();
        var aggregator = new PostedJournalAggregator(accounts, journals);
        var service = new TrialBalanceReportService(aggregator);

        var cash = new Account(OrganizationId, "1000", "Cash", AccountType.Asset, new CurrencyCode("USD"), new Money(0m, new CurrencyCode("USD")), null);
        var equity = new Account(OrganizationId, "3000", "Equity", AccountType.Equity, new CurrencyCode("USD"), new Money(0m, new CurrencyCode("USD")), null);
        await accounts.AddAsync(cash);
        await accounts.AddAsync(equity);
        await PostJournalAsync(journals, new DateOnly(2026, 3, 1), (cash.Id, 1000m, 0m), (equity.Id, 0m, 1000m));

        var result = await service.GenerateAsync(
            OrganizationId,
            new TrialBalanceReportRequest(new DateOnly(2026, 3, 31)));

        Assert.True(result.IsSuccess);
        Assert.Equal(1000m, result.Value!.TotalDebits);
        Assert.Equal(1000m, result.Value.TotalCredits);
        Assert.Equal(2, result.Value.Lines.Count);
    }

    [Fact]
    public async Task ProfitAndLossReportService_calculates_net_income()
    {
        var accounts = new InMemoryAccountRepository();
        var journals = new InMemoryJournalRepository();
        var aggregator = new PostedJournalAggregator(accounts, journals);
        var service = new ProfitAndLossReportService(aggregator);

        var revenue = new Account(OrganizationId, "4000", "Revenue", AccountType.Income, new CurrencyCode("USD"), new Money(0m, new CurrencyCode("USD")), null);
        var expense = new Account(OrganizationId, "5000", "Expense", AccountType.Expense, new CurrencyCode("USD"), new Money(0m, new CurrencyCode("USD")), null);
        var cash = new Account(OrganizationId, "1000", "Cash", AccountType.Asset, new CurrencyCode("USD"), new Money(0m, new CurrencyCode("USD")), null);
        await accounts.AddAsync(revenue);
        await accounts.AddAsync(expense);
        await accounts.AddAsync(cash);
        await PostJournalAsync(journals, new DateOnly(2026, 3, 5), (cash.Id, 500m, 0m), (revenue.Id, 0m, 500m));
        await PostJournalAsync(journals, new DateOnly(2026, 3, 10), (expense.Id, 200m, 0m), (cash.Id, 0m, 200m));

        var result = await service.GenerateAsync(
            OrganizationId,
            new ProfitAndLossReportRequest(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31)));

        Assert.True(result.IsSuccess);
        Assert.Equal(500m, result.Value!.TotalIncome);
        Assert.Equal(200m, result.Value.TotalExpenses);
        Assert.Equal(300m, result.Value.NetIncome);
    }

    [Fact]
    public async Task ProfitAndLossReportService_fails_when_end_date_before_start_date()
    {
        var service = new ProfitAndLossReportService(
            new PostedJournalAggregator(new InMemoryAccountRepository(), new InMemoryJournalRepository()));

        var result = await service.GenerateAsync(
            OrganizationId,
            new ProfitAndLossReportRequest(new DateOnly(2026, 3, 31), new DateOnly(2026, 3, 1)));

        Assert.True(result.IsFailure);
        Assert.Contains("End date", result.Error);
    }

    [Fact]
    public async Task BalanceSheetReportService_groups_accounts_by_type()
    {
        var accounts = new InMemoryAccountRepository();
        var journals = new InMemoryJournalRepository();
        var aggregator = new PostedJournalAggregator(accounts, journals);
        var service = new BalanceSheetReportService(aggregator);

        var cash = new Account(OrganizationId, "1000", "Cash", AccountType.Asset, new CurrencyCode("USD"), new Money(0m, new CurrencyCode("USD")), null);
        var payable = new Account(OrganizationId, "2000", "Payable", AccountType.Liability, new CurrencyCode("USD"), new Money(0m, new CurrencyCode("USD")), null);
        var equity = new Account(OrganizationId, "3000", "Equity", AccountType.Equity, new CurrencyCode("USD"), new Money(0m, new CurrencyCode("USD")), null);
        await accounts.AddAsync(cash);
        await accounts.AddAsync(payable);
        await accounts.AddAsync(equity);
        await PostJournalAsync(journals, new DateOnly(2026, 3, 1), (cash.Id, 800m, 0m), (payable.Id, 0m, 300m), (equity.Id, 0m, 500m));

        var result = await service.GenerateAsync(
            OrganizationId,
            new BalanceSheetReportRequest(new DateOnly(2026, 3, 31)));

        Assert.True(result.IsSuccess);
        Assert.Equal(800m, result.Value!.TotalAssets);
        Assert.Equal(300m, result.Value.TotalLiabilities);
        Assert.Equal(500m, result.Value.TotalEquity);
        Assert.Single(result.Value.AssetLines);
        Assert.Single(result.Value.LiabilityLines);
        Assert.Single(result.Value.EquityLines);
    }

    [Fact]
    public async Task AgedReceivablesReportService_buckets_outstanding_invoices()
    {
        var invoices = new InMemoryInvoiceRepository();
        var contacts = new InMemoryContactRepository();
        var service = new AgedReceivablesReportService(invoices, contacts);

        var customer = new Contact(
            OrganizationId,
            ContactType.Customer,
            "Acme Corp",
            null,
            null,
            null,
            null,
            new CurrencyCode("USD"),
            PaymentTerms.Net30,
            null,
            null);

        await contacts.AddAsync(customer);

        var currentInvoice = CreateInvoice(
            customer.Id,
            "INV-CURRENT",
            new DateOnly(2026, 3, 1),
            new DateOnly(2026, 4, 15),
            100m,
            0m);

        var overdueInvoice = CreateInvoice(
            customer.Id,
            "INV-OVERDUE",
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 2, 1),
            250m,
            50m);

        await invoices.AddAsync(currentInvoice);
        await invoices.AddAsync(overdueInvoice);

        var result = await service.GenerateAsync(
            OrganizationId,
            new AgedReceivablesReportRequest(new DateOnly(2026, 3, 31)));

        Assert.True(result.IsSuccess);
        Assert.Equal(300m, result.Value!.TotalOutstanding);
        Assert.Equal(100m, result.Value.TotalCurrent);
        Assert.Equal(200m, result.Value.TotalDays31To60);
        Assert.Equal(2, result.Value.Lines.Count);

        var overdueLine = result.Value.Lines.Single(line => line.InvoiceNumber == "INV-OVERDUE");
        Assert.Equal(200m, overdueLine.BalanceDue);
        Assert.Equal(58, overdueLine.DaysOverdue);
    }

    [Fact]
    public void Exporters_generate_non_empty_files()
    {
        var pdfExporter = new ReportPdfExporter();
        var excelExporter = new ReportExcelExporter();

        var trialBalance = new TrialBalanceReportDto(
            new DateOnly(2026, 3, 31),
            100m,
            100m,
            new[]
            {
                new TrialBalanceLineDto(Guid.NewGuid(), "1000", "Cash", AccountType.Asset, 100m, 0m)
            });

        var profitAndLoss = new ProfitAndLossReportDto(
            new DateOnly(2026, 3, 1),
            new DateOnly(2026, 3, 31),
            500m,
            200m,
            300m,
            new[] { new ProfitAndLossLineDto(Guid.NewGuid(), "4000", "Revenue", AccountType.Income, 500m) },
            new[] { new ProfitAndLossLineDto(Guid.NewGuid(), "5000", "Expense", AccountType.Expense, 200m) });

        var balanceSheet = new BalanceSheetReportDto(
            new DateOnly(2026, 3, 31),
            800m,
            300m,
            500m,
            new[] { new BalanceSheetLineDto(Guid.NewGuid(), "1000", "Cash", AccountType.Asset, 800m) },
            new[] { new BalanceSheetLineDto(Guid.NewGuid(), "2000", "Payable", AccountType.Liability, 300m) },
            new[] { new BalanceSheetLineDto(Guid.NewGuid(), "3000", "Equity", AccountType.Equity, 500m) });

        var agedReceivables = new AgedReceivablesReportDto(
            new DateOnly(2026, 3, 31),
            200m,
            100m,
            100m,
            0m,
            0m,
            0m,
            new[]
            {
                new AgedReceivableLineDto(
                    Guid.NewGuid(),
                    "INV-1",
                    Guid.NewGuid(),
                    "Acme",
                    new DateOnly(2026, 3, 1),
                    new DateOnly(2026, 4, 1),
                    200m,
                    0m,
                    200m,
                    0,
                    200m,
                    0m,
                    0m,
                    0m,
                    0m)
            });

        Assert.NotEmpty(pdfExporter.ExportTrialBalance(trialBalance));
        Assert.NotEmpty(excelExporter.ExportTrialBalance(trialBalance));
        Assert.NotEmpty(pdfExporter.ExportProfitAndLoss(profitAndLoss));
        Assert.NotEmpty(excelExporter.ExportProfitAndLoss(profitAndLoss));
        Assert.NotEmpty(pdfExporter.ExportBalanceSheet(balanceSheet));
        Assert.NotEmpty(excelExporter.ExportBalanceSheet(balanceSheet));
        Assert.NotEmpty(pdfExporter.ExportAgedReceivables(agedReceivables));
        Assert.NotEmpty(excelExporter.ExportAgedReceivables(agedReceivables));
    }

    private static Invoice CreateInvoice(
        Guid customerId,
        string invoiceNumber,
        DateOnly issueDate,
        DateOnly dueDate,
        decimal totalAmount,
        decimal amountPaid)
    {
        var incomeAccountId = Guid.NewGuid();
        var receivableAccountId = Guid.NewGuid();
        var invoice = new Invoice(
            OrganizationId,
            customerId,
            invoiceNumber,
            issueDate,
            dueDate,
            new CurrencyCode("USD"),
            incomeAccountId,
            receivableAccountId);

        invoice.AddLine(
            "Service",
            1m,
            new Money(totalAmount, new CurrencyCode("USD")),
            null,
            new Money(0m, new CurrencyCode("USD")));

        invoice.Send(new Dictionary<Guid, decimal>());

        if (amountPaid > 0m)
        {
            invoice.RecordPayment(new Money(amountPaid, new CurrencyCode("USD")));
        }

        return invoice;
    }

    private static async Task PostJournalAsync(
        InMemoryJournalRepository repository,
        DateOnly entryDate,
        params (Guid AccountId, decimal Debit, decimal Credit)[] lines)
    {
        var entry = new JournalEntry(
            OrganizationId,
            entryDate,
            null,
            "Test entry",
            new CurrencyCode("USD"));

        foreach (var (accountId, debit, credit) in lines)
        {
            entry.AddLine(
                accountId,
                new Money(debit, new CurrencyCode("USD")),
                new Money(credit, new CurrencyCode("USD")));
        }

        entry.Post();
        await repository.AddAsync(entry);
    }

    private sealed class InMemoryAccountRepository : IAccountRepository
    {
        private readonly List<Account> _accounts = new();

        public Task<Account?> GetByIdAsync(Guid organizationId, Guid accountId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_accounts.FirstOrDefault(account => account.OrganizationId == organizationId && account.Id == accountId));

        public Task<Account?> GetByCodeAsync(Guid organizationId, string code, CancellationToken cancellationToken = default) =>
            Task.FromResult(_accounts.FirstOrDefault(account =>
                account.OrganizationId == organizationId &&
                string.Equals(account.Code, code, StringComparison.Ordinal)));

        public Task<IReadOnlyList<Account>> ListByOrganizationAsync(
            Guid organizationId,
            bool includeArchived = false,
            CancellationToken cancellationToken = default)
        {
            var query = _accounts.Where(account => account.OrganizationId == organizationId);

            if (!includeArchived)
            {
                query = query.Where(account => !account.IsArchived);
            }

            IReadOnlyList<Account> results = query.OrderBy(account => account.Code).ToList();
            return Task.FromResult(results);
        }

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

    private sealed class InMemoryJournalRepository : IJournalRepository
    {
        private readonly List<JournalEntry> _entries = new();

        public Task<JournalEntry?> GetByIdAsync(
            Guid organizationId,
            Guid journalEntryId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_entries.FirstOrDefault(entry =>
                entry.OrganizationId == organizationId && entry.Id == journalEntryId));

        public Task<IReadOnlyList<JournalEntry>> ListByOrganizationAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<JournalEntry>>(_entries.Where(entry => entry.OrganizationId == organizationId).ToList());

        public Task<IReadOnlyList<(JournalEntry Entry, JournalLine Line)>> ListPostedLinesByAccountAsync(
            Guid organizationId,
            Guid accountId,
            DateOnly startDate,
            DateOnly endDate,
            CancellationToken cancellationToken = default)
        {
            var lines = _entries
                .Where(entry =>
                    entry.OrganizationId == organizationId &&
                    entry.Status == JournalEntryStatus.Posted &&
                    entry.EntryDate >= startDate &&
                    entry.EntryDate <= endDate)
                .SelectMany(entry => entry.Lines
                    .Where(line => line.AccountId == accountId)
                    .Select(line => (entry, line)))
                .ToList();

            return Task.FromResult<IReadOnlyList<(JournalEntry Entry, JournalLine Line)>>(lines);
        }

        public Task<IReadOnlyList<(JournalEntry Entry, JournalLine Line)>> ListPostedLinesAsync(
            Guid organizationId,
            DateOnly? startDate,
            DateOnly endDate,
            CancellationToken cancellationToken = default)
        {
            var query = _entries.Where(entry =>
                entry.OrganizationId == organizationId &&
                entry.Status == JournalEntryStatus.Posted &&
                entry.EntryDate <= endDate);

            if (startDate.HasValue)
            {
                query = query.Where(entry => entry.EntryDate >= startDate.Value);
            }

            var lines = query
                .SelectMany(entry => entry.Lines.Select(line => (entry, line)))
                .ToList();

            return Task.FromResult<IReadOnlyList<(JournalEntry Entry, JournalLine Line)>>(lines);
        }

        public Task AddAsync(JournalEntry entry, CancellationToken cancellationToken = default)
        {
            _entries.Add(entry);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(JournalEntry entry, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemoryInvoiceRepository : IInvoiceRepository
    {
        private readonly List<Invoice> _invoices = new();

        public Task<Invoice?> GetByIdAsync(Guid organizationId, Guid invoiceId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_invoices.FirstOrDefault(invoice =>
                invoice.OrganizationId == organizationId && invoice.Id == invoiceId));

        public Task<IReadOnlyList<Invoice>> ListByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Invoice>>(_invoices.Where(invoice => invoice.OrganizationId == organizationId).ToList());

        public Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
        {
            _invoices.Add(invoice);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemoryContactRepository : IContactRepository
    {
        private readonly List<Contact> _contacts = new();

        public Task<Contact?> GetByIdAsync(Guid organizationId, Guid contactId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_contacts.FirstOrDefault(contact =>
                contact.OrganizationId == organizationId && contact.Id == contactId));

        public Task<IReadOnlyList<Contact>> ListByTypeAsync(
            Guid organizationId,
            ContactType contactType,
            bool includeArchived = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Contact>>(_contacts.Where(contact =>
                contact.OrganizationId == organizationId &&
                contact.ContactType == contactType).ToList());

        public Task AddAsync(Contact contact, CancellationToken cancellationToken = default)
        {
            _contacts.Add(contact);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Contact contact, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
