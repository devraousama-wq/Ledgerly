using Ledgerly.Application.Abstractions;
using Ledgerly.Domain.Entities;
using Ledgerly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ledgerly.Infrastructure.Persistence.Repositories;

public sealed class JournalRepository : IJournalRepository
{
    private readonly LedgerlyDbContext _context;

    public JournalRepository(LedgerlyDbContext context)
    {
        _context = context;
    }

    public Task<JournalEntry?> GetByIdAsync(
        Guid organizationId,
        Guid journalEntryId,
        CancellationToken cancellationToken = default) =>
        _context.JournalEntries
            .Include(entry => entry.Lines)
            .FirstOrDefaultAsync(
                entry => entry.OrganizationId == organizationId && entry.Id == journalEntryId,
                cancellationToken);

    public async Task<IReadOnlyList<JournalEntry>> ListByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        return await _context.JournalEntries
            .Include(entry => entry.Lines)
            .Where(entry => entry.OrganizationId == organizationId)
            .OrderByDescending(entry => entry.EntryDate)
            .ThenByDescending(entry => entry.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(JournalEntry entry, CancellationToken cancellationToken = default)
    {
        await _context.JournalEntries.AddAsync(entry, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(JournalEntry entry, CancellationToken cancellationToken = default)
    {
        _context.JournalEntries.Update(entry);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
