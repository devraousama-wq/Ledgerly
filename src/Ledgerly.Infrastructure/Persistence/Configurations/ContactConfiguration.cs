using Ledgerly.Domain.Entities;
using Ledgerly.Domain.Enums;
using Ledgerly.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ledgerly.Infrastructure.Persistence.Configurations;

public sealed class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.ToTable("contacts");

        builder.HasKey(contact => contact.Id);

        builder.Property(contact => contact.OrganizationId)
            .IsRequired();

        builder.Property(contact => contact.ContactType)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(contact => contact.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(contact => contact.Email)
            .HasMaxLength(256);

        builder.Property(contact => contact.Phone)
            .HasMaxLength(64);

        builder.Property(contact => contact.DefaultCurrency)
            .HasConversion(
                currency => currency.Value,
                value => new CurrencyCode(value))
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(contact => contact.PaymentTerms)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(contact => contact.TaxId)
            .HasMaxLength(64);

        builder.Property(contact => contact.DefaultExpenseAccountId);

        builder.Property(contact => contact.IsArchived)
            .IsRequired();

        ConfigureAddress(builder, contact => contact.BillingAddress, "Billing");
        ConfigureAddress(builder, contact => contact.ShippingAddress, "Shipping");

        builder.HasIndex(contact => new { contact.OrganizationId, contact.ContactType, contact.Name });
    }

    private static void ConfigureAddress(
        EntityTypeBuilder<Contact> builder,
        System.Linq.Expressions.Expression<Func<Contact, Address?>> navigationExpression,
        string prefix)
    {
        builder.OwnsOne(navigationExpression, address =>
        {
            address.Property(a => a.Line1)
                .HasColumnName($"{prefix}Line1")
                .HasMaxLength(256);

            address.Property(a => a.Line2)
                .HasColumnName($"{prefix}Line2")
                .HasMaxLength(256);

            address.Property(a => a.City)
                .HasColumnName($"{prefix}City")
                .HasMaxLength(128);

            address.Property(a => a.State)
                .HasColumnName($"{prefix}State")
                .HasMaxLength(128);

            address.Property(a => a.PostalCode)
                .HasColumnName($"{prefix}PostalCode")
                .HasMaxLength(32);

            address.Property(a => a.Country)
                .HasColumnName($"{prefix}Country")
                .HasMaxLength(128);
        });
    }
}
