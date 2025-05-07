using System.Globalization;
using System.Text.RegularExpressions;

namespace Ledgerly.Application.Reconciliation;

public static class OfxBankStatementImporter
{
    public static IReadOnlyList<ImportedBankLine> Parse(string ofxContent)
    {
        if (string.IsNullOrWhiteSpace(ofxContent))
        {
            throw new InvalidOperationException("OFX content is required.");
        }

        var transactions = Regex.Matches(
            ofxContent,
            "<STMTTRN>(.*?)</STMTTRN>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        if (transactions.Count == 0)
        {
            throw new InvalidOperationException("OFX content does not contain statement transactions.");
        }

        var lines = new List<ImportedBankLine>();

        foreach (Match transaction in transactions)
        {
            var block = transaction.Groups[1].Value;
            var amount = ParseDecimal(ReadTag(block, "TRNAMT"));
            var date = ParseOfxDate(ReadTag(block, "DTPOSTED"));
            var description = ReadTag(block, "NAME") ?? ReadTag(block, "MEMO") ?? string.Empty;
            var reference = ReadTag(block, "FITID") ?? ReadTag(block, "CHECKNUM");

            lines.Add(new ImportedBankLine(date, amount, description, reference));
        }

        return lines;
    }

    private static string? ReadTag(string content, string tag)
    {
        var match = Regex.Match(content, $"<{tag}>([^<\\r\\n]+)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    private static decimal ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            !decimal.TryParse(value, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var amount))
        {
            throw new InvalidOperationException("OFX transaction amount is invalid.");
        }

        return amount;
    }

    private static DateOnly ParseOfxDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException("OFX transaction date is invalid.");
        }

        var digits = new string(value.Where(char.IsDigit).Take(8).ToArray());

        if (digits.Length != 8 ||
            !DateOnly.TryParseExact(digits, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            throw new InvalidOperationException("OFX transaction date is invalid.");
        }

        return date;
    }
}
