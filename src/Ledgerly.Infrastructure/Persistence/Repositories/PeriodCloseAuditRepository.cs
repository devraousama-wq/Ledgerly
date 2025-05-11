using Ledgerly.Application.Abstractions;
using Ledgerly.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ledgerly.Infrastructure.Persistence.Repositories;

public sealed class PeriodCloseAuditRepository : IPeriodCloseAuditRepository
{
    private readonly LedgerlyDbContext _context;

    public PeriodCloseAuditRepository(LedgerlyDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<PeriodCloseAudit>> ListByOrganizationAsync(
        Guid organizationId,
        int? year = null,
        int? month = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PeriodCloseAudits
            .Where(audit => audit.OrganizationId == organizationId);

        if (year.HasValue)
        {
            query = query.Where(audit => audit.Year == year.Value);
        }

        if (month.HasValue)
        {
            query = query.Where(audit => audit.Month == month.Value);
        }

        return await query
            .OrderByDescending(audit => audit.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(PeriodCloseAudit audit, CancellationToken cancellationToken = default)
    {
        await _context.PeriodCloseAudits.AddAsync(audit, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
