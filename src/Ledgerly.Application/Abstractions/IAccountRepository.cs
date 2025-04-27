using Ledgerly.Domain.Entities;

namespace Ledgerly.Application.Abstractions;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid organizationId, Guid accountId, CancellationToken cancellationToken = default);

    Task<Account?> GetByCodeAsync(Guid organizationId, string code, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Account>> ListByOrganizationAsync(
        Guid organizationId,
        bool includeArchived = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Account>> GetChildrenAsync(
        Guid organizationId,
        Guid parentAccountId,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveChildrenAsync(
        Guid organizationId,
        Guid accountId,
        CancellationToken cancellationToken = default);

    Task AddAsync(Account account, CancellationToken cancellationToken = default);

    Task UpdateAsync(Account account, CancellationToken cancellationToken = default);
}
