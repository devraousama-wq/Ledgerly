using Ledgerly.Domain.Enums;

namespace Ledgerly.Application.Periods;

public sealed record FiscalPeriodDto(
    Guid Id,
    Guid OrganizationId,
    int Year,
    int Month,
    FiscalPeriodStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record PeriodCloseAuditDto(
    Guid Id,
    Guid OrganizationId,
    Guid FiscalPeriodId,
    int Year,
    int Month,
    PeriodCloseAction Action,
    DateTimeOffset CreatedAt);
