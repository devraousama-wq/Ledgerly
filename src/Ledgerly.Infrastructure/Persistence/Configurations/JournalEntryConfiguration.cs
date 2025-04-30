using Ledgerly.Domain.Entities;
using Ledgerly.Domain.Enums;
using Ledgerly.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ledgerly.Infrastructure.Persistence.Configurations;

public sealed class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
{
    public void Configure(EntityTypeBuilder<JournalEntry> builder)
    {
        builder.ToTable("journal_entries");

        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.OrganizationId)
            .IsRequired();

        builder.Property(entry => entry.EntryDate)
            .IsRequired();

        builder.Property(entry => entry.Reference)
            .HasMaxLength(64);

        builder.Property(entry => entry.Description)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(entry => entry.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(entry => entry.BaseCurrency)
            .HasConversion(
                currency => currency.Value,
                value => new CurrencyCode(value))
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(entry => entry.ReversalOfEntryId);

        builder.HasMany(entry => entry.Lines)
            .WithOne()
            .HasForeignKey(line => line.JournalEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(entry => entry.Lines)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(entry => new { entry.OrganizationId, entry.EntryDate });
    }
}

public sealed class JournalLineConfiguration : IEntityTypeConfiguration<JournalLine>
{
    public void Configure(EntityTypeBuilder<JournalLine> builder)
    {
        builder.ToTable("journal_lines");

        builder.HasKey(line => line.Id);

        builder.Property(line => line.JournalEntryId)
            .IsRequired();

        builder.Property(line => line.AccountId)
            .IsRequired();

        builder.Property(line => line.Memo)
            .HasMaxLength(256);

        builder.ComplexProperty(line => line.Debit, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("DebitAmount")
                .HasPrecision(18, 4)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("DebitCurrency")
                .HasConversion(
                    currency => currency.Value,
                    value => new CurrencyCode(value))
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.ComplexProperty(line => line.Credit, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("CreditAmount")
                .HasPrecision(18, 4)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("CreditCurrency")
                .HasConversion(
                    currency => currency.Value,
                    value => new CurrencyCode(value))
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.HasIndex(line => line.AccountId);
    }
}
