using Ledgerly.Application.Abstractions;
using Ledgerly.Application.TaxCodes;
using Ledgerly.Domain.Entities;
using Ledgerly.Domain.Enums;
using Ledgerly.Domain.ValueObjects;

namespace Ledgerly.Application.Tests;

public class TaxLiabilityReportServiceTests
{
    private static readonly Guid OrganizationId = Guid.NewGuid();

    [Fact]
    public async Task GenerateAsync_aggregates_sales_and_purchases_by_tax_code()
    {
        var taxCodes = new InMemoryTaxCodeRepository();
        var invoices = new InMemoryInvoiceRepository();
        var bills = new InMemoryBillRepository();
        var expenses = new InMemoryExpenseRepository();
        var service = new TaxLiabilityReportService(taxCodes, invoices, bills, expenses);
        var liability = Guid.NewGuid();
        var salesTax = new TaxCode(OrganizationId, "GST", "GST", TaxType.Gst, 0.1m, liability);
        var purchaseTax = new TaxCode(OrganizationId, "INPUT", "Input Tax", TaxType.Vat, 0.05m, liability);

        await taxCodes.AddAsync(salesTax);
        await taxCodes.AddAsync(purchaseTax);

        var customer = Guid.NewGuid();
        var vendor = Guid.NewGuid();
        var income = Guid.NewGuid();
        var receivable = Guid.NewGuid();
        var expenseAccount = Guid.NewGuid();
        var payable = Guid.NewGuid();

        var invoice = new Invoice(
            OrganizationId,
            customer,
            "INV-1",
            new DateOnly(2026, 3, 15),
            new DateOnly(2026, 4, 15),
            new CurrencyCode("USD"),
            income,
            receivable);

        invoice.AddLine("Service", 1m, new Money(200m, new CurrencyCode("USD")), salesTax.Id, new Money(0m, new CurrencyCode("USD")));
        invoice.Send(new Dictionary<Guid, TaxCode> { [salesTax.Id] = salesTax });
        await invoices.AddAsync(invoice);

        var bill = new Bill(
            OrganizationId,
            vendor,
            "BILL-1",
            new DateOnly(2026, 3, 10),
            new DateOnly(2026, 4, 10),
            new CurrencyCode("USD"),
            expenseAccount,
            payable);

        bill.AddLine("Supplies", 1m, new Money(100m, new CurrencyCode("USD")), purchaseTax.Id, new Money(0m, new CurrencyCode("USD")));
        bill.Approve(new Dictionary<Guid, TaxCode> { [purchaseTax.Id] = purchaseTax });
        await bills.AddAsync(bill);

        var expense = new Expense(
            OrganizationId,
            "Fuel",
            new DateOnly(2026, 3, 20),
            new CurrencyCode("USD"),
            expenseAccount,
            Guid.NewGuid(),
            new Money(50m, new CurrencyCode("USD")),
            null,
            purchaseTax.Id,
            null);

        expense.Post(purchaseTax);
        await expenses.AddAsync(expense);

        var result = await service.GenerateAsync(
            OrganizationId,
            new TaxLiabilityReportRequest(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31)));

        Assert.True(result.IsSuccess);
        Assert.Equal(20m, result.Value!.TotalSalesTax);
        Assert.Equal(7.50m, result.Value.TotalPurchaseTax);
        Assert.Equal(12.50m, result.Value.NetLiability);
        Assert.Equal(2, result.Value.Lines.Count);

        var salesLine = result.Value.Lines.Single(l => l.TaxCodeId == salesTax.Id);
        Assert.Equal(20m, salesLine.SalesTax);
        Assert.Equal(0m, salesLine.PurchaseTax);

        var purchaseLine = result.Value.Lines.Single(l => l.TaxCodeId == purchaseTax.Id);
        Assert.Equal(0m, purchaseLine.SalesTax);
        Assert.Equal(7.50m, purchaseLine.PurchaseTax);
    }

    [Fact]
    public async Task GenerateAsync_fails_when_end_date_before_start_date()
    {
        var service = new TaxLiabilityReportService(
            new InMemoryTaxCodeRepository(),
            new InMemoryInvoiceRepository(),
            new InMemoryBillRepository(),
            new InMemoryExpenseRepository());

        var result = await service.GenerateAsync(
            OrganizationId,
            new TaxLiabilityReportRequest(new DateOnly(2026, 3, 31), new DateOnly(2026, 3, 1)));

        Assert.True(result.IsFailure);
        Assert.Contains("End date", result.Error);
    }

    private sealed class InMemoryTaxCodeRepository : ITaxCodeRepository
    {
        private readonly List<TaxCode> _taxCodes = new();

        public Task<TaxCode?> GetByIdAsync(
            Guid organizationId,
            Guid taxCodeId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_taxCodes.FirstOrDefault(t =>
                t.OrganizationId == organizationId && t.Id == taxCodeId));

        public Task<TaxCode?> GetByCodeAsync(
            Guid organizationId,
            string code,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_taxCodes.FirstOrDefault(t =>
                t.OrganizationId == organizationId &&
                string.Equals(t.Code, code, StringComparison.Ordinal)));

        public Task<IReadOnlyList<TaxCode>> ListByOrganizationAsync(
            Guid organizationId,
            bool includeArchived = false,
            CancellationToken cancellationToken = default)
        {
            var query = _taxCodes.Where(t => t.OrganizationId == organizationId);

            if (!includeArchived)
            {
                query = query.Where(t => !t.IsArchived);
            }

            IReadOnlyList<TaxCode> results = query.OrderBy(t => t.Code).ToList();
            return Task.FromResult(results);
        }

        public Task AddAsync(TaxCode taxCode, CancellationToken cancellationToken = default)
        {
            _taxCodes.Add(taxCode);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(TaxCode taxCode, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemoryInvoiceRepository : IInvoiceRepository
    {
        private readonly List<Invoice> _invoices = new();

        public Task<Invoice?> GetByIdAsync(
            Guid organizationId,
            Guid invoiceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_invoices.FirstOrDefault(i =>
                i.OrganizationId == organizationId && i.Id == invoiceId));

        public Task<IReadOnlyList<Invoice>> ListByOrganizationAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Invoice>>(_invoices.Where(i => i.OrganizationId == organizationId).ToList());

        public Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
        {
            _invoices.Add(invoice);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemoryBillRepository : IBillRepository
    {
        private readonly List<Bill> _bills = new();

        public Task<Bill?> GetByIdAsync(
            Guid organizationId,
            Guid billId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_bills.FirstOrDefault(b =>
                b.OrganizationId == organizationId && b.Id == billId));

        public Task<IReadOnlyList<Bill>> ListByOrganizationAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Bill>>(_bills.Where(b => b.OrganizationId == organizationId).ToList());

        public Task AddAsync(Bill bill, CancellationToken cancellationToken = default)
        {
            _bills.Add(bill);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Bill bill, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemoryExpenseRepository : IExpenseRepository
    {
        private readonly List<Expense> _expenses = new();

        public Task<Expense?> GetByIdAsync(
            Guid organizationId,
            Guid expenseId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_expenses.FirstOrDefault(e =>
                e.OrganizationId == organizationId && e.Id == expenseId));

        public Task<IReadOnlyList<Expense>> ListByOrganizationAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Expense>>(_expenses.Where(e => e.OrganizationId == organizationId).ToList());

        public Task AddAsync(Expense expense, CancellationToken cancellationToken = default)
        {
            _expenses.Add(expense);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Expense expense, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
