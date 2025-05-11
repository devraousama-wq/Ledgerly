using Ledgerly.Domain.Common;
using Ledgerly.Domain.Enums;
using Ledgerly.Domain.Exceptions;

namespace Ledgerly.Domain.Entities;

public sealed class FiscalPeriod : OrganizationScopedEntity
{
    public int Year { get; private set; }

    public int Month { get; private set; }

    public FiscalPeriodStatus Status { get; private set; }

    private FiscalPeriod()
    {
    }

    public FiscalPeriod(Guid organizationId, int year, int month)
        : base(organizationId)
    {
        ValidateYearMonth(year, month);
        Year = year;
        Month = month;
        Status = FiscalPeriodStatus.Open;
    }

    public void Close()
    {
        if (Status == FiscalPeriodStatus.Closed)
        {
            throw new DomainException("Fiscal period is already closed.");
        }

        Status = FiscalPeriodStatus.Closed;
        Touch();
    }

    public void Reopen()
    {
        if (Status == FiscalPeriodStatus.Open)
        {
            throw new DomainException("Fiscal period is already open.");
        }

        Status = FiscalPeriodStatus.Open;
        Touch();
    }

    private static void ValidateYearMonth(int year, int month)
    {
        if (year < 1)
        {
            throw new DomainException("Fiscal period year must be positive.");
        }

        if (month is < 1 or > 12)
        {
            throw new DomainException("Fiscal period month must be between 1 and 12.");
        }
    }
}
