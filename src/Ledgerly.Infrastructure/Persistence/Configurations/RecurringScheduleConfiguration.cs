using Ledgerly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ledgerly.Infrastructure.Persistence.Configurations;

public sealed class RecurringScheduleConfiguration : IEntityTypeConfiguration<RecurringSchedule>
{
    public void Configure(EntityTypeBuilder<RecurringSchedule> b)
    {
        b.ToTable("recurring_schedules");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(128).IsRequired();
        b.Property(x => x.CronExpression).HasMaxLength(128).IsRequired();
        b.Property(x => x.NextRunUtc).IsRequired();
        b.Property(x => x.IsPaused).IsRequired();
        b.Property(x => x.TransactionType).HasConversion<string>().HasMaxLength(16).IsRequired();
        b.Property(x => x.TemplateJson).IsRequired();
        b.HasIndex(x => new { x.OrganizationId, x.IsPaused, x.NextRunUtc });
    }
}

public sealed class RecurringScheduleRunConfiguration : IEntityTypeConfiguration<RecurringScheduleRun>
{
    public void Configure(EntityTypeBuilder<RecurringScheduleRun> b)
    {
        b.ToTable("recurring_schedule_runs");
        b.HasKey(x => x.Id);
        b.Property(x => x.OccurrenceUtc).IsRequired();
        b.Property(x => x.TransactionType).HasConversion<string>().HasMaxLength(16).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        b.HasIndex(x => new { x.RecurringScheduleId, x.OccurrenceUtc }).IsUnique();
    }
}
