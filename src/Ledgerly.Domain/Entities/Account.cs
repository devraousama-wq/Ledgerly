using Ledgerly.Domain.Common;
using Ledgerly.Domain.Enums;
using Ledgerly.Domain.Exceptions;
using Ledgerly.Domain.ValueObjects;

namespace Ledgerly.Domain.Entities;

public sealed class Account : OrganizationScopedEntity
{
    private readonly List<Account> _childAccounts = new();

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public AccountType AccountType { get; private set; }

    public Guid? ParentAccountId { get; private set; }

    public CurrencyCode CurrencyCode { get; private set; }

    public bool IsArchived { get; private set; }

    public Money OpeningBalance { get; private set; }

    public Account? ParentAccount { get; private set; }

    public IReadOnlyCollection<Account> ChildAccounts => _childAccounts.AsReadOnly();

    private Account()
    {
        CurrencyCode = new CurrencyCode("USD");
        OpeningBalance = new Money(0m, CurrencyCode);
    }

    public Account(
        Guid organizationId,
        string code,
        string name,
        AccountType accountType,
        CurrencyCode currencyCode,
        Money openingBalance,
        Guid? parentAccountId = null)
        : base(organizationId)
    {
        SetCode(code);
        SetName(name);
        AccountType = accountType;
        CurrencyCode = currencyCode;
        SetOpeningBalance(openingBalance);
        ParentAccountId = parentAccountId;
    }

    public void Update(
        string code,
        string name,
        AccountType accountType,
        CurrencyCode currencyCode,
        Money openingBalance,
        Guid? parentAccountId)
    {
        if (IsArchived)
        {
            throw new DomainException("Cannot update an archived account.");
        }

        SetCode(code);
        SetName(name);
        AccountType = accountType;
        CurrencyCode = currencyCode;
        SetOpeningBalance(openingBalance);
        ParentAccountId = parentAccountId;
    }

    public void SetParent(Guid? parentAccountId)
    {
        if (IsArchived)
        {
            throw new DomainException("Cannot change parent of an archived account.");
        }

        ParentAccountId = parentAccountId;
        Touch();
    }

    public void Archive()
    {
        if (IsArchived)
        {
            throw new DomainException("Account is already archived.");
        }

        IsArchived = true;
        Touch();
    }

    public void Restore()
    {
        if (!IsArchived)
        {
            throw new DomainException("Account is not archived.");
        }

        IsArchived = false;
        Touch();
    }

    private void SetCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("Account code is required.");
        }

        Code = code.Trim();
        Touch();
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Account name is required.");
        }

        Name = name.Trim();
        Touch();
    }

    private void SetOpeningBalance(Money openingBalance)
    {
        if (openingBalance.Currency != CurrencyCode)
        {
            throw new DomainException("Opening balance currency must match account currency.");
        }

        OpeningBalance = openingBalance;
        Touch();
    }
}
