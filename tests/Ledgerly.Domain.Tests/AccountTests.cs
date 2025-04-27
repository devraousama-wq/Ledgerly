using Ledgerly.Domain.Entities;
using Ledgerly.Domain.Enums;
using Ledgerly.Domain.Exceptions;
using Ledgerly.Domain.ValueObjects;

namespace Ledgerly.Domain.Tests;

public class AccountTests
{
    private static readonly CurrencyCode Usd = new("USD");
    private static readonly CurrencyCode Eur = new("EUR");
    private static readonly Guid OrganizationId = Guid.NewGuid();

    [Fact]
    public void Constructor_sets_properties()
    {
        var openingBalance = new Money(1500m, Usd);

        var account = new Account(
            OrganizationId,
            "1000",
            "Cash",
            AccountType.Asset,
            Usd,
            openingBalance);

        Assert.Equal(OrganizationId, account.OrganizationId);
        Assert.Equal("1000", account.Code);
        Assert.Equal("Cash", account.Name);
        Assert.Equal(AccountType.Asset, account.AccountType);
        Assert.Null(account.ParentAccountId);
        Assert.Equal(Usd, account.CurrencyCode);
        Assert.Equal(1500m, account.OpeningBalance.Amount);
        Assert.False(account.IsArchived);
    }

    [Fact]
    public void Constructor_trims_code_and_name()
    {
        var account = new Account(
            OrganizationId,
            " 2000 ",
            " Accounts Receivable ",
            AccountType.Asset,
            Usd,
            new Money(0m, Usd));

        Assert.Equal("2000", account.Code);
        Assert.Equal("Accounts Receivable", account.Name);
    }

    [Fact]
    public void Constructor_throws_when_code_missing()
    {
        var exception = Assert.Throws<DomainException>(() =>
            new Account(
                OrganizationId,
                " ",
                "Cash",
                AccountType.Asset,
                Usd,
                new Money(0m, Usd)));

        Assert.Contains("code", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_throws_when_name_missing()
    {
        var exception = Assert.Throws<DomainException>(() =>
            new Account(
                OrganizationId,
                "1000",
                "",
                AccountType.Asset,
                Usd,
                new Money(0m, Usd)));

        Assert.Contains("name", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_throws_when_opening_balance_currency_mismatches()
    {
        var exception = Assert.Throws<DomainException>(() =>
            new Account(
                OrganizationId,
                "1000",
                "Cash",
                AccountType.Asset,
                Usd,
                new Money(100m, Eur)));

        Assert.Contains("currency", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Update_changes_mutable_fields()
    {
        var account = CreateAccount("1000", "Cash");

        account.Update(
            "1100",
            "Petty Cash",
            AccountType.Asset,
            Usd,
            new Money(250m, Usd),
            null);

        Assert.Equal("1100", account.Code);
        Assert.Equal("Petty Cash", account.Name);
        Assert.Equal(250m, account.OpeningBalance.Amount);
    }

    [Fact]
    public void Update_throws_when_archived()
    {
        var account = CreateAccount("1000", "Cash");
        account.Archive();

        Assert.Throws<DomainException>(() =>
            account.Update(
                "1100",
                "Petty Cash",
                AccountType.Asset,
                Usd,
                new Money(0m, Usd),
                null));
    }

    [Fact]
    public void SetParent_assigns_parent_id()
    {
        var parentId = Guid.NewGuid();
        var account = CreateAccount("2000", "Receivables");

        account.SetParent(parentId);

        Assert.Equal(parentId, account.ParentAccountId);
    }

    [Fact]
    public void SetParent_throws_when_archived()
    {
        var account = CreateAccount("2000", "Receivables");
        account.Archive();

        Assert.Throws<DomainException>(() => account.SetParent(Guid.NewGuid()));
    }

    [Fact]
    public void Archive_marks_account_as_archived()
    {
        var account = CreateAccount("3000", "Inventory");

        account.Archive();

        Assert.True(account.IsArchived);
    }

    [Fact]
    public void Archive_throws_when_already_archived()
    {
        var account = CreateAccount("3000", "Inventory");
        account.Archive();

        Assert.Throws<DomainException>(() => account.Archive());
    }

    [Fact]
    public void Restore_clears_archived_flag()
    {
        var account = CreateAccount("4000", "Equipment");
        account.Archive();

        account.Restore();

        Assert.False(account.IsArchived);
    }

    [Fact]
    public void Restore_throws_when_not_archived()
    {
        var account = CreateAccount("4000", "Equipment");

        Assert.Throws<DomainException>(() => account.Restore());
    }

    [Fact]
    public void Constructor_accepts_parent_account_id()
    {
        var parentId = Guid.NewGuid();

        var account = new Account(
            OrganizationId,
            "1010",
            "Operating Cash",
            AccountType.Asset,
            Usd,
            new Money(0m, Usd),
            parentId);

        Assert.Equal(parentId, account.ParentAccountId);
    }

    private static Account CreateAccount(string code, string name) =>
        new(
            OrganizationId,
            code,
            name,
            AccountType.Asset,
            Usd,
            new Money(0m, Usd));
}
