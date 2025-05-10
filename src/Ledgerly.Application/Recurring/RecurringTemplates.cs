namespace Ledgerly.Application.Recurring;

public sealed record RecurringLineTemplate(string Description, decimal Quantity, decimal UnitPriceAmount, Guid? TaxCodeId, decimal DiscountAmount);
public sealed record RecurringInvoiceTemplate(Guid CustomerId, string NumberPrefix, string Currency, Guid IncomeAccountId, Guid ReceivableAccountId, int DueDaysOffset, IReadOnlyList<RecurringLineTemplate> Lines);
public sealed record RecurringBillTemplate(Guid VendorId, string NumberPrefix, string Currency, Guid ExpenseAccountId, Guid PayableAccountId, int DueDaysOffset, IReadOnlyList<RecurringLineTemplate> Lines);
public sealed record RecurringJournalLineTemplate(Guid AccountId, decimal DebitAmount, decimal CreditAmount, string? Memo);
public sealed record RecurringJournalTemplate(string? ReferencePrefix, string Description, string BaseCurrency, IReadOnlyList<RecurringJournalLineTemplate> Lines);
