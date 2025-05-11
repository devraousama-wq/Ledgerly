using Ledgerly.Application.Abstractions;
using Ledgerly.Domain.Common;
using Ledgerly.Domain.Entities;
using Ledgerly.Domain.Enums;
using Ledgerly.Domain.Exceptions;

namespace Ledgerly.Application.Periods;

public sealed class PeriodCloseService
{
    private readonly IFiscalPeriodRepository _fiscalPeriodRepository;
    private readonly IPeriodCloseAuditRepository _periodCloseAuditRepository;

    public PeriodCloseService(
        IFiscalPeriodRepository fiscalPeriodRepository,
        IPeriodCloseAuditRepository periodCloseAuditRepository)
    {
        _fiscalPeriodRepository = fiscalPeriodRepository;
        _periodCloseAuditRepository = periodCloseAuditRepository;
    }

    public async Task<Result<IReadOnlyList<FiscalPeriodDto>>> ListAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var periods = await _fiscalPeriodRepository.ListByOrganizationAsync(organizationId, cancellationToken);

        IReadOnlyList<FiscalPeriodDto> results = periods
            .Select(MapToDto)
            .ToList();

        return Result<IReadOnlyList<FiscalPeriodDto>>.Success(results);
    }

    public async Task<Result<FiscalPeriodDto>> GetAsync(
        Guid organizationId,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var period = await _fiscalPeriodRepository.GetByYearMonthAsync(
                organizationId,
                year,
                month,
                cancellationToken);

            if (period is null)
            {
                return Result<FiscalPeriodDto>.Failure("Fiscal period not found.");
            }

            return Result<FiscalPeriodDto>.Success(MapToDto(period));
        }
        catch (DomainException exception)
        {
            return Result<FiscalPeriodDto>.Failure(exception.Message);
        }
    }

    public async Task<Result<FiscalPeriodDto>> CloseAsync(
        Guid organizationId,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var period = await _fiscalPeriodRepository.GetByYearMonthAsync(
                organizationId,
                year,
                month,
                cancellationToken);

            if (period is null)
            {
                period = new FiscalPeriod(organizationId, year, month);
                period.Close();
                await _fiscalPeriodRepository.AddAsync(period, cancellationToken);
            }
            else
            {
                period.Close();
                await _fiscalPeriodRepository.UpdateAsync(period, cancellationToken);
            }

            await RecordAuditAsync(organizationId, period, PeriodCloseAction.Close, cancellationToken);

            return Result<FiscalPeriodDto>.Success(MapToDto(period));
        }
        catch (DomainException exception)
        {
            return Result<FiscalPeriodDto>.Failure(exception.Message);
        }
    }

    public async Task<Result<FiscalPeriodDto>> ReopenAsync(
        Guid organizationId,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var period = await _fiscalPeriodRepository.GetByYearMonthAsync(
                organizationId,
                year,
                month,
                cancellationToken);

            if (period is null)
            {
                return Result<FiscalPeriodDto>.Failure("Fiscal period not found.");
            }

            period.Reopen();
            await _fiscalPeriodRepository.UpdateAsync(period, cancellationToken);
            await RecordAuditAsync(organizationId, period, PeriodCloseAction.Reopen, cancellationToken);

            return Result<FiscalPeriodDto>.Success(MapToDto(period));
        }
        catch (DomainException exception)
        {
            return Result<FiscalPeriodDto>.Failure(exception.Message);
        }
    }

    public async Task<Result<IReadOnlyList<PeriodCloseAuditDto>>> ListAuditAsync(
        Guid organizationId,
        int? year = null,
        int? month = null,
        CancellationToken cancellationToken = default)
    {
        var audits = await _periodCloseAuditRepository.ListByOrganizationAsync(
            organizationId,
            year,
            month,
            cancellationToken);

        IReadOnlyList<PeriodCloseAuditDto> results = audits
            .Select(MapAuditToDto)
            .ToList();

        return Result<IReadOnlyList<PeriodCloseAuditDto>>.Success(results);
    }

    private async Task RecordAuditAsync(
        Guid organizationId,
        FiscalPeriod period,
        PeriodCloseAction action,
        CancellationToken cancellationToken)
    {
        var audit = new PeriodCloseAudit(
            organizationId,
            period.Id,
            period.Year,
            period.Month,
            action);

        await _periodCloseAuditRepository.AddAsync(audit, cancellationToken);
    }

    private static FiscalPeriodDto MapToDto(FiscalPeriod period) =>
        new(
            period.Id,
            period.OrganizationId,
            period.Year,
            period.Month,
            period.Status,
            period.CreatedAt,
            period.UpdatedAt);

    private static PeriodCloseAuditDto MapAuditToDto(PeriodCloseAudit audit) =>
        new(
            audit.Id,
            audit.OrganizationId,
            audit.FiscalPeriodId,
            audit.Year,
            audit.Month,
            audit.Action,
            audit.CreatedAt);
}
