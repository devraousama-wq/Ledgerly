using Ledgerly.Domain.Common;
using Ledgerly.Domain.Enums;
using Ledgerly.Domain.Exceptions;

namespace Ledgerly.Domain.Entities;

public sealed class TaxCode : OrganizationScopedEntity
{
    private readonly List<TaxCodeComponent> _components = new();

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public TaxType TaxType { get; private set; }

    public decimal Rate { get; private set; }

    public Guid LiabilityAccountId { get; private set; }

    public bool IsCompound => _components.Count > 1;

    public bool IsArchived { get; private set; }

    public IReadOnlyCollection<TaxCodeComponent> Components => _components.AsReadOnly();

    private TaxCode()
    {
    }

    public TaxCode(
        Guid organizationId,
        string code,
        string name,
        TaxType taxType,
        decimal rate,
        Guid liabilityAccountId,
        IEnumerable<(string Name, decimal Rate, int Sequence, bool AppliesOnPrevious)>? components = null)
        : base(organizationId)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("Tax code is required.");
        }

        if (liabilityAccountId == Guid.Empty)
        {
            throw new DomainException("Tax liability account is required.");
        }

        Code = code.Trim();
        Name = name.Trim();
        TaxType = taxType;
        LiabilityAccountId = liabilityAccountId;

        var componentList = components?.ToList() ?? new List<(string, decimal, int, bool)>();

        if (componentList.Count > 1)
        {
            SetComponents(componentList);
            Rate = GetEffectiveRate();
        }
        else
        {
            if (rate < 0m)
            {
                throw new DomainException("Tax rate cannot be negative.");
            }

            Rate = rate;
        }
    }

    public void Update(
        string code,
        string name,
        TaxType taxType,
        decimal rate,
        Guid liabilityAccountId,
        IEnumerable<(string Name, decimal Rate, int Sequence, bool AppliesOnPrevious)>? components = null)
    {
        if (IsArchived)
        {
            throw new DomainException("Cannot update an archived tax code.");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("Tax code is required.");
        }

        if (liabilityAccountId == Guid.Empty)
        {
            throw new DomainException("Tax liability account is required.");
        }

        Code = code.Trim();
        Name = name.Trim();
        TaxType = taxType;
        LiabilityAccountId = liabilityAccountId;
        _components.Clear();

        var componentList = components?.ToList() ?? new List<(string, decimal, int, bool)>();

        if (componentList.Count > 1)
        {
            SetComponents(componentList);
            Rate = GetEffectiveRate();
        }
        else
        {
            if (rate < 0m)
            {
                throw new DomainException("Tax rate cannot be negative.");
            }

            Rate = rate;
        }

        Touch();
    }

    public void Archive()
    {
        if (IsArchived)
        {
            throw new DomainException("Tax code is already archived.");
        }

        IsArchived = true;
        Touch();
    }

    public decimal GetEffectiveRate()
    {
        if (_components.Count == 0)
        {
            return Rate;
        }

        if (_components.All(c => !c.AppliesOnPrevious))
        {
            return _components.Sum(c => c.Rate);
        }

        return CalculateTaxAmount(100m) / 100m;
    }

    public decimal CalculateTaxAmount(decimal netAmount)
    {
        if (netAmount <= 0m || TaxType == TaxType.None)
        {
            return 0m;
        }

        if (_components.Count == 0)
        {
            return Math.Round(netAmount * Rate, 2, MidpointRounding.AwayFromZero);
        }

        decimal totalTax = 0m;
        decimal basis = netAmount;

        foreach (var component in _components.OrderBy(c => c.Sequence))
        {
            var componentTax = Math.Round(basis * component.Rate, 2, MidpointRounding.AwayFromZero);
            totalTax += componentTax;

            if (component.AppliesOnPrevious)
            {
                basis += componentTax;
            }
        }

        return totalTax;
    }

    private void SetComponents(IReadOnlyList<(string Name, decimal Rate, int Sequence, bool AppliesOnPrevious)> components)
    {
        if (components.Count < 2)
        {
            throw new DomainException("Compound tax codes require at least two components.");
        }

        foreach (var component in components.OrderBy(c => c.Sequence))
        {
            _components.Add(new TaxCodeComponent(
                Id,
                component.Name,
                component.Rate,
                component.Sequence,
                component.AppliesOnPrevious));
        }
    }
}
