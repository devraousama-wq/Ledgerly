using NCrontab;

namespace Ledgerly.Application.Recurring;

public static class CronScheduleHelper
{
    public static DateTimeOffset GetNextOccurrence(string cronExpression, DateTimeOffset fromUtc)
    {
        var schedule = CrontabSchedule.Parse(cronExpression);
        var next = schedule.GetNextOccurrence(fromUtc.UtcDateTime);
        return new DateTimeOffset(DateTime.SpecifyKind(next, DateTimeKind.Utc));
    }

    public static IReadOnlyList<DateTimeOffset> GetNextOccurrences(string cronExpression, DateTimeOffset fromUtc, int count)
    {
        if (count <= 0)
            return Array.Empty<DateTimeOffset>();

        var schedule = CrontabSchedule.Parse(cronExpression);
        var results = new List<DateTimeOffset>(count);
        var cursor = fromUtc.UtcDateTime;
        for (var i = 0; i < count; i++)
        {
            var next = schedule.GetNextOccurrence(cursor);
            results.Add(new DateTimeOffset(DateTime.SpecifyKind(next, DateTimeKind.Utc)));
            cursor = next;
        }

        return results;
    }

    public static bool IsValid(string cronExpression)
    {
        try
        {
            CrontabSchedule.Parse(cronExpression);
            return true;
        }
        catch (CrontabException)
        {
            return false;
        }
    }
}
