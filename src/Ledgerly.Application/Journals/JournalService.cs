using Ledgerly.Application.Abstractions;
using Ledgerly.Domain.Common;
using Ledgerly.Domain.Entities;
using Ledgerly.Domain.Enums;
using Ledgerly.Domain.Exceptions;
using Ledgerly.Domain.ValueObjects;

namespace Ledgerly.Application.Journals;

public sealed class JournalService
{
    private readonly IJournalRepository _journalRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IFiscalPeriodRepository _fiscalPeriodRepository;

    public JournalService(
        IJournalRepository journalRepository,
        IAccountRepository accountRepository,
        IFiscalPeriodRepository fiscalPeriodRepository)
    {
        _journalRepository = journalRepository;
        _accountRepository = accountRepository;
        _fiscalPeriodRepository = fiscalPeriodRepository;
    }

    public async Task<Result<JournalEntryDto>> CreateDraftAsync(
        CreateJournalRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var baseCurrency = new CurrencyCode(request.BaseCurrency);

            var entry = new JournalEntry(
                request.OrganizationId,
                request.EntryDate,
                request.Reference,
                request.Description,
                baseCurrency);

            await _journalRepository.AddAsync(entry, cancellationToken);

            return Result<JournalEntryDto>.Success(MapToDto(entry));
        }
        catch (DomainException exception)
        {
            return Result<JournalEntryDto>.Failure(exception.Message);
        }
    }

    public async Task<Result<JournalEntryDto>> GetByIdAsync(
        Guid organizationId,
        Guid journalEntryId,
        CancellationToken cancellationToken = default)
    {
        var entry = await _journalRepository.GetByIdAsync(organizationId, journalEntryId, cancellationToken);

        if (entry is null)
        {
            return Result<JournalEntryDto>.Failure("Journal entry not found.");
        }

        return Result<JournalEntryDto>.Success(MapToDto(entry));
    }

    public async Task<Result<JournalEntryDto>> AddLineAsync(
        Guid organizationId,
        Guid journalEntryId,
        AddJournalLineRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entry = await _journalRepository.GetByIdAsync(organizationId, journalEntryId, cancellationToken);

            if (entry is null)
            {
                return Result<JournalEntryDto>.Failure("Journal entry not found.");
            }

            var accountValidation = await ValidateAccountAsync(
                organizationId,
                request.AccountId,
                entry.BaseCurrency,
                cancellationToken);

            if (accountValidation.IsFailure)
            {
                return Result<JournalEntryDto>.Failure(accountValidation.Error!);
            }

            var debit = new Money(request.DebitAmount, entry.BaseCurrency);
            var credit = new Money(request.CreditAmount, entry.BaseCurrency);

            entry.AddLine(request.AccountId, debit, credit, request.Memo);

            await _journalRepository.UpdateAsync(entry, cancellationToken);

            return Result<JournalEntryDto>.Success(MapToDto(entry));
        }
        catch (DomainException exception)
        {
            return Result<JournalEntryDto>.Failure(exception.Message);
        }
    }

    public async Task<Result<JournalEntryDto>> PostAsync(
        Guid organizationId,
        Guid journalEntryId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entry = await _journalRepository.GetByIdAsync(organizationId, journalEntryId, cancellationToken);

            if (entry is null)
            {
                return Result<JournalEntryDto>.Failure("Journal entry not found.");
            }

            var periodValidation = await EnsurePeriodOpenAsync(
                organizationId,
                entry.EntryDate,
                cancellationToken);

            if (periodValidation.IsFailure)
            {
                return Result<JournalEntryDto>.Failure(periodValidation.Error!);
            }

            entry.Post();

            await _journalRepository.UpdateAsync(entry, cancellationToken);

            return Result<JournalEntryDto>.Success(MapToDto(entry));
        }
        catch (UnbalancedJournalException exception)
        {
            return Result<JournalEntryDto>.Failure(exception.Message);
        }
        catch (DomainException exception)
        {
            return Result<JournalEntryDto>.Failure(exception.Message);
        }
    }

    public async Task<Result<JournalEntryDto>> ReverseAsync(
        Guid organizationId,
        Guid journalEntryId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entry = await _journalRepository.GetByIdAsync(organizationId, journalEntryId, cancellationToken);

            if (entry is null)
            {
                return Result<JournalEntryDto>.Failure("Journal entry not found.");
            }

            var periodValidation = await EnsurePeriodOpenAsync(
                organizationId,
                entry.EntryDate,
                cancellationToken);

            if (periodValidation.IsFailure)
            {
                return Result<JournalEntryDto>.Failure(periodValidation.Error!);
            }

            var reversal = entry.CreateReversal();

            await _journalRepository.UpdateAsync(entry, cancellationToken);
            await _journalRepository.AddAsync(reversal, cancellationToken);

            return Result<JournalEntryDto>.Success(MapToDto(reversal));
        }
        catch (DomainException exception)
        {
            return Result<JournalEntryDto>.Failure(exception.Message);
        }
    }

    private async Task<Result> ValidateAccountAsync(
        Guid organizationId,
        Guid accountId,
        CurrencyCode baseCurrency,
        CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByIdAsync(organizationId, accountId, cancellationToken);

        if (account is null)
        {
            return Result.Failure("Account not found.");
        }

        if (account.IsArchived)
        {
            return Result.Failure("Cannot post to an archived account.");
        }

        if (account.CurrencyCode != baseCurrency)
        {
            return Result.Failure("Account currency must match journal base currency.");
        }

        return Result.Success();
    }

    private async Task<Result> EnsurePeriodOpenAsync(
        Guid organizationId,
        DateOnly entryDate,
        CancellationToken cancellationToken)
    {
        var period = await _fiscalPeriodRepository.GetByYearMonthAsync(
            organizationId,
            entryDate.Year,
            entryDate.Month,
            cancellationToken);

        if (period?.Status == FiscalPeriodStatus.Closed)
        {
            return Result.Failure($"Fiscal period {entryDate:yyyy-MM} is closed.");
        }

        return Result.Success();
    }

    private static JournalEntryDto MapToDto(JournalEntry entry) =>
        new(
            entry.Id,
            entry.OrganizationId,
            entry.EntryDate,
            entry.Reference,
            entry.Description,
            entry.Status,
            entry.BaseCurrency.Value,
            entry.ReversalOfEntryId,
            entry.Lines
                .Select(line => new JournalLineDto(
                    line.Id,
                    line.AccountId,
                    line.Debit.Amount,
                    line.Credit.Amount,
                    line.Memo))
                .ToList(),
            entry.CreatedAt,
            entry.UpdatedAt);
}
