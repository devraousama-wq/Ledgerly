using Ledgerly.Domain.Entities;
using Ledgerly.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ledgerly.Infrastructure.Persistence.Configurations;

public sealed class TaxCodeConfiguration : IEntityTypeConfiguration<TaxCode>
{
    public void Configure(EntityTypeBuilder<TaxCode> builder)
    {
        builder.ToTable("tax_codes");

        builder.HasKey(taxCode => taxCode.Id);

        builder.Property(taxCode => taxCode.OrganizationId)
            .IsRequired();

        builder.Property(taxCode => taxCode.Code)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(taxCode => taxCode.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(taxCode => taxCode.TaxType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(taxCode => taxCode.Rate)
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(taxCode => taxCode.LiabilityAccountId)
            .IsRequired();

        builder.Property(taxCode => taxCode.IsArchived)
            .IsRequired();

        builder.HasIndex(taxCode => new { taxCode.OrganizationId, taxCode.Code })
            .IsUnique();

        builder.HasMany<TaxCodeComponent>()
            .WithOne()
            .HasForeignKey(component => component.TaxCodeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(taxCode => taxCode.Components)
            .HasField("_components")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class TaxCodeComponentConfiguration : IEntityTypeConfiguration<TaxCodeComponent>
{
    public void Configure(EntityTypeBuilder<TaxCodeComponent> builder)
    {
        builder.ToTable("tax_code_components");

        builder.HasKey(component => component.Id);

        builder.Property(component => component.TaxCodeId)
            .IsRequired();

        builder.Property(component => component.Name)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(component => component.Rate)
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(component => component.Sequence)
            .IsRequired();

        builder.Property(component => component.AppliesOnPrevious)
            .IsRequired();
    }
}
