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

    public async Task<IReadOnlyList<(JournalEntry Entry, JournalLine Line)>> ListPostedLinesByAccountAsync(
        Guid organizationId,
        Guid accountId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        var entries = await _context.JournalEntries
            .Include(entry => entry.Lines)
            .Where(entry =>
                entry.OrganizationId == organizationId &&
                entry.Status == Domain.Enums.JournalEntryStatus.Posted &&
                entry.EntryDate >= startDate &&
                entry.EntryDate <= endDate)
            .ToListAsync(cancellationToken);

        return entries
            .SelectMany(entry => entry.Lines
                .Where(line => line.AccountId == accountId)
                .Select(line => (entry, line)))
            .ToList();
    }

    public async Task<IReadOnlyList<(JournalEntry Entry, JournalLine Line)>> ListPostedLinesAsync(
        Guid organizationId,
        DateOnly? startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        var query = _context.JournalEntries
            .Include(entry => entry.Lines)
            .Where(entry =>
                entry.OrganizationId == organizationId &&
                entry.Status == Domain.Enums.JournalEntryStatus.Posted &&
                entry.EntryDate <= endDate);

        if (startDate.HasValue)
        {
            query = query.Where(entry => entry.EntryDate >= startDate.Value);
        }

        var entries = await query.ToListAsync(cancellationToken);

        return entries
            .SelectMany(entry => entry.Lines.Select(line => (entry, line)))
            .ToList();
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
