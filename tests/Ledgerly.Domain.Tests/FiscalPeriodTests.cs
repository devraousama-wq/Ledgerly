using Ledgerly.Domain.Entities;
using Ledgerly.Domain.Enums;
using Ledgerly.Domain.Exceptions;

namespace Ledgerly.Domain.Tests;

public class FiscalPeriodTests
{
    private static readonly Guid OrganizationId = Guid.NewGuid();

    [Fact]
    public void Constructor_creates_open_period()
    {
        var period = new FiscalPeriod(OrganizationId, 2026, 2);

        Assert.Equal(2026, period.Year);
        Assert.Equal(2, period.Month);
        Assert.Equal(FiscalPeriodStatus.Open, period.Status);
    }

    [Fact]
    public void Close_marks_period_closed()
    {
        var period = new FiscalPeriod(OrganizationId, 2026, 3);

        period.Close();

        Assert.Equal(FiscalPeriodStatus.Closed, period.Status);
    }

    [Fact]
    public void Close_fails_when_already_closed()
    {
        var period = new FiscalPeriod(OrganizationId, 2026, 3);
        period.Close();

        Assert.Throws<DomainException>(() => period.Close());
    }

    [Fact]
    public void Reopen_marks_period_open()
    {
        var period = new FiscalPeriod(OrganizationId, 2026, 4);
        period.Close();

        period.Reopen();

        Assert.Equal(FiscalPeriodStatus.Open, period.Status);
    }

    [Fact]
    public void Reopen_fails_when_already_open()
    {
        var period = new FiscalPeriod(OrganizationId, 2026, 4);

        Assert.Throws<DomainException>(() => period.Reopen());
    }

    [Fact]
    public void Constructor_fails_for_invalid_month()
    {
        Assert.Throws<DomainException>(() => new FiscalPeriod(OrganizationId, 2026, 13));
    }
}
