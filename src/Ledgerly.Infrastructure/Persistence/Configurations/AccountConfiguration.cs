using Ledgerly.Domain.Entities;
using Ledgerly.Domain.Enums;
using Ledgerly.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ledgerly.Infrastructure.Persistence.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts");

        builder.HasKey(account => account.Id);

        builder.Property(account => account.OrganizationId)
            .IsRequired();

        builder.Property(account => account.Code)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(account => account.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(account => account.AccountType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(account => account.CurrencyCode)
            .HasConversion(
                currency => currency.Value,
                value => new CurrencyCode(value))
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(account => account.IsArchived)
            .IsRequired();

        builder.ComplexProperty(account => account.OpeningBalance, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("OpeningBalanceAmount")
                .HasPrecision(18, 4)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("OpeningBalanceCurrency")
                .HasConversion(
                    currency => currency.Value,
                    value => new CurrencyCode(value))
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.HasIndex(account => new { account.OrganizationId, account.Code })
            .IsUnique();

        builder.HasOne(account => account.ParentAccount)
            .WithMany(account => account.ChildAccounts)
            .HasForeignKey(account => account.ParentAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
