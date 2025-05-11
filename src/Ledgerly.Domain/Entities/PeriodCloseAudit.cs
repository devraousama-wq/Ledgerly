using Ledgerly.Domain.Common;
using Ledgerly.Domain.Enums;

namespace Ledgerly.Domain.Entities;

public sealed class PeriodCloseAudit : OrganizationScopedEntity
{
    public Guid FiscalPeriodId { get; private set; }

    public int Year { get; private set; }

    public int Month { get; private set; }

    public PeriodCloseAction Action { get; private set; }

    private PeriodCloseAudit()
    {
    }

    public PeriodCloseAudit(
        Guid organizationId,
        Guid fiscalPeriodId,
        int year,
        int month,
        PeriodCloseAction action)
        : base(organizationId)
    {
        FiscalPeriodId = fiscalPeriodId;
        Year = year;
        Month = month;
        Action = action;
    }
}
