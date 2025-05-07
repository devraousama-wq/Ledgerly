using Ledgerly.Domain.Entities;
using Ledgerly.Domain.Enums;
using Ledgerly.Domain.Exceptions;
using Ledgerly.Domain.ValueObjects;

namespace Ledgerly.Domain.Tests;

public class BankStatementTests
{
    private static readonly Guid Org = Guid.NewGuid();
    private static readonly Guid BankAccount = Guid.NewGuid();
    private static readonly CurrencyCode Currency = new("USD");

    [Fact]
    public void ImportLines_adds_transactions_within_period()
    {
        var statement = CreateStatement(1000m, 1250m);

        statement.ImportLines(new[]
        {
            (new DateOnly(2026, 3, 5), new Money(200m, Currency), "Deposit", "DEP-1"),
            (new DateOnly(2026, 3, 10), new Money(50m, Currency), "Interest", null)
        });

        Assert.Equal(2, statement.Lines.Count);
        Assert.Equal(250m, statement.CalculateMovementTotal().Amount);
    }

    [Fact]
    public void MatchLine_links_journal_line_for_deposit()
    {
        var statement = CreateStatement(1000m, 1100m);
        var line = statement.AddManualLine(new DateOnly(2026, 3, 1), new Money(100m, Currency), "Deposit", null);
        var journalLineId = Guid.NewGuid();

        statement.MatchLine(line.Id, journalLineId, new Money(100m, Currency), new Money(0m, Currency));

        Assert.Equal(BankStatementLineStatus.Matched, line.Status);
        Assert.Equal(journalLineId, line.MatchedJournalLineId);
    }

    [Fact]
    public void Reconcile_locks_statement_when_balanced()
    {
        var statement = CreateStatement(1000m, 1100m);
        var line = statement.AddManualLine(new DateOnly(2026, 3, 1), new Money(100m, Currency), "Deposit", null);
        statement.MatchLine(line.Id, Guid.NewGuid(), new Money(100m, Currency), new Money(0m, Currency));

        statement.Reconcile();

        Assert.Equal(BankStatementStatus.Reconciled, statement.Status);
    }

    [Fact]
    public void Reconcile_throws_when_lines_are_pending()
    {
        var statement = CreateStatement(1000m, 1100m);
        statement.AddManualLine(new DateOnly(2026, 3, 1), new Money(100m, Currency), "Deposit", null);

        Assert.Throws<DomainException>(() => statement.Reconcile());
    }

    [Fact]
    public void Reconcile_throws_when_closing_balance_does_not_match()
    {
        var statement = CreateStatement(1000m, 1200m);
        var line = statement.AddManualLine(new DateOnly(2026, 3, 1), new Money(100m, Currency), "Deposit", null);
        statement.MatchLine(line.Id, Guid.NewGuid(), new Money(100m, Currency), new Money(0m, Currency));

        Assert.Throws<DomainException>(() => statement.Reconcile());
    }

    private static BankStatement CreateStatement(decimal opening, decimal closing) =>
        new(
            Org,
            BankAccount,
            new DateOnly(2026, 3, 1),
            new DateOnly(2026, 3, 31),
            new Money(opening, Currency),
            new Money(closing, Currency));
}
