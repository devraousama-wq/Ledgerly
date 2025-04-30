namespace Ledgerly.Application.Journals;

public sealed record AddJournalLineRequest(
    Guid AccountId,
    decimal DebitAmount,
    decimal CreditAmount,
    string? Memo);
