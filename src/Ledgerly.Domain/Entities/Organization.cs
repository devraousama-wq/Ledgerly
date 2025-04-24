using Ledgerly.Domain.Common;
using Ledgerly.Domain.Exceptions;
using Ledgerly.Domain.ValueObjects;

namespace Ledgerly.Domain.Entities;

public sealed class Organization : Entity
{
    public string Name { get; private set; } = string.Empty;

    public CurrencyCode BaseCurrency { get; private set; }

    public int FiscalYearStartMonth { get; private set; }

    private Organization()
    {
        BaseCurrency = new CurrencyCode("USD");
    }

    public Organization(string name, CurrencyCode baseCurrency, int fiscalYearStartMonth)
    {
        SetName(name);
        BaseCurrency = baseCurrency;
        SetFiscalYearStartMonth(fiscalYearStartMonth);
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Organization name is required.");
        }

        Name = name.Trim();
        Touch();
    }

    public void SetBaseCurrency(CurrencyCode baseCurrency)
    {
        BaseCurrency = baseCurrency;
        Touch();
    }

    public void SetFiscalYearStartMonth(int month)
    {
        if (month is < 1 or > 12)
        {
            throw new DomainException("Fiscal year start month must be between 1 and 12.");
        }

        FiscalYearStartMonth = month;
        Touch();
    }
}
