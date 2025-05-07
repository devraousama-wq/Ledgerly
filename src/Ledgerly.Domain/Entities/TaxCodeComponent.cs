using Ledgerly.Domain.Common;
using Ledgerly.Domain.Exceptions;

namespace Ledgerly.Domain.Entities;

public sealed class TaxCodeComponent : Entity
{
    public Guid TaxCodeId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public decimal Rate { get; private set; }

    public int Sequence { get; private set; }

    public bool AppliesOnPrevious { get; private set; }

    private TaxCodeComponent()
    {
    }

    internal TaxCodeComponent(
        Guid taxCodeId,
        string name,
        decimal rate,
        int sequence,
        bool appliesOnPrevious)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Tax component name is required.");
        }

        if (rate < 0m)
        {
            throw new DomainException("Tax component rate cannot be negative.");
        }

        TaxCodeId = taxCodeId;
        Name = name.Trim();
        Rate = rate;
        Sequence = sequence;
        AppliesOnPrevious = appliesOnPrevious;
    }
}
