using Ledgerly.Domain.Common;
using Ledgerly.Domain.Enums;
using Ledgerly.Domain.Exceptions;
using Ledgerly.Domain.ValueObjects;

namespace Ledgerly.Domain.Entities;

public sealed class BankStatementLine : Entity
{
    public Guid BankStatementId { get; private set; }

    public DateOnly TransactionDate { get; private set; }

    public Money Amount { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public string? Reference { get; private set; }

    public BankStatementLineStatus Status { get; private set; }

    public Guid? MatchedJournalLineId { get; private set; }

    public Guid? CreatedJournalEntryId { get; private set; }

    private BankStatementLine()
    {
        Amount = new Money(0m, new CurrencyCode("USD"));
    }

    internal BankStatementLine(
        Guid bankStatementId,
        DateOnly transactionDate,
        Money amount,
        string description,
        string? reference)
    {
        if (bankStatementId == Guid.Empty)
        {
            throw new DomainException("Bank statement is required.");
        }

        if (amount.Amount == 0m)
        {
            throw new DomainException("Transaction amount cannot be zero.");
        }

        BankStatementId = bankStatementId;
        TransactionDate = transactionDate;
        Amount = amount;
        Description = description.Trim();
        Reference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim();
        Status = BankStatementLineStatus.Pending;
    }

    public void Match(Guid journalLineId, Money debit, Money credit)
    {
        if (Status != BankStatementLineStatus.Pending)
        {
            throw new DomainException("Only pending lines can be matched.");
        }

        if (journalLineId == Guid.Empty)
        {
            throw new DomainException("Journal line is required.");
        }

        ValidateLedgerAmount(debit, credit);
        MatchedJournalLineId = journalLineId;
        Status = BankStatementLineStatus.Matched;
    }

    public void MarkEntryCreated(Guid journalEntryId, Guid journalLineId, Money debit, Money credit)
    {
        if (Status != BankStatementLineStatus.Pending)
        {
            throw new DomainException("Only pending lines can create entries.");
        }

        if (journalEntryId == Guid.Empty || journalLineId == Guid.Empty)
        {
            throw new DomainException("Journal entry is required.");
        }

        ValidateLedgerAmount(debit, credit);
        CreatedJournalEntryId = journalEntryId;
        MatchedJournalLineId = journalLineId;
        Status = BankStatementLineStatus.EntryCreated;
    }

    public Money GetSignedLedgerAmount()
    {
        if (Amount.Amount > 0m)
        {
            return Amount;
        }

        return new Money(-Amount.Amount, Amount.Currency);
    }

    private void ValidateLedgerAmount(Money debit, Money credit)
    {
        if (debit.Currency != Amount.Currency || credit.Currency != Amount.Currency)
        {
            throw new DomainException("Journal line currency must match statement currency.");
        }

        if (Amount.Amount > 0m)
        {
            if (debit.Amount != Amount.Amount || credit.Amount != 0m)
            {
                throw new DomainException("Deposit lines must match a bank debit amount.");
            }

            return;
        }

        var withdrawal = new Money(-Amount.Amount, Amount.Currency);

        if (credit.Amount != withdrawal.Amount || debit.Amount != 0m)
        {
            throw new DomainException("Withdrawal lines must match a bank credit amount.");
        }
    }
}
