using Ledgerly.Domain.Entities;
using Ledgerly.Domain.Enums;
using Ledgerly.Domain.Exceptions;

namespace Ledgerly.Domain.Tests;

public class RecurringScheduleTests
{
    static readonly Guid Org = Guid.NewGuid();

    [Fact]
    public void Pause_sets_paused()
    {
        var s = Create();
        s.Pause();
        Assert.True(s.IsPaused);
    }

    [Fact]
    public void Resume_clears_paused()
    {
        var s = Create();
        s.Pause();
        s.Resume(DateTimeOffset.UtcNow.AddDays(1));
        Assert.False(s.IsPaused);
    }

    [Fact]
    public void Create_requires_name()
    {
        Assert.Throws<DomainException>(() => new RecurringSchedule(Org, " ", "0 0 * * *", DateTimeOffset.UtcNow, RecurringTransactionType.Invoice, "{}"));
    }

    static RecurringSchedule Create()
        => new(Org, "Monthly rent", "0 0 1 * *", new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero), RecurringTransactionType.Invoice, "{}");
}
