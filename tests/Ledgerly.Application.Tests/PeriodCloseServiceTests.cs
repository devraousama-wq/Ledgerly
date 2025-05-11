using Ledgerly.Application.Periods;
using Ledgerly.Domain.Enums;

namespace Ledgerly.Application.Tests;

public class PeriodCloseServiceTests
{
    private static readonly Guid OrganizationId = Guid.NewGuid();

    [Fact]
    public async Task CloseAsync_creates_and_closes_period()
    {
        var (service, periods, _) = CreateServices();

        var result = await service.CloseAsync(OrganizationId, 2026, 1);

        Assert.True(result.IsSuccess);
        Assert.Equal(FiscalPeriodStatus.Closed, result.Value!.Status);

        var listed = await periods.ListByOrganizationAsync(OrganizationId);
        Assert.Single(listed);
    }

    [Fact]
    public async Task CloseAsync_fails_when_period_already_closed()
    {
        var (service, _, _) = CreateServices();

        await service.CloseAsync(OrganizationId, 2026, 2);
        var result = await service.CloseAsync(OrganizationId, 2026, 2);

        Assert.True(result.IsFailure);
        Assert.Contains("already closed", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReopenAsync_reopens_closed_period()
    {
        var (service, _, _) = CreateServices();

        await service.CloseAsync(OrganizationId, 2026, 3);
        var result = await service.ReopenAsync(OrganizationId, 2026, 3);

        Assert.True(result.IsSuccess);
        Assert.Equal(FiscalPeriodStatus.Open, result.Value!.Status);
    }

    [Fact]
    public async Task ReopenAsync_fails_when_period_not_found()
    {
        var (service, _, _) = CreateServices();

        var result = await service.ReopenAsync(OrganizationId, 2026, 5);

        Assert.True(result.IsFailure);
        Assert.Equal("Fiscal period not found.", result.Error);
    }

    [Fact]
    public async Task CloseAsync_records_audit_entry()
    {
        var (service, _, audits) = CreateServices();

        await service.CloseAsync(OrganizationId, 2026, 6);

        var auditEntries = await audits.ListByOrganizationAsync(OrganizationId);

        Assert.Single(auditEntries);
        Assert.Equal(PeriodCloseAction.Close, auditEntries[0].Action);
        Assert.Equal(2026, auditEntries[0].Year);
        Assert.Equal(6, auditEntries[0].Month);
    }

    [Fact]
    public async Task ReopenAsync_records_audit_entry()
    {
        var (service, _, audits) = CreateServices();

        await service.CloseAsync(OrganizationId, 2026, 7);
        await service.ReopenAsync(OrganizationId, 2026, 7);

        var auditEntries = await audits.ListByOrganizationAsync(OrganizationId);

        Assert.Equal(2, auditEntries.Count);
        Assert.Equal(PeriodCloseAction.Reopen, auditEntries[0].Action);
        Assert.Equal(PeriodCloseAction.Close, auditEntries[1].Action);
    }

    [Fact]
    public async Task ListAsync_returns_periods_for_organization()
    {
        var (service, _, _) = CreateServices();

        await service.CloseAsync(OrganizationId, 2026, 8);
        await service.CloseAsync(OrganizationId, 2026, 9);

        var result = await service.ListAsync(OrganizationId);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
    }

    private static (
        PeriodCloseService Service,
        InMemoryFiscalPeriodRepository Periods,
        InMemoryPeriodCloseAuditRepository Audits) CreateServices()
    {
        var periods = new InMemoryFiscalPeriodRepository();
        var audits = new InMemoryPeriodCloseAuditRepository();
        var service = new PeriodCloseService(periods, audits);
        return (service, periods, audits);
    }
}
