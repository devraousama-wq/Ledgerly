using Ledgerly.Domain.Entities;

namespace Ledgerly.Application.Abstractions;

public interface ITaxCodeRepository
{
    Task<TaxCode?> GetByIdAsync(
        Guid organizationId,
        Guid taxCodeId,
        CancellationToken cancellationToken = default);

    Task<TaxCode?> GetByCodeAsync(
        Guid organizationId,
        string code,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaxCode>> ListByOrganizationAsync(
        Guid organizationId,
        bool includeArchived = false,
        CancellationToken cancellationToken = default);

    Task AddAsync(TaxCode taxCode, CancellationToken cancellationToken = default);

    Task UpdateAsync(TaxCode taxCode, CancellationToken cancellationToken = default);
}
