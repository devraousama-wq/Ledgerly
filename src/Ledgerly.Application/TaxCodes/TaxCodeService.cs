using Ledgerly.Application.Abstractions;
using Ledgerly.Domain.Common;
using Ledgerly.Domain.Entities;
using Ledgerly.Domain.Enums;
using Ledgerly.Domain.Exceptions;

namespace Ledgerly.Application.TaxCodes;

public sealed class TaxCodeService
{
    private readonly ITaxCodeRepository _taxCodeRepository;
    private readonly IAccountRepository _accountRepository;

    public TaxCodeService(ITaxCodeRepository taxCodeRepository, IAccountRepository accountRepository)
    {
        _taxCodeRepository = taxCodeRepository;
        _accountRepository = accountRepository;
    }

    public async Task<Result<TaxCodeDto>> CreateAsync(
        CreateTaxCodeRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var liabilityValidation = await ValidateLiabilityAccountAsync(
                request.OrganizationId,
                request.LiabilityAccountId,
                cancellationToken);

            if (liabilityValidation.IsFailure)
            {
                return Result<TaxCodeDto>.Failure(liabilityValidation.Error!);
            }

            var existing = await _taxCodeRepository.GetByCodeAsync(
                request.OrganizationId,
                request.Code,
                cancellationToken);

            if (existing is not null)
            {
                return Result<TaxCodeDto>.Failure($"Tax code '{request.Code}' already exists.");
            }

            var taxCode = new TaxCode(
                request.OrganizationId,
                request.Code,
                request.Name,
                request.TaxType,
                request.Rate,
                request.LiabilityAccountId,
                MapComponents(request.Components));

            await _taxCodeRepository.AddAsync(taxCode, cancellationToken);

            return Result<TaxCodeDto>.Success(MapToDto(taxCode));
        }
        catch (DomainException exception)
        {
            return Result<TaxCodeDto>.Failure(exception.Message);
        }
    }

    public async Task<Result<TaxCodeDto>> GetByIdAsync(
        Guid organizationId,
        Guid taxCodeId,
        CancellationToken cancellationToken = default)
    {
        var taxCode = await _taxCodeRepository.GetByIdAsync(organizationId, taxCodeId, cancellationToken);

        if (taxCode is null)
        {
            return Result<TaxCodeDto>.Failure("Tax code not found.");
        }

        return Result<TaxCodeDto>.Success(MapToDto(taxCode));
    }

    public async Task<Result<IReadOnlyList<TaxCodeDto>>> ListByOrganizationAsync(
        Guid organizationId,
        bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var taxCodes = await _taxCodeRepository.ListByOrganizationAsync(
            organizationId,
            includeArchived,
            cancellationToken);

        var dtos = taxCodes.Select(MapToDto).ToList();
        return Result<IReadOnlyList<TaxCodeDto>>.Success(dtos);
    }

    public async Task<Result<TaxCodeDto>> UpdateAsync(
        Guid organizationId,
        Guid taxCodeId,
        UpdateTaxCodeRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var taxCode = await _taxCodeRepository.GetByIdAsync(organizationId, taxCodeId, cancellationToken);

            if (taxCode is null)
            {
                return Result<TaxCodeDto>.Failure("Tax code not found.");
            }

            var duplicate = await _taxCodeRepository.GetByCodeAsync(organizationId, request.Code, cancellationToken);

            if (duplicate is not null && duplicate.Id != taxCodeId)
            {
                return Result<TaxCodeDto>.Failure($"Tax code '{request.Code}' already exists.");
            }

            var liabilityValidation = await ValidateLiabilityAccountAsync(
                organizationId,
                request.LiabilityAccountId,
                cancellationToken);

            if (liabilityValidation.IsFailure)
            {
                return Result<TaxCodeDto>.Failure(liabilityValidation.Error!);
            }

            taxCode.Update(
                request.Code,
                request.Name,
                request.TaxType,
                request.Rate,
                request.LiabilityAccountId,
                MapComponents(request.Components));

            await _taxCodeRepository.UpdateAsync(taxCode, cancellationToken);

            return Result<TaxCodeDto>.Success(MapToDto(taxCode));
        }
        catch (DomainException exception)
        {
            return Result<TaxCodeDto>.Failure(exception.Message);
        }
    }

    public async Task<Result<TaxCodeDto>> ArchiveAsync(
        Guid organizationId,
        Guid taxCodeId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var taxCode = await _taxCodeRepository.GetByIdAsync(organizationId, taxCodeId, cancellationToken);

            if (taxCode is null)
            {
                return Result<TaxCodeDto>.Failure("Tax code not found.");
            }

            taxCode.Archive();
            await _taxCodeRepository.UpdateAsync(taxCode, cancellationToken);

            return Result<TaxCodeDto>.Success(MapToDto(taxCode));
        }
        catch (DomainException exception)
        {
            return Result<TaxCodeDto>.Failure(exception.Message);
        }
    }

    private async Task<Result> ValidateLiabilityAccountAsync(
        Guid organizationId,
        Guid liabilityAccountId,
        CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByIdAsync(
            organizationId,
            liabilityAccountId,
            cancellationToken);

        if (account is null)
        {
            return Result.Failure("Tax liability account not found.");
        }

        if (account.IsArchived)
        {
            return Result.Failure("Tax liability account is archived.");
        }

        if (account.AccountType != AccountType.Liability)
        {
            return Result.Failure("Tax liability account must be a liability account.");
        }

        return Result.Success();
    }

    private static IEnumerable<(string Name, decimal Rate, int Sequence, bool AppliesOnPrevious)>? MapComponents(
        IReadOnlyList<CreateTaxCodeComponentRequest>? components) =>
        components?.Select(c => (c.Name, c.Rate, c.Sequence, c.AppliesOnPrevious));

    internal static TaxCodeDto MapToDto(TaxCode taxCode) =>
        new(
            taxCode.Id,
            taxCode.OrganizationId,
            taxCode.Code,
            taxCode.Name,
            taxCode.TaxType,
            taxCode.Rate,
            taxCode.GetEffectiveRate(),
            taxCode.LiabilityAccountId,
            taxCode.IsCompound,
            taxCode.IsArchived,
            taxCode.Components
                .OrderBy(c => c.Sequence)
                .Select(c => new TaxCodeComponentDto(
                    c.Id,
                    c.Name,
                    c.Rate,
                    c.Sequence,
                    c.AppliesOnPrevious))
                .ToList(),
            taxCode.CreatedAt,
            taxCode.UpdatedAt);
}
