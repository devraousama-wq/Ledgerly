using Ledgerly.Application.Abstractions;
using Ledgerly.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ledgerly.Infrastructure.Persistence.Repositories;

public sealed class RecurringScheduleRepository : IRecurringScheduleRepository
{
    private readonly LedgerlyDbContext _context;

    public RecurringScheduleRepository(LedgerlyDbContext context)
    {
        _context = context;
    }

    public Task<RecurringSchedule?> GetByIdAsync(Guid organizationId, Guid scheduleId, CancellationToken cancellationToken = default)
        => _context.RecurringSchedules.FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == scheduleId, cancellationToken);

    public async Task<IReadOnlyList<RecurringSchedule>> ListByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
        => await _context.RecurringSchedules.Where(x => x.OrganizationId == organizationId).OrderBy(x => x.Name).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<RecurringSchedule>> ListDueAsync(DateTimeOffset asOfUtc, CancellationToken cancellationToken = default)
        => await _context.RecurringSchedules.Where(x => !x.IsPaused && x.NextRunUtc <= asOfUtc).OrderBy(x => x.NextRunUtc).ToListAsync(cancellationToken);

    public async Task AddAsync(RecurringSchedule schedule, CancellationToken cancellationToken = default)
    {
        await _context.RecurringSchedules.AddAsync(schedule, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(RecurringSchedule schedule, CancellationToken cancellationToken = default)
    {
        _context.RecurringSchedules.Update(schedule);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<RecurringScheduleRun?> GetRunByOccurrenceAsync(Guid scheduleId, DateTimeOffset occurrenceUtc, CancellationToken cancellationToken = default)
        => _context.RecurringScheduleRuns.FirstOrDefaultAsync(x => x.RecurringScheduleId == scheduleId && x.OccurrenceUtc == occurrenceUtc, cancellationToken);

    public async Task AddRunAsync(RecurringScheduleRun run, CancellationToken cancellationToken = default)
    {
        await _context.RecurringScheduleRuns.AddAsync(run, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
