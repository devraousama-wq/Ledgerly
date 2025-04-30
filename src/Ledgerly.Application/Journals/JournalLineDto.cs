namespace Ledgerly.Application.Journals;

public sealed record JournalLineDto(
    Guid Id,
    Guid AccountId,
    decimal DebitAmount,
    decimal CreditAmount,
    string? Memo);
