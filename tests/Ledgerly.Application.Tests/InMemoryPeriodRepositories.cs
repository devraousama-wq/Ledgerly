using Ledgerly.Application.Abstractions;
using Ledgerly.Domain.Entities;

namespace Ledgerly.Application.Tests;

internal sealed class InMemoryFiscalPeriodRepository : IFiscalPeriodRepository
{
    private readonly List<FiscalPeriod> _periods = new();

    public Task<FiscalPeriod?> GetByYearMonthAsync(
        Guid organizationId,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var period = _periods.FirstOrDefault(p =>
            p.OrganizationId == organizationId &&
            p.Year == year &&
            p.Month == month);

        return Task.FromResult(period);
    }

    public Task<IReadOnlyList<FiscalPeriod>> ListByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<FiscalPeriod> results = _periods
            .Where(p => p.OrganizationId == organizationId)
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.Month)
            .ToList();

        return Task.FromResult(results);
    }

    public Task AddAsync(FiscalPeriod period, CancellationToken cancellationToken = default)
    {
        _periods.Add(period);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(FiscalPeriod period, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryPeriodCloseAuditRepository : IPeriodCloseAuditRepository
{
    private readonly List<PeriodCloseAudit> _audits = new();

    public Task<IReadOnlyList<PeriodCloseAudit>> ListByOrganizationAsync(
        Guid organizationId,
        int? year = null,
        int? month = null,
        CancellationToken cancellationToken = default)
    {
        var query = _audits.Where(a => a.OrganizationId == organizationId);

        if (year.HasValue)
        {
            query = query.Where(a => a.Year == year.Value);
        }

        if (month.HasValue)
        {
            query = query.Where(a => a.Month == month.Value);
        }

        IReadOnlyList<PeriodCloseAudit> results = query
            .OrderByDescending(a => a.CreatedAt)
            .ToList();

        return Task.FromResult(results);
    }

    public Task AddAsync(PeriodCloseAudit audit, CancellationToken cancellationToken = default)
    {
        _audits.Add(audit);
        return Task.CompletedTask;
    }
}
