using Ledgerly.Application.Abstractions;
using Ledgerly.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ledgerly.Infrastructure.Persistence.Repositories;

public sealed class FiscalPeriodRepository : IFiscalPeriodRepository
{
    private readonly LedgerlyDbContext _context;

    public FiscalPeriodRepository(LedgerlyDbContext context)
    {
        _context = context;
    }

    public Task<FiscalPeriod?> GetByYearMonthAsync(
        Guid organizationId,
        int year,
        int month,
        CancellationToken cancellationToken = default) =>
        _context.FiscalPeriods
            .FirstOrDefaultAsync(
                period =>
                    period.OrganizationId == organizationId &&
                    period.Year == year &&
                    period.Month == month,
                cancellationToken);

    public async Task<IReadOnlyList<FiscalPeriod>> ListByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        await _context.FiscalPeriods
            .Where(period => period.OrganizationId == organizationId)
            .OrderByDescending(period => period.Year)
            .ThenByDescending(period => period.Month)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(FiscalPeriod period, CancellationToken cancellationToken = default)
    {
        await _context.FiscalPeriods.AddAsync(period, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(FiscalPeriod period, CancellationToken cancellationToken = default)
    {
        _context.FiscalPeriods.Update(period);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
