using Ledgerly.Domain.Entities;
using Ledgerly.Domain.Enums;
using Ledgerly.Domain.Exceptions;
using Ledgerly.Domain.ValueObjects;

namespace Ledgerly.Domain.Tests;

public class JournalEntryTests
{
    private static readonly CurrencyCode Usd = new("USD");
    private static readonly CurrencyCode Eur = new("EUR");
    private static readonly Guid OrganizationId = Guid.NewGuid();
    private static readonly Guid CashAccountId = Guid.NewGuid();
    private static readonly Guid RevenueAccountId = Guid.NewGuid();

    [Fact]
    public void Constructor_sets_draft_status_and_properties()
    {
        var entry = CreateEntry("INV-001", "Customer invoice");

        Assert.Equal(OrganizationId, entry.OrganizationId);
        Assert.Equal(new DateOnly(2026, 1, 15), entry.EntryDate);
        Assert.Equal("INV-001", entry.Reference);
        Assert.Equal("Customer invoice", entry.Description);
        Assert.Equal(JournalEntryStatus.Draft, entry.Status);
        Assert.Equal(Usd, entry.BaseCurrency);
        Assert.Empty(entry.Lines);
    }

    [Fact]
    public void Constructor_throws_when_description_missing()
    {
        var exception = Assert.Throws<DomainException>(() =>
            new JournalEntry(
                OrganizationId,
                new DateOnly(2026, 1, 15),
                null,
                " ",
                Usd));

        Assert.Contains("description", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddLine_appends_balanced_line()
    {
        var entry = CreateEntry(null, "Sale");

        var line = entry.AddLine(
            CashAccountId,
            new Money(100m, Usd),
            new Money(0m, Usd),
            "Cash received");

        Assert.Single(entry.Lines);
        Assert.Equal(CashAccountId, line.AccountId);
        Assert.Equal(100m, line.Debit.Amount);
        Assert.Equal(0m, line.Credit.Amount);
        Assert.Equal("Cash received", line.Memo);
    }

    [Fact]
    public void AddLine_throws_when_both_sides_have_amounts()
    {
        var entry = CreateEntry(null, "Sale");

        var exception = Assert.Throws<DomainException>(() =>
            entry.AddLine(
                CashAccountId,
                new Money(100m, Usd),
                new Money(50m, Usd)));

        Assert.Contains("both debit and credit", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddLine_throws_when_currency_mismatches_base()
    {
        var entry = CreateEntry(null, "Sale");

        var exception = Assert.Throws<DomainException>(() =>
            entry.AddLine(
                CashAccountId,
                new Money(100m, Eur),
                new Money(0m, Eur)));

        Assert.Contains("base currency", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddLine_throws_when_entry_is_posted()
    {
        var entry = CreateBalancedEntry();
        entry.Post();

        Assert.Throws<DomainException>(() =>
            entry.AddLine(
                CashAccountId,
                new Money(10m, Usd),
                new Money(0m, Usd)));
    }

    [Fact]
    public void ValidateBalance_passes_for_balanced_entry()
    {
        var entry = CreateBalancedEntry();

        entry.ValidateBalance();

        Assert.Equal(2, entry.Lines.Count);
    }

    [Fact]
    public void ValidateBalance_throws_unbalanced_journal_exception()
    {
        var entry = CreateEntry(null, "Unbalanced");
        entry.AddLine(CashAccountId, new Money(100m, Usd), new Money(0m, Usd));
        entry.AddLine(RevenueAccountId, new Money(0m, Usd), new Money(75m, Usd));

        var exception = Assert.Throws<UnbalancedJournalException>(() => entry.ValidateBalance());

        Assert.Equal(100m, exception.Debits);
        Assert.Equal(75m, exception.Credits);
    }

    [Fact]
    public void ValidateBalance_throws_when_fewer_than_two_lines()
    {
        var entry = CreateEntry(null, "Single line");
        entry.AddLine(CashAccountId, new Money(100m, Usd), new Money(0m, Usd));

        Assert.Throws<DomainException>(() => entry.ValidateBalance());
    }

    [Fact]
    public void Post_marks_entry_as_posted_when_balanced()
    {
        var entry = CreateBalancedEntry();

        entry.Post();

        Assert.Equal(JournalEntryStatus.Posted, entry.Status);
    }

    [Fact]
    public void Post_throws_when_unbalanced()
    {
        var entry = CreateEntry(null, "Unbalanced");
        entry.AddLine(CashAccountId, new Money(100m, Usd), new Money(0m, Usd));
        entry.AddLine(RevenueAccountId, new Money(0m, Usd), new Money(50m, Usd));

        Assert.Throws<UnbalancedJournalException>(() => entry.Post());
    }

    [Fact]
    public void Post_throws_when_already_posted()
    {
        var entry = CreateBalancedEntry();
        entry.Post();

        Assert.Throws<DomainException>(() => entry.Post());
    }

    [Fact]
    public void CreateReversal_marks_original_reversed_and_returns_posted_mirror()
    {
        var entry = CreateBalancedEntry();
        entry.Post();

        var reversal = entry.CreateReversal();

        Assert.Equal(JournalEntryStatus.Reversed, entry.Status);
        Assert.Equal(JournalEntryStatus.Posted, reversal.Status);
        Assert.Equal(entry.Id, reversal.ReversalOfEntryId);
        Assert.Equal(2, reversal.Lines.Count);
        Assert.Equal(
            entry.Lines.First(line => line.AccountId == CashAccountId).Debit.Amount,
            reversal.Lines.First(line => line.AccountId == CashAccountId).Credit.Amount);
        Assert.Equal(
            entry.Lines.First(line => line.AccountId == RevenueAccountId).Credit.Amount,
            reversal.Lines.First(line => line.AccountId == RevenueAccountId).Debit.Amount);
    }

    [Fact]
    public void CreateReversal_throws_when_entry_is_draft()
    {
        var entry = CreateBalancedEntry();

        Assert.Throws<DomainException>(() => entry.CreateReversal());
    }

    private static JournalEntry CreateEntry(string? reference, string description) =>
        new(
            OrganizationId,
            new DateOnly(2026, 1, 15),
            reference,
            description,
            Usd);

    private static JournalEntry CreateBalancedEntry()
    {
        var entry = CreateEntry("JE-100", "Balanced entry");
        entry.AddLine(CashAccountId, new Money(250m, Usd), new Money(0m, Usd));
        entry.AddLine(RevenueAccountId, new Money(0m, Usd), new Money(250m, Usd));
        return entry;
    }
}
