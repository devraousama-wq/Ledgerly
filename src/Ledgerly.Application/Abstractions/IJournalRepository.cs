using Ledgerly.Domain.Entities;

namespace Ledgerly.Application.Abstractions;

public interface IJournalRepository
{
    Task<JournalEntry?> GetByIdAsync(
        Guid organizationId,
        Guid journalEntryId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<JournalEntry>> ListByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<(JournalEntry Entry, JournalLine Line)>> ListPostedLinesByAccountAsync(
        Guid organizationId,
        Guid accountId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);

    Task AddAsync(JournalEntry entry, CancellationToken cancellationToken = default);

    Task UpdateAsync(JournalEntry entry, CancellationToken cancellationToken = default);
}
