namespace Ledgerly.Application.Journals;

public sealed record CreateJournalRequest(
    Guid OrganizationId,
    DateOnly EntryDate,
    string? Reference,
    string Description,
    string BaseCurrency);
