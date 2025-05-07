using Ledgerly.Domain.Enums;

namespace Ledgerly.Application.Reconciliation;

public sealed record BankStatementLineDto(
    Guid Id,
    DateOnly TransactionDate,
    decimal Amount,
    string Description,
    string? Reference,
    BankStatementLineStatus Status,
    Guid? MatchedJournalLineId,
    Guid? CreatedJournalEntryId);

public sealed record BankStatementDto(
    Guid Id,
    Guid OrganizationId,
    Guid BankAccountId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    decimal OpeningBalance,
    decimal ClosingBalance,
    string Currency,
    BankStatementStatus Status,
    IReadOnlyList<BankStatementLineDto> Lines,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateBankStatementRequest(
    Guid OrganizationId,
    Guid BankAccountId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    decimal OpeningBalance,
    decimal ClosingBalance,
    string Currency);

public sealed record AddBankStatementLineRequest(
    DateOnly TransactionDate,
    decimal Amount,
    string Description,
    string? Reference);

public sealed record CsvColumnMapping(
    int DateColumn,
    int AmountColumn,
    int DescriptionColumn,
    int? ReferenceColumn,
    string DateFormat,
    bool HasHeaderRow);

public sealed record ImportCsvBankStatementRequest(
    string CsvContent,
    CsvColumnMapping Mapping);

public sealed record ImportOfxBankStatementRequest(string OfxContent);

public sealed record MatchBankStatementLineRequest(Guid JournalLineId);

public sealed record CreateBankEntryRequest(
    Guid OffsetAccountId,
    string? Memo);

public sealed record ReconciliationReportRequest(
    Guid BankAccountId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd);

public sealed record ReconciliationReportLineDto(
    Guid StatementLineId,
    DateOnly TransactionDate,
    decimal Amount,
    string Description,
    BankStatementLineStatus Status,
    Guid? MatchedJournalLineId,
    Guid? CreatedJournalEntryId);

public sealed record ReconciliationReportDto(
    Guid BankAccountId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    decimal OpeningBalance,
    decimal ClosingBalance,
    decimal StatementMovement,
    decimal MatchedAmount,
    decimal UnmatchedAmount,
    int MatchedLineCount,
    int UnmatchedLineCount,
    IReadOnlyList<ReconciliationReportLineDto> Lines);

public sealed record ImportedBankLine(
    DateOnly TransactionDate,
    decimal Amount,
    string Description,
    string? Reference);
