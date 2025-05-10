using Ledgerly.Application.Abstractions;
using Ledgerly.Domain.Common;
using Ledgerly.Domain.Entities;
using Ledgerly.Domain.Exceptions;

namespace Ledgerly.Application.Recurring;

public sealed class RecurringScheduleService
{
    private readonly IRecurringScheduleRepository _repository;

    public RecurringScheduleService(IRecurringScheduleRepository repository)
    {
        _repository = repository;
    }

    public Task<Result<IReadOnlyList<RecurringScheduleDto>>> ListAsync(Guid organizationId, CancellationToken cancellationToken = default)
        => MapListAsync(organizationId, cancellationToken);

    public async Task<Result<RecurringScheduleDto>> GetByIdAsync(Guid organizationId, Guid scheduleId, CancellationToken cancellationToken = default)
    {
        var schedule = await _repository.GetByIdAsync(organizationId, scheduleId, cancellationToken);
        return schedule is null
            ? Result<RecurringScheduleDto>.Failure("Recurring schedule not found.")
            : Result<RecurringScheduleDto>.Success(Map(schedule));
    }

    public async Task<Result<RecurringScheduleDto>> CreateAsync(CreateRecurringScheduleRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!CronScheduleHelper.IsValid(request.CronExpression))
                return Result<RecurringScheduleDto>.Failure("Invalid cron expression.");

            var startUtc = request.StartUtc ?? DateTimeOffset.UtcNow;
            var nextRunUtc = CronScheduleHelper.GetNextOccurrence(request.CronExpression, startUtc);
            var schedule = new RecurringSchedule(request.OrganizationId, request.Name, request.CronExpression, nextRunUtc, request.TransactionType, request.TemplateJson);
            await _repository.AddAsync(schedule, cancellationToken);
            return Result<RecurringScheduleDto>.Success(Map(schedule));
        }
        catch (DomainException e)
        {
            return Result<RecurringScheduleDto>.Failure(e.Message);
        }
    }

    public async Task<Result<RecurringScheduleDto>> UpdateAsync(Guid organizationId, Guid scheduleId, UpdateRecurringScheduleRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var schedule = await _repository.GetByIdAsync(organizationId, scheduleId, cancellationToken);
            if (schedule is null)
                return Result<RecurringScheduleDto>.Failure("Recurring schedule not found.");

            if (!CronScheduleHelper.IsValid(request.CronExpression))
                return Result<RecurringScheduleDto>.Failure("Invalid cron expression.");

            schedule.Update(request.Name, request.CronExpression, request.NextRunUtc, request.TemplateJson);
            await _repository.UpdateAsync(schedule, cancellationToken);
            return Result<RecurringScheduleDto>.Success(Map(schedule));
        }
        catch (DomainException e)
        {
            return Result<RecurringScheduleDto>.Failure(e.Message);
        }
    }

    public async Task<Result<RecurringScheduleDto>> PauseAsync(Guid organizationId, Guid scheduleId, CancellationToken cancellationToken = default)
    {
        var schedule = await _repository.GetByIdAsync(organizationId, scheduleId, cancellationToken);
        if (schedule is null)
            return Result<RecurringScheduleDto>.Failure("Recurring schedule not found.");

        schedule.Pause();
        await _repository.UpdateAsync(schedule, cancellationToken);
        return Result<RecurringScheduleDto>.Success(Map(schedule));
    }

    public async Task<Result<RecurringScheduleDto>> ResumeAsync(Guid organizationId, Guid scheduleId, CancellationToken cancellationToken = default)
    {
        try
        {
            var schedule = await _repository.GetByIdAsync(organizationId, scheduleId, cancellationToken);
            if (schedule is null)
                return Result<RecurringScheduleDto>.Failure("Recurring schedule not found.");

            schedule.Resume(CronScheduleHelper.GetNextOccurrence(schedule.CronExpression, DateTimeOffset.UtcNow.AddSeconds(-1)));
            await _repository.UpdateAsync(schedule, cancellationToken);
            return Result<RecurringScheduleDto>.Success(Map(schedule));
        }
        catch (DomainException e)
        {
            return Result<RecurringScheduleDto>.Failure(e.Message);
        }
    }

    public Result<IReadOnlyList<DateTimeOffset>> PreviewAsync(PreviewRecurringScheduleRequest request)
    {
        if (!CronScheduleHelper.IsValid(request.CronExpression))
            return Result<IReadOnlyList<DateTimeOffset>>.Failure("Invalid cron expression.");

        return Result<IReadOnlyList<DateTimeOffset>>.Success(CronScheduleHelper.GetNextOccurrences(request.CronExpression, request.FromUtc, request.Count));
    }

    private async Task<Result<IReadOnlyList<RecurringScheduleDto>>> MapListAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var schedules = await _repository.ListByOrganizationAsync(organizationId, cancellationToken);
        return Result<IReadOnlyList<RecurringScheduleDto>>.Success(schedules.Select(Map).ToList());
    }

    private static RecurringScheduleDto Map(RecurringSchedule schedule)
        => new(schedule.Id, schedule.OrganizationId, schedule.Name, schedule.CronExpression, schedule.NextRunUtc, schedule.IsPaused, schedule.TransactionType, schedule.TemplateJson, schedule.CreatedAt, schedule.UpdatedAt);
}
