using Ledgerly.Domain.Enums;

namespace Ledgerly.Application.Recurring;

public sealed record RecurringScheduleDto(Guid Id, Guid OrganizationId, string Name, string CronExpression, DateTimeOffset NextRunUtc, bool IsPaused, RecurringTransactionType TransactionType, string TemplateJson, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record CreateRecurringScheduleRequest(Guid OrganizationId, string Name, string CronExpression, DateTimeOffset? StartUtc, RecurringTransactionType TransactionType, string TemplateJson);
public sealed record UpdateRecurringScheduleRequest(string Name, string CronExpression, DateTimeOffset NextRunUtc, string TemplateJson);
public sealed record PreviewRecurringScheduleRequest(string CronExpression, DateTimeOffset FromUtc, int Count);
