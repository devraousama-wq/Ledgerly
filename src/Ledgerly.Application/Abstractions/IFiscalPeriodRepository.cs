using Ledgerly.Domain.Entities;

namespace Ledgerly.Application.Abstractions;

public interface IFiscalPeriodRepository
{
    Task<FiscalPeriod?> GetByYearMonthAsync(
        Guid organizationId,
        int year,
        int month,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FiscalPeriod>> ListByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(FiscalPeriod period, CancellationToken cancellationToken = default);

    Task UpdateAsync(FiscalPeriod period, CancellationToken cancellationToken = default);
}
