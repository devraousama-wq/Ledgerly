using Ledgerly.Domain.Common;
using Ledgerly.Domain.ValueObjects;

namespace Ledgerly.Domain.Entities;

public sealed class JournalLine : Entity
{
    public Guid JournalEntryId { get; private set; }

    public Guid AccountId { get; private set; }

    public Money Debit { get; private set; }

    public Money Credit { get; private set; }

    public string? Memo { get; private set; }

    private JournalLine()
    {
        Debit = new Money(0m, new CurrencyCode("USD"));
        Credit = new Money(0m, new CurrencyCode("USD"));
    }

    internal JournalLine(
        Guid journalEntryId,
        Guid accountId,
        Money debit,
        Money credit,
        string? memo)
    {
        JournalEntryId = journalEntryId;
        AccountId = accountId;
        Debit = debit;
        Credit = credit;
        Memo = memo?.Trim();
    }
}
