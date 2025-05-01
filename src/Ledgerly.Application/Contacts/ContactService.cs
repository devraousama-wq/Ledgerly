using Ledgerly.Application.Abstractions;
using Ledgerly.Domain.Common;
using Ledgerly.Domain.Entities;
using Ledgerly.Domain.Enums;
using Ledgerly.Domain.Exceptions;
using Ledgerly.Domain.ValueObjects;

namespace Ledgerly.Application.Contacts;

public sealed class ContactService
{
    private readonly IContactRepository _contactRepository;
    private readonly IAccountRepository _accountRepository;

    public ContactService(IContactRepository contactRepository, IAccountRepository accountRepository)
    {
        _contactRepository = contactRepository;
        _accountRepository = accountRepository;
    }

    public async Task<Result<ContactDto>> CreateAsync(
        CreateContactRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var expenseAccountValidation = await ValidateExpenseAccountAsync(
                request.OrganizationId,
                request.ContactType,
                request.DefaultExpenseAccountId,
                cancellationToken);

            if (expenseAccountValidation.IsFailure)
            {
                return Result<ContactDto>.Failure(expenseAccountValidation.Error!);
            }

            var contact = new Contact(
                request.OrganizationId,
                request.ContactType,
                request.Name,
                request.Email,
                request.Phone,
                MapToAddress(request.BillingAddress),
                MapToAddress(request.ShippingAddress),
                new CurrencyCode(request.DefaultCurrency),
                request.PaymentTerms,
                request.TaxId,
                request.DefaultExpenseAccountId);

            await _contactRepository.AddAsync(contact, cancellationToken);

            return Result<ContactDto>.Success(MapToDto(contact));
        }
        catch (DomainException exception)
        {
            return Result<ContactDto>.Failure(exception.Message);
        }
    }

    public async Task<Result<ContactDto>> GetByIdAsync(
        Guid organizationId,
        Guid contactId,
        CancellationToken cancellationToken = default)
    {
        var contact = await _contactRepository.GetByIdAsync(organizationId, contactId, cancellationToken);

        if (contact is null)
        {
            return Result<ContactDto>.Failure("Contact not found.");
        }

        return Result<ContactDto>.Success(MapToDto(contact));
    }

    public async Task<Result<IReadOnlyList<ContactDto>>> ListByTypeAsync(
        Guid organizationId,
        ContactType contactType,
        bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var contacts = await _contactRepository.ListByTypeAsync(
            organizationId,
            contactType,
            includeArchived,
            cancellationToken);

        var dtos = contacts.Select(MapToDto).ToList();
        return Result<IReadOnlyList<ContactDto>>.Success(dtos);
    }

    public async Task<Result<ContactDto>> UpdateAsync(
        Guid organizationId,
        Guid contactId,
        UpdateContactRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var contact = await _contactRepository.GetByIdAsync(organizationId, contactId, cancellationToken);

            if (contact is null)
            {
                return Result<ContactDto>.Failure("Contact not found.");
            }

            var expenseAccountValidation = await ValidateExpenseAccountAsync(
                organizationId,
                contact.ContactType,
                request.DefaultExpenseAccountId,
                cancellationToken);

            if (expenseAccountValidation.IsFailure)
            {
                return Result<ContactDto>.Failure(expenseAccountValidation.Error!);
            }

            contact.Update(
                request.Name,
                request.Email,
                request.Phone,
                MapToAddress(request.BillingAddress),
                MapToAddress(request.ShippingAddress),
                new CurrencyCode(request.DefaultCurrency),
                request.PaymentTerms,
                request.TaxId,
                request.DefaultExpenseAccountId);

            await _contactRepository.UpdateAsync(contact, cancellationToken);

            return Result<ContactDto>.Success(MapToDto(contact));
        }
        catch (DomainException exception)
        {
            return Result<ContactDto>.Failure(exception.Message);
        }
    }

    public async Task<Result<ContactDto>> ArchiveAsync(
        Guid organizationId,
        Guid contactId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var contact = await _contactRepository.GetByIdAsync(organizationId, contactId, cancellationToken);

            if (contact is null)
            {
                return Result<ContactDto>.Failure("Contact not found.");
            }

            contact.Archive();
            await _contactRepository.UpdateAsync(contact, cancellationToken);

            return Result<ContactDto>.Success(MapToDto(contact));
        }
        catch (DomainException exception)
        {
            return Result<ContactDto>.Failure(exception.Message);
        }
    }

    private async Task<Result> ValidateExpenseAccountAsync(
        Guid organizationId,
        ContactType contactType,
        Guid? defaultExpenseAccountId,
        CancellationToken cancellationToken)
    {
        if (!defaultExpenseAccountId.HasValue)
        {
            return Result.Success();
        }

        if (contactType != ContactType.Vendor)
        {
            return Result.Failure("Default expense account is only valid for vendors.");
        }

        var account = await _accountRepository.GetByIdAsync(
            organizationId,
            defaultExpenseAccountId.Value,
            cancellationToken);

        if (account is null)
        {
            return Result.Failure("Default expense account not found.");
        }

        if (account.IsArchived)
        {
            return Result.Failure("Default expense account is archived.");
        }

        if (account.AccountType != AccountType.Expense)
        {
            return Result.Failure("Default expense account must be an expense account.");
        }

        return Result.Success();
    }

    private static Address? MapToAddress(AddressDto? address) =>
        address is null
            ? null
            : new Address(
                address.Line1,
                address.Line2,
                address.City,
                address.State,
                address.PostalCode,
                address.Country);

    private static AddressDto? MapToDto(Address? address) =>
        address is null
            ? null
            : new AddressDto(
                address.Line1,
                address.Line2,
                address.City,
                address.State,
                address.PostalCode,
                address.Country);

    private static ContactDto MapToDto(Contact contact) =>
        new(
            contact.Id,
            contact.OrganizationId,
            contact.ContactType,
            contact.Name,
            contact.Email,
            contact.Phone,
            MapToDto(contact.BillingAddress),
            MapToDto(contact.ShippingAddress),
            contact.DefaultCurrency.Value,
            contact.PaymentTerms,
            contact.TaxId,
            contact.DefaultExpenseAccountId,
            contact.IsArchived,
            contact.CreatedAt,
            contact.UpdatedAt);
}
