using Ledgerly.Application.Abstractions;
using Ledgerly.Domain.Common;
using Ledgerly.Domain.Entities;
using Ledgerly.Domain.Enums;
using Ledgerly.Domain.Exceptions;
using Ledgerly.Domain.ValueObjects;

namespace Ledgerly.Application.Accounts;

public sealed class AccountService
{
    private readonly IAccountRepository _accountRepository;

    public AccountService(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<Result<AccountDto>> CreateAsync(
        CreateAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currency = new CurrencyCode(request.CurrencyCode);
            var openingBalance = new Money(request.OpeningBalanceAmount, currency);

            var existing = await _accountRepository.GetByCodeAsync(
                request.OrganizationId,
                request.Code,
                cancellationToken);

            if (existing is not null)
            {
                return Result<AccountDto>.Failure($"Account code '{request.Code}' already exists.");
            }

            if (request.ParentAccountId.HasValue)
            {
                var parentValidation = await ValidateParentAsync(
                    request.OrganizationId,
                    request.ParentAccountId.Value,
                    request.AccountType,
                    cancellationToken);

                if (parentValidation.IsFailure)
                {
                    return Result<AccountDto>.Failure(parentValidation.Error!);
                }
            }

            var account = new Account(
                request.OrganizationId,
                request.Code,
                request.Name,
                request.AccountType,
                currency,
                openingBalance,
                request.ParentAccountId);

            await _accountRepository.AddAsync(account, cancellationToken);

            return Result<AccountDto>.Success(MapToDto(account));
        }
        catch (DomainException exception)
        {
            return Result<AccountDto>.Failure(exception.Message);
        }
    }

    public async Task<Result<AccountDto>> GetByIdAsync(
        Guid organizationId,
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        var account = await _accountRepository.GetByIdAsync(organizationId, accountId, cancellationToken);

        if (account is null)
        {
            return Result<AccountDto>.Failure("Account not found.");
        }

        return Result<AccountDto>.Success(MapToDto(account));
    }

    public async Task<Result<IReadOnlyList<AccountDto>>> ListByOrganizationAsync(
        Guid organizationId,
        bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var accounts = await _accountRepository.ListByOrganizationAsync(
            organizationId,
            includeArchived,
            cancellationToken);

        var dtos = accounts.Select(MapToDto).ToList();
        return Result<IReadOnlyList<AccountDto>>.Success(dtos);
    }

    public async Task<Result<AccountDto>> UpdateAsync(
        Guid organizationId,
        Guid accountId,
        UpdateAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var account = await _accountRepository.GetByIdAsync(organizationId, accountId, cancellationToken);

            if (account is null)
            {
                return Result<AccountDto>.Failure("Account not found.");
            }

            var duplicate = await _accountRepository.GetByCodeAsync(organizationId, request.Code, cancellationToken);

            if (duplicate is not null && duplicate.Id != accountId)
            {
                return Result<AccountDto>.Failure($"Account code '{request.Code}' already exists.");
            }

            if (request.ParentAccountId.HasValue)
            {
                if (request.ParentAccountId.Value == accountId)
                {
                    return Result<AccountDto>.Failure("An account cannot be its own parent.");
                }

                var parentValidation = await ValidateParentAsync(
                    organizationId,
                    request.ParentAccountId.Value,
                    request.AccountType,
                    cancellationToken);

                if (parentValidation.IsFailure)
                {
                    return Result<AccountDto>.Failure(parentValidation.Error!);
                }

                var allAccounts = await _accountRepository.ListByOrganizationAsync(
                    organizationId,
                    includeArchived: true,
                    cancellationToken);

                if (WouldCreateCycle(allAccounts, accountId, request.ParentAccountId))
                {
                    return Result<AccountDto>.Failure("Parent assignment would create a circular hierarchy.");
                }
            }

            var currency = new CurrencyCode(request.CurrencyCode);
            var openingBalance = new Money(request.OpeningBalanceAmount, currency);

            account.Update(
                request.Code,
                request.Name,
                request.AccountType,
                currency,
                openingBalance,
                request.ParentAccountId);

            await _accountRepository.UpdateAsync(account, cancellationToken);

            return Result<AccountDto>.Success(MapToDto(account));
        }
        catch (DomainException exception)
        {
            return Result<AccountDto>.Failure(exception.Message);
        }
    }

    public async Task<Result<AccountDto>> ArchiveAsync(
        Guid organizationId,
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var account = await _accountRepository.GetByIdAsync(organizationId, accountId, cancellationToken);

            if (account is null)
            {
                return Result<AccountDto>.Failure("Account not found.");
            }

            var hasActiveChildren = await _accountRepository.HasActiveChildrenAsync(
                organizationId,
                accountId,
                cancellationToken);

            if (hasActiveChildren)
            {
                return Result<AccountDto>.Failure("Cannot archive an account with active child accounts.");
            }

            account.Archive();
            await _accountRepository.UpdateAsync(account, cancellationToken);

            return Result<AccountDto>.Success(MapToDto(account));
        }
        catch (DomainException exception)
        {
            return Result<AccountDto>.Failure(exception.Message);
        }
    }

    private async Task<Result> ValidateParentAsync(
        Guid organizationId,
        Guid parentAccountId,
        AccountType childAccountType,
        CancellationToken cancellationToken)
    {
        var parent = await _accountRepository.GetByIdAsync(organizationId, parentAccountId, cancellationToken);

        if (parent is null)
        {
            return Result.Failure("Parent account not found.");
        }

        if (parent.IsArchived)
        {
            return Result.Failure("Cannot assign an archived account as parent.");
        }

        if (parent.AccountType != childAccountType)
        {
            return Result.Failure("Parent and child accounts must share the same account type.");
        }

        return Result.Success();
    }

    private static bool WouldCreateCycle(
        IReadOnlyList<Account> accounts,
        Guid accountId,
        Guid? newParentId)
    {
        if (!newParentId.HasValue)
        {
            return false;
        }

        var current = newParentId;
        var visited = new HashSet<Guid>();

        while (current.HasValue)
        {
            if (current.Value == accountId)
            {
                return true;
            }

            if (!visited.Add(current.Value))
            {
                return false;
            }

            var parent = accounts.FirstOrDefault(a => a.Id == current.Value);
            current = parent?.ParentAccountId;
        }

        return false;
    }

    private static AccountDto MapToDto(Account account) =>
        new(
            account.Id,
            account.OrganizationId,
            account.Code,
            account.Name,
            account.AccountType,
            account.ParentAccountId,
            account.CurrencyCode.Value,
            account.OpeningBalance.Amount,
            account.IsArchived,
            account.CreatedAt,
            account.UpdatedAt);
}
