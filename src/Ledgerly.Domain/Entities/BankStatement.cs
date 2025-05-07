using Ledgerly.Domain.Common;
using Ledgerly.Domain.Enums;
using Ledgerly.Domain.Exceptions;
using Ledgerly.Domain.ValueObjects;

namespace Ledgerly.Domain.Entities;

public sealed class BankStatement : OrganizationScopedEntity
{
    private readonly List<BankStatementLine> _lines = new();

    public Guid BankAccountId { get; private set; }

    public DateOnly PeriodStart { get; private set; }

    public DateOnly PeriodEnd { get; private set; }

    public Money OpeningBalance { get; private set; }

    public Money ClosingBalance { get; private set; }

    public BankStatementStatus Status { get; private set; }

    public IReadOnlyCollection<BankStatementLine> Lines => _lines.AsReadOnly();

    private BankStatement()
    {
        var currency = new CurrencyCode("USD");
        OpeningBalance = new Money(0m, currency);
        ClosingBalance = new Money(0m, currency);
    }

    public BankStatement(
        Guid organizationId,
        Guid bankAccountId,
        DateOnly periodStart,
        DateOnly periodEnd,
        Money openingBalance,
        Money closingBalance)
        : base(organizationId)
    {
        if (bankAccountId == Guid.Empty)
        {
            throw new DomainException("Bank account is required.");
        }

        if (periodEnd < periodStart)
        {
            throw new DomainException("Period end must be on or after period start.");
        }

        if (openingBalance.Currency != closingBalance.Currency)
        {
            throw new DomainException("Opening and closing balances must use the same currency.");
        }

        BankAccountId = bankAccountId;
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        OpeningBalance = openingBalance;
        ClosingBalance = closingBalance;
        Status = BankStatementStatus.Open;
    }

    public BankStatementLine AddManualLine(
        DateOnly transactionDate,
        Money amount,
        string description,
        string? reference)
    {
        EnsureOpen();

        if (transactionDate < PeriodStart || transactionDate > PeriodEnd)
        {
            throw new DomainException("Transaction date must fall within the statement period.");
        }

        EnsureCurrency(amount);
        var line = new BankStatementLine(Id, transactionDate, amount, description, reference);
        _lines.Add(line);
        Touch();
        return line;
    }

    public void ImportLines(IEnumerable<(DateOnly Date, Money Amount, string Description, string? Reference)> lines)
    {
        EnsureOpen();

        foreach (var imported in lines)
        {
            if (imported.Date < PeriodStart || imported.Date > PeriodEnd)
            {
                throw new DomainException("Imported transaction date must fall within the statement period.");
            }

            EnsureCurrency(imported.Amount);
            _lines.Add(new BankStatementLine(Id, imported.Date, imported.Amount, imported.Description, imported.Reference));
        }

        Touch();
    }

    public void MatchLine(Guid lineId, Guid journalLineId, Money debit, Money credit)
    {
        EnsureOpen();
        GetLine(lineId).Match(journalLineId, debit, credit);
        Touch();
    }

    public void MarkLineEntryCreated(Guid lineId, Guid journalEntryId, Guid journalLineId, Money debit, Money credit)
    {
        EnsureOpen();
        GetLine(lineId).MarkEntryCreated(journalEntryId, journalLineId, debit, credit);
        Touch();
    }

    public void Reconcile()
    {
        EnsureOpen();

        if (_lines.Count == 0)
        {
            throw new DomainException("Statement must contain at least one line before reconciliation.");
        }

        if (_lines.Any(line => line.Status == BankStatementLineStatus.Pending))
        {
            throw new DomainException("All statement lines must be matched before reconciliation.");
        }

        var movement = _lines.Aggregate(new Money(0m, OpeningBalance.Currency), (sum, line) => sum + line.Amount);
        var calculatedClosing = OpeningBalance + movement;

        if (calculatedClosing.Amount != ClosingBalance.Amount)
        {
            throw new DomainException("Statement lines do not reconcile to the closing balance.");
        }

        Status = BankStatementStatus.Reconciled;
        Touch();
    }

    public Money CalculateMovementTotal()
    {
        return _lines.Aggregate(new Money(0m, OpeningBalance.Currency), (sum, line) => sum + line.Amount);
    }

    private BankStatementLine GetLine(Guid lineId)
    {
        var line = _lines.FirstOrDefault(l => l.Id == lineId);

        if (line is null)
        {
            throw new DomainException("Statement line not found.");
        }

        return line;
    }

    private void EnsureOpen()
    {
        if (Status != BankStatementStatus.Open)
        {
            throw new DomainException("Reconciled statements cannot be modified.");
        }
    }

    private void EnsureCurrency(Money amount)
    {
        if (amount.Currency != OpeningBalance.Currency)
        {
            throw new DomainException("Line currency must match statement currency.");
        }
    }
}
