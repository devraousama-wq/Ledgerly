using Ledgerly.Domain.Enums;

namespace Ledgerly.Application.TaxCodes;

public sealed record TaxCodeComponentDto(
    Guid Id,
    string Name,
    decimal Rate,
    int Sequence,
    bool AppliesOnPrevious);

public sealed record CreateTaxCodeComponentRequest(
    string Name,
    decimal Rate,
    int Sequence,
    bool AppliesOnPrevious);

public sealed record CreateTaxCodeRequest(
    Guid OrganizationId,
    string Code,
    string Name,
    TaxType TaxType,
    decimal Rate,
    Guid LiabilityAccountId,
    IReadOnlyList<CreateTaxCodeComponentRequest>? Components);

public sealed record UpdateTaxCodeRequest(
    string Code,
    string Name,
    TaxType TaxType,
    decimal Rate,
    Guid LiabilityAccountId,
    IReadOnlyList<CreateTaxCodeComponentRequest>? Components);

public sealed record TaxCodeDto(
    Guid Id,
    Guid OrganizationId,
    string Code,
    string Name,
    TaxType TaxType,
    decimal Rate,
    decimal EffectiveRate,
    Guid LiabilityAccountId,
    bool IsCompound,
    bool IsArchived,
    IReadOnlyList<TaxCodeComponentDto> Components,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record TaxLiabilityReportRequest(
    DateOnly StartDate,
    DateOnly EndDate);

public sealed record TaxCodeReportLineDto(
    Guid TaxCodeId,
    string Code,
    string Name,
    TaxType TaxType,
    decimal SalesTax,
    decimal PurchaseTax,
    decimal NetLiability);

public sealed record TaxLiabilityReportDto(
    DateOnly StartDate,
    DateOnly EndDate,
    decimal TotalSalesTax,
    decimal TotalPurchaseTax,
    decimal NetLiability,
    IReadOnlyList<TaxCodeReportLineDto> Lines);
