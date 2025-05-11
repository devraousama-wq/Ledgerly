using Ledgerly.Domain.Entities;

namespace Ledgerly.Application.Abstractions;

public interface IPeriodCloseAuditRepository
{
    Task<IReadOnlyList<PeriodCloseAudit>> ListByOrganizationAsync(
        Guid organizationId,
        int? year = null,
        int? month = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(PeriodCloseAudit audit, CancellationToken cancellationToken = default);
}
