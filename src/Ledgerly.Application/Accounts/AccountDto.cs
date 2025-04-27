using Ledgerly.Domain.Enums;

namespace Ledgerly.Application.Accounts;

public sealed record AccountDto(
    Guid Id,
    Guid OrganizationId,
    string Code,
    string Name,
    AccountType AccountType,
    Guid? ParentAccountId,
    string CurrencyCode,
    decimal OpeningBalanceAmount,
    bool IsArchived,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
