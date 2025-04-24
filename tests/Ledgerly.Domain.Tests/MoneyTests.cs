using Ledgerly.Domain.Exceptions;
using Ledgerly.Domain.ValueObjects;

namespace Ledgerly.Domain.Tests;

public class MoneyTests
{
    private static readonly CurrencyCode Usd = new("USD");
    private static readonly CurrencyCode Eur = new("EUR");

    [Fact]
    public void Add_combines_amounts_with_same_currency()
    {
        var left = new Money(10.50m, Usd);
        var right = new Money(4.25m, Usd);

        var result = left.Add(right);

        Assert.Equal(14.75m, result.Amount);
        Assert.Equal(Usd, result.Currency);
    }

    [Fact]
    public void Subtract_reduces_amount_with_same_currency()
    {
        var left = new Money(20m, Usd);
        var right = new Money(7.50m, Usd);

        var result = left.Subtract(right);

        Assert.Equal(12.50m, result.Amount);
        Assert.Equal(Usd, result.Currency);
    }

    [Fact]
    public void Add_throws_when_currencies_differ()
    {
        var left = new Money(10m, Usd);
        var right = new Money(5m, Eur);

        var exception = Assert.Throws<DomainException>(() => left.Add(right));
        Assert.Contains("different currencies", exception.Message);
    }

    [Fact]
    public void Subtract_throws_when_currencies_differ()
    {
        var left = new Money(10m, Usd);
        var right = new Money(5m, Eur);

        Assert.Throws<DomainException>(() => left.Subtract(right));
    }

    [Fact]
    public void Round_uses_bankers_rounding()
    {
        var money = new Money(2.005m, Usd);

        var rounded = money.Round(2);

        Assert.Equal(2.00m, rounded.Amount);
    }

    [Fact]
    public void Round_rounds_half_to_even()
    {
        var money = new Money(2.015m, Usd);

        var rounded = money.Round(2);

        Assert.Equal(2.02m, rounded.Amount);
    }

    [Fact]
    public void Equality_compares_amount_and_currency()
    {
        var first = new Money(100m, Usd);
        var second = new Money(100m, Usd);
        var third = new Money(100m, Eur);

        Assert.Equal(first, second);
        Assert.NotEqual(first, third);
    }

    [Fact]
    public void Operator_plus_combines_amounts()
    {
        var left = new Money(3m, Usd);
        var right = new Money(2m, Usd);

        var result = left + right;

        Assert.Equal(5m, result.Amount);
    }
}
