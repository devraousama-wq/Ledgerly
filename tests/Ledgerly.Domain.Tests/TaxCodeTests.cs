using Ledgerly.Domain.Entities;
using Ledgerly.Domain.Enums;

namespace Ledgerly.Domain.Tests;

public class TaxCodeTests
{
    private static readonly Guid Org = Guid.NewGuid();
    private static readonly Guid Liability = Guid.NewGuid();

    [Fact]
    public void CalculateTaxAmount_uses_rate_for_simple_code()
    {
        var taxCode = new TaxCode(Org, "GST", "Goods and Services Tax", TaxType.Gst, 0.1m, Liability);

        Assert.Equal(10m, taxCode.CalculateTaxAmount(100m));
        Assert.False(taxCode.IsCompound);
    }

    [Fact]
    public void CalculateTaxAmount_sums_additive_components()
    {
        var taxCode = new TaxCode(
            Org,
            "COMPOUND",
            "Federal + Provincial",
            TaxType.SalesTax,
            0m,
            Liability,
            new[]
            {
                ("Federal", 0.05m, 1, false),
                ("Provincial", 0.10m, 2, false)
            });

        Assert.True(taxCode.IsCompound);
        Assert.Equal(15m, taxCode.CalculateTaxAmount(100m));
        Assert.Equal(0.15m, taxCode.GetEffectiveRate());
    }

    [Fact]
    public void CalculateTaxAmount_applies_cascading_components()
    {
        var taxCode = new TaxCode(
            Org,
            "CASCADE",
            "Cascading Tax",
            TaxType.Vat,
            0m,
            Liability,
            new[]
            {
                ("First", 0.05m, 1, true),
                ("Second", 0.10m, 2, false)
            });

        Assert.Equal(15.50m, taxCode.CalculateTaxAmount(100m));
    }

    [Fact]
    public void Archive_marks_tax_code_archived()
    {
        var taxCode = new TaxCode(Org, "NONE", "No Tax", TaxType.None, 0m, Liability);

        taxCode.Archive();

        Assert.True(taxCode.IsArchived);
    }

    [Fact]
    public void Update_replaces_components()
    {
        var taxCode = new TaxCode(Org, "T1", "Tax 1", TaxType.Vat, 0.08m, Liability);

        taxCode.Update(
            "T1",
            "Updated Tax",
            TaxType.Vat,
            0m,
            Liability,
            new[]
            {
                ("A", 0.04m, 1, false),
                ("B", 0.06m, 2, false)
            });

        Assert.Equal(10m, taxCode.CalculateTaxAmount(100m));
        Assert.True(taxCode.IsCompound);
    }
}
