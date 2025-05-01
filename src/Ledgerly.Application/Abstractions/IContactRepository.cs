using Ledgerly.Domain.Entities;
using Ledgerly.Domain.Enums;

namespace Ledgerly.Application.Abstractions;

public interface IContactRepository
{
    Task<Contact?> GetByIdAsync(
        Guid organizationId,
        Guid contactId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Contact>> ListByTypeAsync(
        Guid organizationId,
        ContactType contactType,
        bool includeArchived = false,
        CancellationToken cancellationToken = default);

    Task AddAsync(Contact contact, CancellationToken cancellationToken = default);

    Task UpdateAsync(Contact contact, CancellationToken cancellationToken = default);
}
