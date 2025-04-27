using Ledgerly.Domain.Enums;

namespace Ledgerly.Application.Accounts;

public sealed record CreateAccountRequest(
    Guid OrganizationId,
    string Code,
    string Name,
    AccountType AccountType,
    string CurrencyCode,
    decimal OpeningBalanceAmount,
    Guid? ParentAccountId);
