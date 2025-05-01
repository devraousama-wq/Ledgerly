using Ledgerly.Domain.Enums;

namespace Ledgerly.Application.Contacts;

public sealed record CreateContactRequest(
    Guid OrganizationId,
    ContactType ContactType,
    string Name,
    string? Email,
    string? Phone,
    AddressDto? BillingAddress,
    AddressDto? ShippingAddress,
    string DefaultCurrency,
    PaymentTerms PaymentTerms,
    string? TaxId,
    Guid? DefaultExpenseAccountId);
