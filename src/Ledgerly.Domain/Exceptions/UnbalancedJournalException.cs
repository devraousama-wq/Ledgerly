namespace Ledgerly.Domain.Exceptions;

public sealed class UnbalancedJournalException : DomainException
{
    public UnbalancedJournalException(decimal debits, decimal credits)
        : base($"Journal entry is unbalanced: debits {debits}, credits {credits}.")
    {
        Debits = debits;
        Credits = credits;
    }

    public decimal Debits { get; }

    public decimal Credits { get; }
}
