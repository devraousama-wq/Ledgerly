using Ledgerly.Domain.Exceptions;

namespace Ledgerly.Domain.ValueObjects;

public readonly record struct CurrencyCode
{
    public string Value { get; }

    public CurrencyCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 3)
        {
            throw new DomainException("Currency code must be exactly three characters.");
        }

        foreach (var character in value)
        {
            if (!char.IsLetter(character) || !char.IsUpper(character))
            {
                throw new DomainException("Currency code must be three uppercase letters.");
            }
        }

        Value = value;
    }

    public override string ToString() => Value;
}
