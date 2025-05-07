using Ledgerly.Domain.Entities;
using Ledgerly.Domain.Enums;
using Ledgerly.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ledgerly.Infrastructure.Persistence.Configurations;

public sealed class BankStatementConfiguration : IEntityTypeConfiguration<BankStatement>
{
    public void Configure(EntityTypeBuilder<BankStatement> builder)
    {
        builder.ToTable("bank_statements");

        builder.HasKey(statement => statement.Id);

        builder.Property(statement => statement.OrganizationId)
            .IsRequired();

        builder.Property(statement => statement.BankAccountId)
            .IsRequired();

        builder.Property(statement => statement.PeriodStart)
            .IsRequired();

        builder.Property(statement => statement.PeriodEnd)
            .IsRequired();

        builder.Property(statement => statement.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.ComplexProperty(statement => statement.OpeningBalance, money =>
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

        builder.ComplexProperty(statement => statement.ClosingBalance, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("ClosingBalanceAmount")
                .HasPrecision(18, 4)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("ClosingBalanceCurrency")
                .HasConversion(
                    currency => currency.Value,
                    value => new CurrencyCode(value))
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.HasMany<BankStatementLine>()
            .WithOne()
            .HasForeignKey(line => line.BankStatementId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(statement => statement.Lines)
            .HasField("_lines")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(statement => new { statement.OrganizationId, statement.BankAccountId, statement.PeriodStart });
    }
}

public sealed class BankStatementLineConfiguration : IEntityTypeConfiguration<BankStatementLine>
{
    public void Configure(EntityTypeBuilder<BankStatementLine> builder)
    {
        builder.ToTable("bank_statement_lines");

        builder.HasKey(line => line.Id);

        builder.Property(line => line.BankStatementId)
            .IsRequired();

        builder.Property(line => line.TransactionDate)
            .IsRequired();

        builder.Property(line => line.Description)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(line => line.Reference)
            .HasMaxLength(128);

        builder.Property(line => line.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(line => line.MatchedJournalLineId);

        builder.Property(line => line.CreatedJournalEntryId);

        builder.ComplexProperty(line => line.Amount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("Amount")
                .HasPrecision(18, 4)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("Currency")
                .HasConversion(
                    currency => currency.Value,
                    value => new CurrencyCode(value))
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.HasIndex(line => line.MatchedJournalLineId);
    }
}
