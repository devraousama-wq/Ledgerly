using Ledgerly.Application.Abstractions;
using Ledgerly.Domain.Common;
using Ledgerly.Domain.Enums;

namespace Ledgerly.Application.Reconciliation;

public sealed class ReconciliationReportService
{
    private readonly IBankStatementRepository _bankStatementRepository;

    public ReconciliationReportService(IBankStatementRepository bankStatementRepository)
    {
        _bankStatementRepository = bankStatementRepository;
    }

    public async Task<Result<ReconciliationReportDto>> GenerateAsync(
        Guid organizationId,
        ReconciliationReportRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.PeriodEnd < request.PeriodStart)
        {
            return Result<ReconciliationReportDto>.Failure("End date must be on or after start date.");
        }

        var statements = await _bankStatementRepository.ListByOrganizationAsync(organizationId, cancellationToken);

        var relevantLines = statements
            .Where(statement =>
                statement.BankAccountId == request.BankAccountId &&
                statement.PeriodStart <= request.PeriodEnd &&
                statement.PeriodEnd >= request.PeriodStart)
            .SelectMany(statement => statement.Lines
                .Where(line => line.TransactionDate >= request.PeriodStart && line.TransactionDate <= request.PeriodEnd)
                .Select(line => new
                {
                    Statement = statement,
                    Line = line
                }))
            .OrderBy(item => item.Line.TransactionDate)
            .ThenBy(item => item.Line.CreatedAt)
            .ToList();

        if (relevantLines.Count == 0)
        {
            return Result<ReconciliationReportDto>.Success(new ReconciliationReportDto(
                request.BankAccountId,
                request.PeriodStart,
                request.PeriodEnd,
                0m,
                0m,
                0m,
                0m,
                0m,
                0,
                0,
                Array.Empty<ReconciliationReportLineDto>()));
        }

        var opening = relevantLines
            .Select(item => item.Statement)
            .OrderBy(statement => statement.PeriodStart)
            .First()
            .OpeningBalance.Amount;

        var closing = relevantLines
            .Select(item => item.Statement)
            .OrderByDescending(statement => statement.PeriodEnd)
            .First()
            .ClosingBalance.Amount;

        var reportLines = relevantLines
            .Select(item => new ReconciliationReportLineDto(
                item.Line.Id,
                item.Line.TransactionDate,
                item.Line.Amount.Amount,
                item.Line.Description,
                item.Line.Status,
                item.Line.MatchedJournalLineId,
                item.Line.CreatedJournalEntryId))
            .ToList();

        var matchedAmount = reportLines
            .Where(line => line.Status != BankStatementLineStatus.Pending)
            .Sum(line => line.Amount);

        var unmatchedAmount = reportLines
            .Where(line => line.Status == BankStatementLineStatus.Pending)
            .Sum(line => line.Amount);

        return Result<ReconciliationReportDto>.Success(new ReconciliationReportDto(
            request.BankAccountId,
            request.PeriodStart,
            request.PeriodEnd,
            opening,
            closing,
            reportLines.Sum(line => line.Amount),
            matchedAmount,
            unmatchedAmount,
            reportLines.Count(line => line.Status != BankStatementLineStatus.Pending),
            reportLines.Count(line => line.Status == BankStatementLineStatus.Pending),
            reportLines));
    }
}
