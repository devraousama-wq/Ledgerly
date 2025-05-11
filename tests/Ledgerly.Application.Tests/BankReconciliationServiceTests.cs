using Ledgerly.Application.Abstractions;
using Ledgerly.Application.Journals;
using Ledgerly.Application.Reconciliation;
using Ledgerly.Domain.Entities;
using Ledgerly.Domain.Enums;
using Ledgerly.Domain.ValueObjects;

namespace Ledgerly.Application.Tests;

public class BankReconciliationServiceTests
{
    private static readonly Guid OrganizationId = Guid.NewGuid();

    [Fact]
    public void CsvBankStatementImporter_parses_mapped_rows()
    {
        var csv = """
            Date,Amount,Description,Reference
            2026-03-05,150.00,Client Payment,PAY-1
            2026-03-10,-40.00,Office Supplies,SUP-2
            """;

        var lines = CsvBankStatementImporter.Parse(
            csv,
            new CsvColumnMapping(0, 1, 2, 3, "yyyy-MM-dd", true));

        Assert.Equal(2, lines.Count);
        Assert.Equal(150m, lines[0].Amount);
        Assert.Equal("PAY-1", lines[0].Reference);
        Assert.Equal(-40m, lines[1].Amount);
    }

    [Fact]
    public void OfxBankStatementImporter_parses_statement_transactions()
    {
        var ofx = """
            <STMTTRN>
            <TRNTYPE>CREDIT
            <DTPOSTED>20260305120000
            <TRNAMT>150.00
            <NAME>Client Payment
            <FITID>abc123
            </STMTTRN>
            """;

        var lines = OfxBankStatementImporter.Parse(ofx);

        Assert.Single(lines);
        Assert.Equal(new DateOnly(2026, 3, 5), lines[0].TransactionDate);
        Assert.Equal(150m, lines[0].Amount);
        Assert.Equal("abc123", lines[0].Reference);
    }

    [Fact]
    public async Task AutoMatchAsync_matches_posted_journal_lines()
    {
        var accounts = new InMemoryAccountRepository();
        var journals = new InMemoryJournalRepository();
        var statements = new InMemoryBankStatementRepository();
        var service = CreateService(accounts, journals, statements);
        var bank = await SeedBankAccountAsync(accounts);
        var offset = await SeedOffsetAccountAsync(accounts);
        var journalLineId = await SeedPostedDepositAsync(journals, bank, offset, 100m);
        var created = await service.CreateAsync(new CreateBankStatementRequest(
            OrganizationId,
            bank.Id,
            new DateOnly(2026, 3, 1),
            new DateOnly(2026, 3, 31),
            1000m,
            1100m,
            "USD"));

        await service.ImportCsvAsync(
            OrganizationId,
            created.Value!.Id,
            new ImportCsvBankStatementRequest(
                "2026-03-05,100.00,Deposit,REF-1",
                new CsvColumnMapping(0, 1, 2, 3, "yyyy-MM-dd", false)));

        var matched = await service.AutoMatchAsync(OrganizationId, created.Value.Id);

        Assert.True(matched.IsSuccess);
        Assert.Equal(BankStatementLineStatus.Matched, matched.Value!.Lines.Single().Status);
        Assert.Equal(journalLineId, matched.Value.Lines.Single().MatchedJournalLineId);
    }

    [Fact]
    public async Task ReconcileAsync_locks_balanced_statement()
    {
        var accounts = new InMemoryAccountRepository();
        var journals = new InMemoryJournalRepository();
        var statements = new InMemoryBankStatementRepository();
        var service = CreateService(accounts, journals, statements);
        var bank = await SeedBankAccountAsync(accounts);
        var offset = await SeedOffsetAccountAsync(accounts);
        await SeedPostedDepositAsync(journals, bank, offset, 100m);
        var created = await service.CreateAsync(new CreateBankStatementRequest(
            OrganizationId,
            bank.Id,
            new DateOnly(2026, 3, 1),
            new DateOnly(2026, 3, 31),
            1000m,
            1100m,
            "USD"));

        await service.ImportCsvAsync(
            OrganizationId,
            created.Value!.Id,
            new ImportCsvBankStatementRequest(
                "2026-03-05,100.00,Deposit,REF-1",
                new CsvColumnMapping(0, 1, 2, 3, "yyyy-MM-dd", false)));

        await service.AutoMatchAsync(OrganizationId, created.Value.Id);
        var reconciled = await service.ReconcileAsync(OrganizationId, created.Value.Id);

        Assert.True(reconciled.IsSuccess);
        Assert.Equal(BankStatementStatus.Reconciled, reconciled.Value!.Status);
    }

    [Fact]
    public async Task CreateEntryFromLineAsync_posts_missing_journal()
    {
        var accounts = new InMemoryAccountRepository();
        var journals = new InMemoryJournalRepository();
        var statements = new InMemoryBankStatementRepository();
        var service = CreateService(accounts, journals, statements);
        var bank = await SeedBankAccountAsync(accounts);
        var offset = await SeedOffsetAccountAsync(accounts);
        var created = await service.CreateAsync(new CreateBankStatementRequest(
            OrganizationId,
            bank.Id,
            new DateOnly(2026, 3, 1),
            new DateOnly(2026, 3, 31),
            1000m,
            1100m,
            "USD"));

        var withLine = await service.AddLineAsync(
            OrganizationId,
            created.Value!.Id,
            new AddBankStatementLineRequest(new DateOnly(2026, 3, 7), 100m, "Deposit", "DEP-1"));

        var lineId = withLine.Value!.Lines.Single().Id;
        var result = await service.CreateEntryFromLineAsync(
            OrganizationId,
            created.Value.Id,
            lineId,
            new CreateBankEntryRequest(offset.Id, "Imported deposit"));

        Assert.True(result.IsSuccess);
        Assert.Equal(BankStatementLineStatus.EntryCreated, result.Value!.Lines.Single().Status);
        Assert.NotNull(result.Value.Lines.Single().CreatedJournalEntryId);
    }

    [Fact]
    public async Task ReconciliationReportService_summarizes_period()
    {
        var accounts = new InMemoryAccountRepository();
        var journals = new InMemoryJournalRepository();
        var statements = new InMemoryBankStatementRepository();
        var service = CreateService(accounts, journals, statements);
        var reportService = new ReconciliationReportService(statements);
        var bank = await SeedBankAccountAsync(accounts);
        var offset = await SeedOffsetAccountAsync(accounts);
        await SeedPostedDepositAsync(journals, bank, offset, 100m);
        var created = await service.CreateAsync(new CreateBankStatementRequest(
            OrganizationId,
            bank.Id,
            new DateOnly(2026, 3, 1),
            new DateOnly(2026, 3, 31),
            1000m,
            1100m,
            "USD"));

        await service.ImportCsvAsync(
            OrganizationId,
            created.Value!.Id,
            new ImportCsvBankStatementRequest(
                "2026-03-05,100.00,Deposit,REF-1",
                new CsvColumnMapping(0, 1, 2, 3, "yyyy-MM-dd", false)));

        await service.AutoMatchAsync(OrganizationId, created.Value.Id);

        var report = await reportService.GenerateAsync(
            OrganizationId,
            new ReconciliationReportRequest(bank.Id, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31)));

        Assert.True(report.IsSuccess);
        Assert.Equal(1, report.Value!.MatchedLineCount);
        Assert.Equal(100m, report.Value.MatchedAmount);
        Assert.Equal(1100m, report.Value.ClosingBalance);
    }

    private static BankReconciliationService CreateService(
        InMemoryAccountRepository accounts,
        InMemoryJournalRepository journals,
        InMemoryBankStatementRepository statements) =>
        new(statements, accounts, journals, new JournalService(journals, accounts, new InMemoryFiscalPeriodRepository()));

    private static async Task<Account> SeedBankAccountAsync(InMemoryAccountRepository accounts)
    {
        var bank = new Account(
            OrganizationId,
            "1000",
            "Checking",
            AccountType.Asset,
            new CurrencyCode("USD"),
            new Money(0m, new CurrencyCode("USD")),
            null);

        await accounts.AddAsync(bank);
        return bank;
    }

    private static async Task<Account> SeedOffsetAccountAsync(InMemoryAccountRepository accounts)
    {
        var offset = new Account(
            OrganizationId,
            "4000",
            "Income",
            AccountType.Income,
            new CurrencyCode("USD"),
            new Money(0m, new CurrencyCode("USD")),
            null);

        await accounts.AddAsync(offset);
        return offset;
    }

    private static async Task<Guid> SeedPostedDepositAsync(
        InMemoryJournalRepository journals,
        Account bank,
        Account offset,
        decimal amount)
    {
        var entry = new JournalEntry(
            OrganizationId,
            new DateOnly(2026, 3, 5),
            "DEP-1",
            "Deposit",
            bank.CurrencyCode);

        var line = entry.AddLine(bank.Id, new Money(amount, bank.CurrencyCode), new Money(0m, bank.CurrencyCode), "Deposit");
        entry.AddLine(offset.Id, new Money(0m, bank.CurrencyCode), new Money(amount, bank.CurrencyCode), "Deposit");
        entry.Post();
        await journals.AddAsync(entry);
        return line.Id;
    }

    private sealed class InMemoryBankStatementRepository : IBankStatementRepository
    {
        private readonly List<BankStatement> _statements = new();

        public Task<BankStatement?> GetByIdAsync(Guid organizationId, Guid statementId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_statements.FirstOrDefault(statement =>
                statement.OrganizationId == organizationId && statement.Id == statementId));

        public Task<IReadOnlyList<BankStatement>> ListByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BankStatement>>(_statements.Where(statement => statement.OrganizationId == organizationId).ToList());

        public Task<IReadOnlyList<Guid>> ListMatchedJournalLineIdsAsync(
            Guid organizationId,
            Guid bankAccountId,
            CancellationToken cancellationToken = default)
        {
            var ids = _statements
                .Where(statement => statement.OrganizationId == organizationId && statement.BankAccountId == bankAccountId)
                .SelectMany(statement => statement.Lines)
                .Where(line => line.MatchedJournalLineId.HasValue)
                .Select(line => line.MatchedJournalLineId!.Value)
                .Distinct()
                .ToList();

            return Task.FromResult<IReadOnlyList<Guid>>(ids);
        }

        public Task AddAsync(BankStatement statement, CancellationToken cancellationToken = default)
        {
            _statements.Add(statement);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(BankStatement statement, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemoryJournalRepository : IJournalRepository
    {
        private readonly List<JournalEntry> _entries = new();

        public Task<JournalEntry?> GetByIdAsync(Guid organizationId, Guid journalEntryId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_entries.FirstOrDefault(entry => entry.OrganizationId == organizationId && entry.Id == journalEntryId));

        public Task<IReadOnlyList<JournalEntry>> ListByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
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

    private sealed class InMemoryAccountRepository : IAccountRepository
    {
        private readonly List<Account> _accounts = new();

        public Task<Account?> GetByIdAsync(Guid organizationId, Guid accountId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_accounts.FirstOrDefault(account => account.OrganizationId == organizationId && account.Id == accountId));

        public Task<Account?> GetByCodeAsync(Guid organizationId, string code, CancellationToken cancellationToken = default) =>
            Task.FromResult(_accounts.FirstOrDefault(account => account.OrganizationId == organizationId && account.Code == code));

        public Task<IReadOnlyList<Account>> ListByOrganizationAsync(Guid organizationId, bool includeArchived = false, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Account>>(_accounts.Where(account => account.OrganizationId == organizationId).ToList());

        public Task<IReadOnlyList<Account>> GetChildrenAsync(Guid organizationId, Guid parentAccountId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Account>>(Array.Empty<Account>());

        public Task<bool> HasActiveChildrenAsync(Guid organizationId, Guid accountId, CancellationToken cancellationToken = default) =>
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
