using Ledgerly.Domain.Entities;
using Ledgerly.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ledgerly.Infrastructure.Persistence.Configurations;

public sealed class FiscalPeriodConfiguration : IEntityTypeConfiguration<FiscalPeriod>
{
    public void Configure(EntityTypeBuilder<FiscalPeriod> builder)
    {
        builder.ToTable("fiscal_periods");

        builder.HasKey(period => period.Id);

        builder.Property(period => period.OrganizationId)
            .IsRequired();

        builder.Property(period => period.Year)
            .IsRequired();

        builder.Property(period => period.Month)
            .IsRequired();

        builder.Property(period => period.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.HasIndex(period => new { period.OrganizationId, period.Year, period.Month })
            .IsUnique();
    }
}

public sealed class PeriodCloseAuditConfiguration : IEntityTypeConfiguration<PeriodCloseAudit>
{
    public void Configure(EntityTypeBuilder<PeriodCloseAudit> builder)
    {
        builder.ToTable("period_close_audits");

        builder.HasKey(audit => audit.Id);

        builder.Property(audit => audit.OrganizationId)
            .IsRequired();

        builder.Property(audit => audit.FiscalPeriodId)
            .IsRequired();

        builder.Property(audit => audit.Year)
            .IsRequired();

        builder.Property(audit => audit.Month)
            .IsRequired();

        builder.Property(audit => audit.Action)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.HasIndex(audit => new { audit.OrganizationId, audit.Year, audit.Month });
    }
}
