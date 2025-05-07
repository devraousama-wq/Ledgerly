using Ledgerly.Application.Abstractions;
using Ledgerly.Application.Journals;
using Ledgerly.Domain.Common;
using Ledgerly.Domain.Entities;
using Ledgerly.Domain.Enums;
using Ledgerly.Domain.Exceptions;
using Ledgerly.Domain.ValueObjects;

namespace Ledgerly.Application.Reconciliation;

public sealed class BankReconciliationService
{
    private readonly IBankStatementRepository _bankStatementRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IJournalRepository _journalRepository;
    private readonly JournalService _journalService;

    public BankReconciliationService(
        IBankStatementRepository bankStatementRepository,
        IAccountRepository accountRepository,
        IJournalRepository journalRepository,
        JournalService journalService)
    {
        _bankStatementRepository = bankStatementRepository;
        _accountRepository = accountRepository;
        _journalRepository = journalRepository;
        _journalService = journalService;
    }

    public async Task<Result<BankStatementDto>> CreateAsync(
        CreateBankStatementRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var accountValidation = await ValidateBankAccountAsync(
                request.OrganizationId,
                request.BankAccountId,
                request.Currency,
                cancellationToken);

            if (accountValidation.IsFailure)
            {
                return Result<BankStatementDto>.Failure(accountValidation.Error!);
            }

            var currency = new CurrencyCode(request.Currency);
            var statement = new BankStatement(
                request.OrganizationId,
                request.BankAccountId,
                request.PeriodStart,
                request.PeriodEnd,
                new Money(request.OpeningBalance, currency),
                new Money(request.ClosingBalance, currency));

            await _bankStatementRepository.AddAsync(statement, cancellationToken);

            return Result<BankStatementDto>.Success(MapToDto(statement));
        }
        catch (DomainException exception)
        {
            return Result<BankStatementDto>.Failure(exception.Message);
        }
    }

    public async Task<Result<BankStatementDto>> GetByIdAsync(
        Guid organizationId,
        Guid statementId,
        CancellationToken cancellationToken = default)
    {
        var statement = await _bankStatementRepository.GetByIdAsync(organizationId, statementId, cancellationToken);

        if (statement is null)
        {
            return Result<BankStatementDto>.Failure("Bank statement not found.");
        }

        return Result<BankStatementDto>.Success(MapToDto(statement));
    }

    public async Task<Result<IReadOnlyList<BankStatementDto>>> ListByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var statements = await _bankStatementRepository.ListByOrganizationAsync(organizationId, cancellationToken);
        return Result<IReadOnlyList<BankStatementDto>>.Success(
            statements.Select(MapToDto).ToList());
    }

    public async Task<Result<BankStatementDto>> AddLineAsync(
        Guid organizationId,
        Guid statementId,
        AddBankStatementLineRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var statement = await GetOpenStatementAsync(organizationId, statementId, cancellationToken);

            if (statement is null)
            {
                return Result<BankStatementDto>.Failure("Bank statement not found.");
            }

            statement.AddManualLine(
                request.TransactionDate,
                new Money(request.Amount, statement.OpeningBalance.Currency),
                request.Description,
                request.Reference);

            await _bankStatementRepository.UpdateAsync(statement, cancellationToken);

            return Result<BankStatementDto>.Success(MapToDto(statement));
        }
        catch (DomainException exception)
        {
            return Result<BankStatementDto>.Failure(exception.Message);
        }
    }

    public async Task<Result<BankStatementDto>> ImportCsvAsync(
        Guid organizationId,
        Guid statementId,
        ImportCsvBankStatementRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var statement = await GetOpenStatementAsync(organizationId, statementId, cancellationToken);

            if (statement is null)
            {
                return Result<BankStatementDto>.Failure("Bank statement not found.");
            }

            var imported = CsvBankStatementImporter.Parse(request.CsvContent, request.Mapping);

            statement.ImportLines(imported.Select(line => (
                line.TransactionDate,
                new Money(line.Amount, statement.OpeningBalance.Currency),
                line.Description,
                line.Reference)));

            await _bankStatementRepository.UpdateAsync(statement, cancellationToken);

            return Result<BankStatementDto>.Success(MapToDto(statement));
        }
        catch (DomainException exception)
        {
            return Result<BankStatementDto>.Failure(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Result<BankStatementDto>.Failure(exception.Message);
        }
    }

    public async Task<Result<BankStatementDto>> ImportOfxAsync(
        Guid organizationId,
        Guid statementId,
        ImportOfxBankStatementRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var statement = await GetOpenStatementAsync(organizationId, statementId, cancellationToken);

            if (statement is null)
            {
                return Result<BankStatementDto>.Failure("Bank statement not found.");
            }

            var imported = OfxBankStatementImporter.Parse(request.OfxContent);

            statement.ImportLines(imported.Select(line => (
                line.TransactionDate,
                new Money(line.Amount, statement.OpeningBalance.Currency),
                line.Description,
                line.Reference)));

            await _bankStatementRepository.UpdateAsync(statement, cancellationToken);

            return Result<BankStatementDto>.Success(MapToDto(statement));
        }
        catch (DomainException exception)
        {
            return Result<BankStatementDto>.Failure(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Result<BankStatementDto>.Failure(exception.Message);
        }
    }

    public async Task<Result<BankStatementDto>> MatchLineAsync(
        Guid organizationId,
        Guid statementId,
        Guid lineId,
        MatchBankStatementLineRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var statement = await GetOpenStatementAsync(organizationId, statementId, cancellationToken);

            if (statement is null)
            {
                return Result<BankStatementDto>.Failure("Bank statement not found.");
            }

            var journalLine = await FindJournalLineAsync(
                organizationId,
                statement.BankAccountId,
                request.JournalLineId,
                cancellationToken);

            if (journalLine is null)
            {
                return Result<BankStatementDto>.Failure("Journal line not found.");
            }

            var matchedIds = await _bankStatementRepository.ListMatchedJournalLineIdsAsync(
                organizationId,
                statement.BankAccountId,
                cancellationToken);

            if (matchedIds.Contains(request.JournalLineId))
            {
                return Result<BankStatementDto>.Failure("Journal line is already reconciled.");
            }

            statement.MatchLine(lineId, request.JournalLineId, journalLine.Value.Line.Debit, journalLine.Value.Line.Credit);
            await _bankStatementRepository.UpdateAsync(statement, cancellationToken);

            return Result<BankStatementDto>.Success(MapToDto(statement));
        }
        catch (DomainException exception)
        {
            return Result<BankStatementDto>.Failure(exception.Message);
        }
    }

    public async Task<Result<BankStatementDto>> AutoMatchAsync(
        Guid organizationId,
        Guid statementId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var statement = await GetOpenStatementAsync(organizationId, statementId, cancellationToken);

            if (statement is null)
            {
                return Result<BankStatementDto>.Failure("Bank statement not found.");
            }

            var matchedIds = (await _bankStatementRepository.ListMatchedJournalLineIdsAsync(
                organizationId,
                statement.BankAccountId,
                cancellationToken)).ToHashSet();

            var candidates = await _journalRepository.ListPostedLinesByAccountAsync(
                organizationId,
                statement.BankAccountId,
                statement.PeriodStart,
                statement.PeriodEnd,
                cancellationToken);

            foreach (var line in statement.Lines.Where(l => l.Status == BankStatementLineStatus.Pending))
            {
                var match = candidates.FirstOrDefault(candidate =>
                    !matchedIds.Contains(candidate.Line.Id) &&
                    candidate.Entry.EntryDate == line.TransactionDate &&
                    AmountMatches(line, candidate.Line));

                if (match == default)
                {
                    continue;
                }

                statement.MatchLine(line.Id, match.Line.Id, match.Line.Debit, match.Line.Credit);
                matchedIds.Add(match.Line.Id);
            }

            await _bankStatementRepository.UpdateAsync(statement, cancellationToken);

            return Result<BankStatementDto>.Success(MapToDto(statement));
        }
        catch (DomainException exception)
        {
            return Result<BankStatementDto>.Failure(exception.Message);
        }
    }

    public async Task<Result<BankStatementDto>> CreateEntryFromLineAsync(
        Guid organizationId,
        Guid statementId,
        Guid lineId,
        CreateBankEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var statement = await GetOpenStatementAsync(organizationId, statementId, cancellationToken);

            if (statement is null)
            {
                return Result<BankStatementDto>.Failure("Bank statement not found.");
            }

            var line = statement.Lines.FirstOrDefault(l => l.Id == lineId);

            if (line is null)
            {
                return Result<BankStatementDto>.Failure("Statement line not found.");
            }

            if (line.Status != BankStatementLineStatus.Pending)
            {
                return Result<BankStatementDto>.Failure("Only pending lines can create entries.");
            }

            var offsetValidation = await ValidateOffsetAccountAsync(
                organizationId,
                request.OffsetAccountId,
                statement.OpeningBalance.Currency,
                cancellationToken);

            if (offsetValidation.IsFailure)
            {
                return Result<BankStatementDto>.Failure(offsetValidation.Error!);
            }

            var draft = await _journalService.CreateDraftAsync(
                new CreateJournalRequest(
                    organizationId,
                    line.TransactionDate,
                    line.Reference,
                    string.IsNullOrWhiteSpace(request.Memo) ? line.Description : request.Memo!,
                    statement.OpeningBalance.Currency.Value),
                cancellationToken);

            if (draft.IsFailure)
            {
                return Result<BankStatementDto>.Failure(draft.Error!);
            }

            var entryId = draft.Value!.Id;
            var amount = line.GetSignedLedgerAmount().Amount;

            if (line.Amount.Amount > 0m)
            {
                await _journalService.AddLineAsync(
                    organizationId,
                    entryId,
                    new AddJournalLineRequest(statement.BankAccountId, amount, 0m, line.Description),
                    cancellationToken);

                await _journalService.AddLineAsync(
                    organizationId,
                    entryId,
                    new AddJournalLineRequest(request.OffsetAccountId, 0m, amount, line.Description),
                    cancellationToken);
            }
            else
            {
                await _journalService.AddLineAsync(
                    organizationId,
                    entryId,
                    new AddJournalLineRequest(request.OffsetAccountId, amount, 0m, line.Description),
                    cancellationToken);

                await _journalService.AddLineAsync(
                    organizationId,
                    entryId,
                    new AddJournalLineRequest(statement.BankAccountId, 0m, amount, line.Description),
                    cancellationToken);
            }

            var posted = await _journalService.PostAsync(organizationId, entryId, cancellationToken);

            if (posted.IsFailure)
            {
                return Result<BankStatementDto>.Failure(posted.Error!);
            }

            var bankLine = posted.Value!.Lines.First(l => l.AccountId == statement.BankAccountId);
            statement.MarkLineEntryCreated(
                lineId,
                posted.Value.Id,
                bankLine.Id,
                new Money(bankLine.DebitAmount, statement.OpeningBalance.Currency),
                new Money(bankLine.CreditAmount, statement.OpeningBalance.Currency));
            await _bankStatementRepository.UpdateAsync(statement, cancellationToken);

            return Result<BankStatementDto>.Success(MapToDto(statement));
        }
        catch (DomainException exception)
        {
            return Result<BankStatementDto>.Failure(exception.Message);
        }
    }

    public async Task<Result<BankStatementDto>> ReconcileAsync(
        Guid organizationId,
        Guid statementId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var statement = await GetOpenStatementAsync(organizationId, statementId, cancellationToken);

            if (statement is null)
            {
                return Result<BankStatementDto>.Failure("Bank statement not found.");
            }

            statement.Reconcile();
            await _bankStatementRepository.UpdateAsync(statement, cancellationToken);

            return Result<BankStatementDto>.Success(MapToDto(statement));
        }
        catch (DomainException exception)
        {
            return Result<BankStatementDto>.Failure(exception.Message);
        }
    }

    private async Task<BankStatement?> GetOpenStatementAsync(
        Guid organizationId,
        Guid statementId,
        CancellationToken cancellationToken)
    {
        var statement = await _bankStatementRepository.GetByIdAsync(organizationId, statementId, cancellationToken);

        if (statement is null)
        {
            return null;
        }

        if (statement.Status != BankStatementStatus.Open)
        {
            throw new DomainException("Reconciled statements cannot be modified.");
        }

        return statement;
    }

    private async Task<(JournalEntry Entry, JournalLine Line)?> FindJournalLineAsync(
        Guid organizationId,
        Guid bankAccountId,
        Guid journalLineId,
        CancellationToken cancellationToken)
    {
        var candidates = await _journalRepository.ListPostedLinesByAccountAsync(
            organizationId,
            bankAccountId,
            DateOnly.MinValue,
            DateOnly.MaxValue,
            cancellationToken);

        return candidates.FirstOrDefault(candidate => candidate.Line.Id == journalLineId);
    }

    private static bool AmountMatches(BankStatementLine line, JournalLine journalLine)
    {
        if (line.Amount.Amount > 0m)
        {
            return journalLine.Debit.Amount == line.Amount.Amount && journalLine.Credit.Amount == 0m;
        }

        return journalLine.Credit.Amount == -line.Amount.Amount && journalLine.Debit.Amount == 0m;
    }

    private async Task<Result> ValidateBankAccountAsync(
        Guid organizationId,
        Guid bankAccountId,
        string currency,
        CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByIdAsync(organizationId, bankAccountId, cancellationToken);

        if (account is null)
        {
            return Result.Failure("Bank account not found.");
        }

        if (account.IsArchived)
        {
            return Result.Failure("Cannot reconcile an archived account.");
        }

        if (account.AccountType != AccountType.Asset)
        {
            return Result.Failure("Bank account must be an asset account.");
        }

        if (account.CurrencyCode.Value != currency)
        {
            return Result.Failure("Statement currency must match bank account currency.");
        }

        return Result.Success();
    }

    private async Task<Result> ValidateOffsetAccountAsync(
        Guid organizationId,
        Guid offsetAccountId,
        CurrencyCode currency,
        CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByIdAsync(organizationId, offsetAccountId, cancellationToken);

        if (account is null)
        {
            return Result.Failure("Offset account not found.");
        }

        if (account.IsArchived)
        {
            return Result.Failure("Cannot post to an archived account.");
        }

        if (account.CurrencyCode != currency)
        {
            return Result.Failure("Offset account currency must match statement currency.");
        }

        return Result.Success();
    }

    private static BankStatementDto MapToDto(BankStatement statement) =>
        new(
            statement.Id,
            statement.OrganizationId,
            statement.BankAccountId,
            statement.PeriodStart,
            statement.PeriodEnd,
            statement.OpeningBalance.Amount,
            statement.ClosingBalance.Amount,
            statement.OpeningBalance.Currency.Value,
            statement.Status,
            statement.Lines
                .Select(line => new BankStatementLineDto(
                    line.Id,
                    line.TransactionDate,
                    line.Amount.Amount,
                    line.Description,
                    line.Reference,
                    line.Status,
                    line.MatchedJournalLineId,
                    line.CreatedJournalEntryId))
                .ToList(),
            statement.CreatedAt,
            statement.UpdatedAt);
}
