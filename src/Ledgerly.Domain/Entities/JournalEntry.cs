using Ledgerly.Domain.Common;
using Ledgerly.Domain.Enums;
using Ledgerly.Domain.Exceptions;
using Ledgerly.Domain.ValueObjects;

namespace Ledgerly.Domain.Entities;

public sealed class JournalEntry : OrganizationScopedEntity
{
    private readonly List<JournalLine> _lines = new();

    public DateOnly EntryDate { get; private set; }

    public string? Reference { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public JournalEntryStatus Status { get; private set; }

    public CurrencyCode BaseCurrency { get; private set; }

    public Guid? ReversalOfEntryId { get; private set; }

    public IReadOnlyCollection<JournalLine> Lines => _lines.AsReadOnly();

    private JournalEntry()
    {
        BaseCurrency = new CurrencyCode("USD");
    }

    public JournalEntry(
        Guid organizationId,
        DateOnly entryDate,
        string? reference,
        string description,
        CurrencyCode baseCurrency)
        : base(organizationId)
    {
        EntryDate = entryDate;
        Reference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim();
        SetDescription(description);
        BaseCurrency = baseCurrency;
        Status = JournalEntryStatus.Draft;
    }

    public JournalLine AddLine(Guid accountId, Money debit, Money credit, string? memo = null)
    {
        EnsureDraft();

        if (accountId == Guid.Empty)
        {
            throw new DomainException("Account is required.");
        }

        EnsureCurrencyMatches(debit);
        EnsureCurrencyMatches(credit);

        if (debit.Amount < 0m || credit.Amount < 0m)
        {
            throw new DomainException("Debit and credit amounts cannot be negative.");
        }

        if (debit.Amount > 0m && credit.Amount > 0m)
        {
            throw new DomainException("A journal line cannot have both debit and credit amounts.");
        }

        if (debit.Amount == 0m && credit.Amount == 0m)
        {
            throw new DomainException("A journal line must have a debit or credit amount.");
        }

        var line = new JournalLine(Id, accountId, debit, credit, memo);
        _lines.Add(line);
        Touch();
        return line;
    }

    public void ValidateBalance()
    {
        if (_lines.Count < 2)
        {
            throw new DomainException("A journal entry must have at least two lines.");
        }

        var totalDebits = _lines.Aggregate(new Money(0m, BaseCurrency), (sum, line) => sum + line.Debit);
        var totalCredits = _lines.Aggregate(new Money(0m, BaseCurrency), (sum, line) => sum + line.Credit);

        if (totalDebits.Amount != totalCredits.Amount)
        {
            throw new UnbalancedJournalException(totalDebits.Amount, totalCredits.Amount);
        }
    }

    public void Post()
    {
        EnsureDraft();
        ValidateBalance();
        Status = JournalEntryStatus.Posted;
        Touch();
    }

    public JournalEntry CreateReversal()
    {
        if (Status != JournalEntryStatus.Posted)
        {
            throw new DomainException("Only posted journal entries can be reversed.");
        }

        var reversalReference = string.IsNullOrWhiteSpace(Reference)
            ? "REV"
            : $"REV-{Reference}";

        var reversal = new JournalEntry(
            OrganizationId,
            EntryDate,
            reversalReference,
            $"Reversal of {Description}",
            BaseCurrency)
        {
            ReversalOfEntryId = Id
        };

        foreach (var line in _lines)
        {
            reversal.AddLine(line.AccountId, line.Credit, line.Debit, line.Memo);
        }

        reversal.Post();
        Status = JournalEntryStatus.Reversed;
        Touch();
        return reversal;
    }

    private void EnsureDraft()
    {
        if (Status != JournalEntryStatus.Draft)
        {
            throw new DomainException("Journal entry is not in draft status.");
        }
    }

    private void EnsureCurrencyMatches(Money amount)
    {
        if (amount.Currency != BaseCurrency)
        {
            throw new DomainException("Line currency must match journal base currency.");
        }
    }

    private void SetDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainException("Journal description is required.");
        }

        Description = description.Trim();
    }
}
