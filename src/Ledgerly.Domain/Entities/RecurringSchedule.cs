using Ledgerly.Domain.Common;
using Ledgerly.Domain.Enums;
using Ledgerly.Domain.Exceptions;

namespace Ledgerly.Domain.Entities;

public sealed class RecurringSchedule : OrganizationScopedEntity
{
    public string Name { get; private set; } = string.Empty;
    public string CronExpression { get; private set; } = string.Empty;
    public DateTimeOffset NextRunUtc { get; private set; }
    public bool IsPaused { get; private set; }
    public RecurringTransactionType TransactionType { get; private set; }
    public string TemplateJson { get; private set; } = string.Empty;

    private RecurringSchedule() { }

    public RecurringSchedule(Guid organizationId, string name, string cronExpression, DateTimeOffset nextRunUtc, RecurringTransactionType transactionType, string templateJson) : base(organizationId)
    {
        SetName(name);
        SetCronExpression(cronExpression);
        NextRunUtc = nextRunUtc;
        TransactionType = transactionType;
        SetTemplateJson(templateJson);
        IsPaused = false;
    }

    public void Update(string name, string cronExpression, DateTimeOffset nextRunUtc, string templateJson)
    {
        SetName(name);
        SetCronExpression(cronExpression);
        NextRunUtc = nextRunUtc;
        SetTemplateJson(templateJson);
        Touch();
    }

    public void Pause()
    {
        IsPaused = true;
        Touch();
    }

    public void Resume(DateTimeOffset nextRunUtc)
    {
        IsPaused = false;
        NextRunUtc = nextRunUtc;
        Touch();
    }

    public void AdvanceNextRun(DateTimeOffset nextRunUtc)
    {
        NextRunUtc = nextRunUtc;
        Touch();
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Schedule name is required.");
        Name = name.Trim();
    }

    private void SetCronExpression(string cronExpression)
    {
        if (string.IsNullOrWhiteSpace(cronExpression))
            throw new DomainException("Cron expression is required.");
        CronExpression = cronExpression.Trim();
    }

    private void SetTemplateJson(string templateJson)
    {
        if (string.IsNullOrWhiteSpace(templateJson))
            throw new DomainException("Template is required.");
        TemplateJson = templateJson;
    }
}
