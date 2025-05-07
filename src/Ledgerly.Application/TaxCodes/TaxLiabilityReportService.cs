using Ledgerly.Application.Abstractions;
using Ledgerly.Domain.Common;
using Ledgerly.Domain.Entities;
using Ledgerly.Domain.Enums;

namespace Ledgerly.Application.TaxCodes;

public sealed class TaxLiabilityReportService
{
    private readonly ITaxCodeRepository _taxCodeRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IBillRepository _billRepository;
    private readonly IExpenseRepository _expenseRepository;

    public TaxLiabilityReportService(
        ITaxCodeRepository taxCodeRepository,
        IInvoiceRepository invoiceRepository,
        IBillRepository billRepository,
        IExpenseRepository expenseRepository)
    {
        _taxCodeRepository = taxCodeRepository;
        _invoiceRepository = invoiceRepository;
        _billRepository = billRepository;
        _expenseRepository = expenseRepository;
    }

    public async Task<Result<TaxLiabilityReportDto>> GenerateAsync(
        Guid organizationId,
        TaxLiabilityReportRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.EndDate < request.StartDate)
        {
            return Result<TaxLiabilityReportDto>.Failure("End date must be on or after start date.");
        }

        var taxCodes = await _taxCodeRepository.ListByOrganizationAsync(
            organizationId,
            includeArchived: true,
            cancellationToken);

        var taxCodeLookup = taxCodes.ToDictionary(t => t.Id);
        var salesByTaxCode = new Dictionary<Guid, decimal>();
        var purchasesByTaxCode = new Dictionary<Guid, decimal>();

        var invoices = await _invoiceRepository.ListByOrganizationAsync(organizationId, cancellationToken);

        foreach (var invoice in invoices.Where(i =>
                     i.IssueDate >= request.StartDate &&
                     i.IssueDate <= request.EndDate &&
                     i.Status is InvoiceStatus.Sent or InvoiceStatus.PartiallyPaid or InvoiceStatus.Paid))
        {
            AccumulateLineTaxes(
                invoice.Lines,
                taxCodeLookup,
                salesByTaxCode,
                line => line.TaxCodeId,
                (line, taxCode) => line.GetTaxAmount(taxCode).Amount);
        }

        var bills = await _billRepository.ListByOrganizationAsync(organizationId, cancellationToken);

        foreach (var bill in bills.Where(b =>
                     b.IssueDate >= request.StartDate &&
                     b.IssueDate <= request.EndDate &&
                     b.Status is BillStatus.Approved or BillStatus.PartiallyPaid or BillStatus.Paid))
        {
            AccumulateLineTaxes(
                bill.Lines,
                taxCodeLookup,
                purchasesByTaxCode,
                line => line.TaxCodeId,
                (line, taxCode) => line.GetTaxAmount(taxCode).Amount);
        }

        var expenses = await _expenseRepository.ListByOrganizationAsync(organizationId, cancellationToken);

        foreach (var expense in expenses.Where(e =>
                     e.ExpenseDate >= request.StartDate &&
                     e.ExpenseDate <= request.EndDate &&
                     e.Status == ExpenseStatus.Posted &&
                     e.TaxCodeId.HasValue))
        {
            if (!taxCodeLookup.TryGetValue(expense.TaxCodeId!.Value, out var taxCode))
            {
                continue;
            }

            purchasesByTaxCode[expense.TaxCodeId.Value] =
                purchasesByTaxCode.GetValueOrDefault(expense.TaxCodeId.Value) +
                taxCode.CalculateTaxAmount(expense.Amount.Amount);
        }

        var taxCodeIds = salesByTaxCode.Keys
            .Union(purchasesByTaxCode.Keys)
            .Union(taxCodes.Select(t => t.Id))
            .Distinct()
            .OrderBy(id => taxCodeLookup.TryGetValue(id, out var taxCode) ? taxCode.Code : string.Empty);

        var lines = new List<TaxCodeReportLineDto>();

        foreach (var taxCodeId in taxCodeIds)
        {
            if (!taxCodeLookup.TryGetValue(taxCodeId, out var taxCode))
            {
                continue;
            }

            var salesTax = salesByTaxCode.GetValueOrDefault(taxCodeId);
            var purchaseTax = purchasesByTaxCode.GetValueOrDefault(taxCodeId);

            if (salesTax == 0m && purchaseTax == 0m)
            {
                continue;
            }

            lines.Add(new TaxCodeReportLineDto(
                taxCode.Id,
                taxCode.Code,
                taxCode.Name,
                taxCode.TaxType,
                salesTax,
                purchaseTax,
                salesTax - purchaseTax));
        }

        var totalSalesTax = lines.Sum(l => l.SalesTax);
        var totalPurchaseTax = lines.Sum(l => l.PurchaseTax);

        return Result<TaxLiabilityReportDto>.Success(new TaxLiabilityReportDto(
            request.StartDate,
            request.EndDate,
            totalSalesTax,
            totalPurchaseTax,
            totalSalesTax - totalPurchaseTax,
            lines));
    }

    private static void AccumulateLineTaxes<TLine>(
        IEnumerable<TLine> lines,
        IReadOnlyDictionary<Guid, TaxCode> taxCodeLookup,
        Dictionary<Guid, decimal> totals,
        Func<TLine, Guid?> taxCodeIdSelector,
        Func<TLine, TaxCode, decimal> taxAmountSelector)
    {
        foreach (var line in lines)
        {
            var taxCodeId = taxCodeIdSelector(line);

            if (!taxCodeId.HasValue ||
                !taxCodeLookup.TryGetValue(taxCodeId.Value, out var taxCode))
            {
                continue;
            }

            totals[taxCodeId.Value] =
                totals.GetValueOrDefault(taxCodeId.Value) +
                taxAmountSelector(line, taxCode);
        }
    }
}
