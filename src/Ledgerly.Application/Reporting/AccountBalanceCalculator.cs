using Ledgerly.Domain.Enums;

namespace Ledgerly.Application.Reporting;

internal static class AccountBalanceCalculator
{
    public static bool IsDebitNormal(AccountType accountType) =>
        accountType is AccountType.Asset or AccountType.Expense;

    public static decimal GetSignedBalance(
        AccountType accountType,
        decimal openingBalance,
        decimal totalDebits,
        decimal totalCredits)
    {
        var net = totalDebits - totalCredits;
        return IsDebitNormal(accountType) ? openingBalance + net : openingBalance - net;
    }

    public static (decimal DebitBalance, decimal CreditBalance) ToTrialBalanceColumns(
        AccountType accountType,
        decimal signedBalance)
    {
        if (signedBalance == 0m)
        {
            return (0m, 0m);
        }

        if (signedBalance > 0m)
        {
            return IsDebitNormal(accountType)
                ? (signedBalance, 0m)
                : (0m, signedBalance);
        }

        var absolute = Math.Abs(signedBalance);
        return IsDebitNormal(accountType)
            ? (0m, absolute)
            : (absolute, 0m);
    }

    public static decimal GetPeriodAmount(
        AccountType accountType,
        decimal totalDebits,
        decimal totalCredits)
    {
        var net = totalDebits - totalCredits;
        return IsDebitNormal(accountType) ? net : -net;
    }
}
