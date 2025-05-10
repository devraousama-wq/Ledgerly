using Ledgerly.Domain.Common;
using Ledgerly.Domain.Enums;

namespace Ledgerly.Domain.Entities;

public sealed class RecurringScheduleRun : Entity
{
    public Guid RecurringScheduleId { get; private set; }
    public Guid OrganizationId { get; private set; }
    public DateTimeOffset OccurrenceUtc { get; private set; }
    public Guid GeneratedEntityId { get; private set; }
    public RecurringTransactionType TransactionType { get; private set; }
    public RecurringScheduleRunStatus Status { get; private set; }

    private RecurringScheduleRun() { }

    public RecurringScheduleRun(Guid recurringScheduleId, Guid organizationId, DateTimeOffset occurrenceUtc, Guid generatedEntityId, RecurringTransactionType transactionType, RecurringScheduleRunStatus status = RecurringScheduleRunStatus.Succeeded)
    {
        RecurringScheduleId = recurringScheduleId;
        OrganizationId = organizationId;
        OccurrenceUtc = occurrenceUtc;
        GeneratedEntityId = generatedEntityId;
        TransactionType = transactionType;
        Status = status;
    }
}
