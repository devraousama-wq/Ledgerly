using Ledgerly.Domain.Enums;

namespace Ledgerly.Application.Accounts;

public sealed record UpdateAccountRequest(
    string Code,
    string Name,
    AccountType AccountType,
    string CurrencyCode,
    decimal OpeningBalanceAmount,
    Guid? ParentAccountId);
