using Ledgerly.Application.Abstractions;
using Ledgerly.Domain.Common;
using Ledgerly.Domain.Enums;

namespace Ledgerly.Application.Reporting;

public sealed class AgedReceivablesReportService
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IContactRepository _contactRepository;

    public AgedReceivablesReportService(
        IInvoiceRepository invoiceRepository,
        IContactRepository contactRepository)
    {
        _invoiceRepository = invoiceRepository;
        _contactRepository = contactRepository;
    }

    public async Task<Result<AgedReceivablesReportDto>> GenerateAsync(
        Guid organizationId,
        AgedReceivablesReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var invoices = await _invoiceRepository.ListByOrganizationAsync(organizationId, cancellationToken);
        var customers = await _contactRepository.ListByTypeAsync(
            organizationId,
            ContactType.Customer,
            includeArchived: true,
            cancellationToken);

        var customerLookup = customers.ToDictionary(customer => customer.Id, customer => customer.Name);

        var lines = new List<AgedReceivableLineDto>();

        foreach (var invoice in invoices.Where(IsOutstanding))
        {
            var balanceDue = invoice.TotalAmount.Amount - invoice.AmountPaid.Amount;

            if (balanceDue <= 0m)
            {
                continue;
            }

            if (invoice.IssueDate > request.AsOfDate)
            {
                continue;
            }

            var daysOverdue = Math.Max(0, request.AsOfDate.DayNumber - invoice.DueDate.DayNumber);
            var buckets = AllocateBuckets(balanceDue, daysOverdue);

            customerLookup.TryGetValue(invoice.CustomerId, out var customerName);

            lines.Add(new AgedReceivableLineDto(
                invoice.Id,
                invoice.InvoiceNumber,
                invoice.CustomerId,
                customerName ?? string.Empty,
                invoice.IssueDate,
                invoice.DueDate,
                invoice.TotalAmount.Amount,
                invoice.AmountPaid.Amount,
                balanceDue,
                daysOverdue,
                buckets.Current,
                buckets.Days1To30,
                buckets.Days31To60,
                buckets.Days61To90,
                buckets.DaysOver90));
        }

        lines = lines
            .OrderBy(line => line.CustomerName)
            .ThenBy(line => line.DueDate)
            .ToList();

        return Result<AgedReceivablesReportDto>.Success(new AgedReceivablesReportDto(
            request.AsOfDate,
            lines.Sum(line => line.BalanceDue),
            lines.Sum(line => line.Current),
            lines.Sum(line => line.Days1To30),
            lines.Sum(line => line.Days31To60),
            lines.Sum(line => line.Days61To90),
            lines.Sum(line => line.DaysOver90),
            lines));
    }

    private static bool IsOutstanding(Domain.Entities.Invoice invoice) =>
        invoice.Status is InvoiceStatus.Sent or InvoiceStatus.PartiallyPaid;

    private static (decimal Current, decimal Days1To30, decimal Days31To60, decimal Days61To90, decimal DaysOver90)
        AllocateBuckets(decimal balanceDue, int daysOverdue)
    {
        if (daysOverdue <= 0)
        {
            return (balanceDue, 0m, 0m, 0m, 0m);
        }

        if (daysOverdue <= 30)
        {
            return (0m, balanceDue, 0m, 0m, 0m);
        }

        if (daysOverdue <= 60)
        {
            return (0m, 0m, balanceDue, 0m, 0m);
        }

        if (daysOverdue <= 90)
        {
            return (0m, 0m, 0m, balanceDue, 0m);
        }

        return (0m, 0m, 0m, 0m, balanceDue);
    }
}
