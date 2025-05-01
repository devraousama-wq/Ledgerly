using Ledgerly.Domain.Common;
using Ledgerly.Domain.Enums;
using Ledgerly.Domain.Exceptions;
using Ledgerly.Domain.ValueObjects;

namespace Ledgerly.Domain.Entities;

public sealed class Contact : OrganizationScopedEntity
{
    public ContactType ContactType { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Email { get; private set; }

    public string? Phone { get; private set; }

    public Address? BillingAddress { get; private set; }

    public Address? ShippingAddress { get; private set; }

    public CurrencyCode DefaultCurrency { get; private set; }

    public PaymentTerms PaymentTerms { get; private set; }

    public string? TaxId { get; private set; }

    public Guid? DefaultExpenseAccountId { get; private set; }

    public bool IsArchived { get; private set; }

    private Contact()
    {
        DefaultCurrency = new CurrencyCode("USD");
    }

    public Contact(
        Guid organizationId,
        ContactType contactType,
        string name,
        string? email,
        string? phone,
        Address? billingAddress,
        Address? shippingAddress,
        CurrencyCode defaultCurrency,
        PaymentTerms paymentTerms,
        string? taxId,
        Guid? defaultExpenseAccountId)
        : base(organizationId)
    {
        ContactType = contactType;
        SetName(name);
        SetEmail(email);
        SetPhone(phone);
        BillingAddress = NormalizeAddress(billingAddress);
        ShippingAddress = NormalizeAddress(shippingAddress);
        DefaultCurrency = defaultCurrency;
        PaymentTerms = paymentTerms;
        SetTaxId(taxId);
        SetDefaultExpenseAccountId(contactType, defaultExpenseAccountId);
    }

    public void Update(
        string name,
        string? email,
        string? phone,
        Address? billingAddress,
        Address? shippingAddress,
        CurrencyCode defaultCurrency,
        PaymentTerms paymentTerms,
        string? taxId,
        Guid? defaultExpenseAccountId)
    {
        if (IsArchived)
        {
            throw new DomainException("Cannot update an archived contact.");
        }

        SetName(name);
        SetEmail(email);
        SetPhone(phone);
        BillingAddress = NormalizeAddress(billingAddress);
        ShippingAddress = NormalizeAddress(shippingAddress);
        DefaultCurrency = defaultCurrency;
        PaymentTerms = paymentTerms;
        SetTaxId(taxId);
        SetDefaultExpenseAccountId(ContactType, defaultExpenseAccountId);
    }

    public void Archive()
    {
        if (IsArchived)
        {
            throw new DomainException("Contact is already archived.");
        }

        IsArchived = true;
        Touch();
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Contact name is required.");
        }

        Name = name.Trim();
        Touch();
    }

    private void SetEmail(string? email)
    {
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        Touch();
    }

    private void SetPhone(string? phone)
    {
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        Touch();
    }

    private void SetTaxId(string? taxId)
    {
        TaxId = string.IsNullOrWhiteSpace(taxId) ? null : taxId.Trim();
        Touch();
    }

    private static Address? NormalizeAddress(Address? address)
    {
        if (address is null || address.IsEmpty)
        {
            return null;
        }

        return address;
    }

    private void SetDefaultExpenseAccountId(ContactType contactType, Guid? defaultExpenseAccountId)
    {
        if (contactType == ContactType.Customer && defaultExpenseAccountId.HasValue)
        {
            throw new DomainException("Customers cannot have a default expense account.");
        }

        DefaultExpenseAccountId = contactType == ContactType.Vendor ? defaultExpenseAccountId : null;
        Touch();
    }
}
