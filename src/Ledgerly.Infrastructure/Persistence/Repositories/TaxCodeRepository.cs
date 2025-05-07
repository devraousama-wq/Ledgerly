using Ledgerly.Application.Abstractions;
using Ledgerly.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ledgerly.Infrastructure.Persistence.Repositories;

public sealed class TaxCodeRepository : ITaxCodeRepository
{
    private readonly LedgerlyDbContext _context;

    public TaxCodeRepository(LedgerlyDbContext context)
    {
        _context = context;
    }

    public Task<TaxCode?> GetByIdAsync(
        Guid organizationId,
        Guid taxCodeId,
        CancellationToken cancellationToken = default) =>
        _context.TaxCodes
            .Include(t => t.Components)
            .FirstOrDefaultAsync(
                t => t.OrganizationId == organizationId && t.Id == taxCodeId,
                cancellationToken);

    public Task<TaxCode?> GetByCodeAsync(
        Guid organizationId,
        string code,
        CancellationToken cancellationToken = default) =>
        _context.TaxCodes
            .Include(t => t.Components)
            .FirstOrDefaultAsync(
                t => t.OrganizationId == organizationId && t.Code == code,
                cancellationToken);

    public async Task<IReadOnlyList<TaxCode>> ListByOrganizationAsync(
        Guid organizationId,
        bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.TaxCodes
            .Include(t => t.Components)
            .Where(t => t.OrganizationId == organizationId);

        if (!includeArchived)
        {
            query = query.Where(t => !t.IsArchived);
        }

        return await query
            .OrderBy(t => t.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TaxCode taxCode, CancellationToken cancellationToken = default)
    {
        await _context.TaxCodes.AddAsync(taxCode, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(TaxCode taxCode, CancellationToken cancellationToken = default)
    {
        _context.TaxCodes.Update(taxCode);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
