namespace Ledgerly.Domain.ValueObjects;

public sealed class Address
{
    public string Line1 { get; private set; } = string.Empty;

    public string? Line2 { get; private set; }

    public string City { get; private set; } = string.Empty;

    public string State { get; private set; } = string.Empty;

    public string PostalCode { get; private set; } = string.Empty;

    public string Country { get; private set; } = string.Empty;

    private Address()
    {
    }

    public Address(
        string line1,
        string? line2,
        string city,
        string state,
        string postalCode,
        string country)
    {
        Line1 = (line1 ?? string.Empty).Trim();
        Line2 = string.IsNullOrWhiteSpace(line2) ? null : line2.Trim();
        City = (city ?? string.Empty).Trim();
        State = (state ?? string.Empty).Trim();
        PostalCode = (postalCode ?? string.Empty).Trim();
        Country = (country ?? string.Empty).Trim();
    }

    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(Line1) &&
        string.IsNullOrWhiteSpace(Line2) &&
        string.IsNullOrWhiteSpace(City) &&
        string.IsNullOrWhiteSpace(State) &&
        string.IsNullOrWhiteSpace(PostalCode) &&
        string.IsNullOrWhiteSpace(Country);
}
