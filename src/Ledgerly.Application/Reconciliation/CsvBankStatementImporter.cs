using System.Globalization;
using System.Text.RegularExpressions;

namespace Ledgerly.Application.Reconciliation;

public static class CsvBankStatementImporter
{
    public static IReadOnlyList<ImportedBankLine> Parse(string csvContent, CsvColumnMapping mapping)
    {
        if (string.IsNullOrWhiteSpace(csvContent))
        {
            throw new InvalidOperationException("CSV content is required.");
        }

        var rows = csvContent
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        if (rows.Count == 0)
        {
            throw new InvalidOperationException("CSV content is required.");
        }

        var startIndex = mapping.HasHeaderRow ? 1 : 0;
        var lines = new List<ImportedBankLine>();

        for (var i = startIndex; i < rows.Count; i++)
        {
            var columns = SplitCsvRow(rows[i]);

            if (columns.Count <= Math.Max(mapping.DateColumn, Math.Max(mapping.AmountColumn, mapping.DescriptionColumn)))
            {
                throw new InvalidOperationException($"CSV row {i + 1} does not contain required columns.");
            }

            if (!DateOnly.TryParseExact(columns[mapping.DateColumn], mapping.DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) &&
                !DateOnly.TryParse(columns[mapping.DateColumn], CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                throw new InvalidOperationException($"CSV row {i + 1} has an invalid date.");
            }

            if (!decimal.TryParse(columns[mapping.AmountColumn], NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var amount))
            {
                throw new InvalidOperationException($"CSV row {i + 1} has an invalid amount.");
            }

            var description = columns[mapping.DescriptionColumn];
            string? reference = null;

            if (mapping.ReferenceColumn is int referenceColumn)
            {
                if (referenceColumn >= columns.Count)
                {
                    throw new InvalidOperationException($"CSV row {i + 1} does not contain a reference column.");
                }

                reference = columns[referenceColumn];
            }

            lines.Add(new ImportedBankLine(date, amount, description, reference));
        }

        return lines;
    }

    private static List<string> SplitCsvRow(string row)
    {
        return Regex.Matches(row, "(?:^|,)(\"(?:[^\"]|\"\")*\"|[^,]*)")
            .Select(match => match.Value.TrimStart(',').Trim('"').Replace("\"\"", "\""))
            .ToList();
    }
}
