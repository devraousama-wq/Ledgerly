namespace Ledgerly.Application.Invoices; public sealed record AddInvoiceLineRequest(string Description,decimal Quantity,decimal UnitPriceAmount,Guid? TaxCodeId,decimal DiscountAmount);
