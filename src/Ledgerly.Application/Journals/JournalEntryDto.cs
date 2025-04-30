using Ledgerly.Domain.Enums;

namespace Ledgerly.Application.Journals;

public sealed record JournalEntryDto(
    Guid Id,
    Guid OrganizationId,
    DateOnly EntryDate,
    string? Reference,
    string Description,
    JournalEntryStatus Status,
    string BaseCurrency,
    Guid? ReversalOfEntryId,
    IReadOnlyList<JournalLineDto> Lines,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
