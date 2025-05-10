using Ledgerly.Domain.Entities;

namespace Ledgerly.Application.Abstractions;

public interface IRecurringScheduleRepository
{
    Task<RecurringSchedule?> GetByIdAsync(Guid organizationId, Guid scheduleId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RecurringSchedule>> ListByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RecurringSchedule>> ListDueAsync(DateTimeOffset asOfUtc, CancellationToken cancellationToken = default);
    Task AddAsync(RecurringSchedule schedule, CancellationToken cancellationToken = default);
    Task UpdateAsync(RecurringSchedule schedule, CancellationToken cancellationToken = default);
    Task<RecurringScheduleRun?> GetRunByOccurrenceAsync(Guid scheduleId, DateTimeOffset occurrenceUtc, CancellationToken cancellationToken = default);
    Task AddRunAsync(RecurringScheduleRun run, CancellationToken cancellationToken = default);
}
