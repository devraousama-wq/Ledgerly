using Ledgerly.Domain.Entities;
using Ledgerly.Domain.Enums;
using Ledgerly.Domain.Exceptions;
using Ledgerly.Domain.ValueObjects;

namespace Ledgerly.Domain.Tests;

public class ContactTests
{
    private static readonly CurrencyCode Usd = new("USD");
    private static readonly Guid OrganizationId = Guid.NewGuid();
    private static readonly Address SampleAddress = new(
        "100 Main St",
        "Suite 200",
        "Springfield",
        "IL",
        "62701",
        "US");

    [Fact]
    public void Constructor_sets_properties_for_customer()
    {
        var contact = CreateCustomer("Acme Corp");

        Assert.Equal(OrganizationId, contact.OrganizationId);
        Assert.Equal(ContactType.Customer, contact.ContactType);
        Assert.Equal("Acme Corp", contact.Name);
        Assert.Equal("billing@acme.com", contact.Email);
        Assert.Equal("+1-555-0100", contact.Phone);
        Assert.NotNull(contact.BillingAddress);
        Assert.Equal("100 Main St", contact.BillingAddress!.Line1);
        Assert.Null(contact.DefaultExpenseAccountId);
        Assert.False(contact.IsArchived);
    }

    [Fact]
    public void Constructor_sets_vendor_expense_account()
    {
        var expenseAccountId = Guid.NewGuid();

        var contact = new Contact(
            OrganizationId,
            ContactType.Vendor,
            "Office Supplies Inc",
            null,
            null,
            null,
            null,
            Usd,
            PaymentTerms.Net30,
            "TAX-123",
            expenseAccountId);

        Assert.Equal(ContactType.Vendor, contact.ContactType);
        Assert.Equal(expenseAccountId, contact.DefaultExpenseAccountId);
        Assert.Equal("TAX-123", contact.TaxId);
    }

    [Fact]
    public void Constructor_trims_name()
    {
        var contact = CreateCustomer("  Acme Corp  ");

        Assert.Equal("Acme Corp", contact.Name);
    }

    [Fact]
    public void Constructor_throws_when_name_missing()
    {
        var exception = Assert.Throws<DomainException>(() =>
            CreateCustomer(" "));

        Assert.Contains("name", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_throws_when_customer_has_expense_account()
    {
        var exception = Assert.Throws<DomainException>(() =>
            new Contact(
                OrganizationId,
                ContactType.Customer,
                "Acme Corp",
                null,
                null,
                null,
                null,
                Usd,
                PaymentTerms.Net30,
                null,
                Guid.NewGuid()));

        Assert.Contains("expense account", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_normalizes_empty_addresses_to_null()
    {
        var contact = new Contact(
            OrganizationId,
            ContactType.Customer,
            "Acme Corp",
            null,
            null,
            new Address(string.Empty, null, string.Empty, string.Empty, string.Empty, string.Empty),
            null,
            Usd,
            PaymentTerms.DueOnReceipt,
            null,
            null);

        Assert.Null(contact.BillingAddress);
    }

    [Fact]
    public void Update_changes_mutable_fields()
    {
        var contact = CreateCustomer("Acme Corp");

        contact.Update(
            "Acme International",
            "info@acme.com",
            null,
            SampleAddress,
            null,
            Usd,
            PaymentTerms.Net60,
            "TAX-999",
            null);

        Assert.Equal("Acme International", contact.Name);
        Assert.Equal("info@acme.com", contact.Email);
        Assert.Equal(PaymentTerms.Net60, contact.PaymentTerms);
        Assert.Equal("TAX-999", contact.TaxId);
    }

    [Fact]
    public void Update_throws_when_archived()
    {
        var contact = CreateCustomer("Acme Corp");
        contact.Archive();

        Assert.Throws<DomainException>(() =>
            contact.Update(
                "Acme International",
                null,
                null,
                null,
                null,
                Usd,
                PaymentTerms.Net30,
                null,
                null));
    }

    [Fact]
    public void Archive_marks_contact_as_archived()
    {
        var contact = CreateCustomer("Acme Corp");

        contact.Archive();

        Assert.True(contact.IsArchived);
    }

    [Fact]
    public void Archive_throws_when_already_archived()
    {
        var contact = CreateCustomer("Acme Corp");
        contact.Archive();

        Assert.Throws<DomainException>(() => contact.Archive());
    }

    private static Contact CreateCustomer(string name) =>
        new(
            OrganizationId,
            ContactType.Customer,
            name,
            "billing@acme.com",
            "+1-555-0100",
            SampleAddress,
            null,
            Usd,
            PaymentTerms.Net30,
            null,
            null);
}
