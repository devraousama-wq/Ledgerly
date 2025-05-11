using Ledgerly.Application.Accounts;
using Ledgerly.Application.Bills;
using Ledgerly.Application.Contacts;
using Ledgerly.Application.Expenses;
using Ledgerly.Application.Invoices;
using Ledgerly.Application.Journals;
using Ledgerly.Application.Periods;
using Ledgerly.Application.Reconciliation;
using Ledgerly.Application.Recurring;
using Ledgerly.Application.Reporting;
using Ledgerly.Application.Reporting.Export;
using Ledgerly.Application.TaxCodes;
using Microsoft.Extensions.DependencyInjection;

namespace Ledgerly.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AccountService>();
        services.AddScoped<JournalService>();
        services.AddScoped<ContactService>();
        services.AddScoped<InvoiceService>();
        services.AddScoped<BillService>();
        services.AddScoped<ExpenseService>();
        services.AddScoped<TaxCodeService>();
        services.AddScoped<TaxLiabilityReportService>();
        services.AddScoped<BankReconciliationService>();
        services.AddScoped<ReconciliationReportService>();
        services.AddScoped<RecurringScheduleService>();
        services.AddScoped<RecurringTransactionProcessor>();
        services.AddScoped<PeriodCloseService>();
        services.AddScoped<PostedJournalAggregator>();
        services.AddScoped<TrialBalanceReportService>();
        services.AddScoped<ProfitAndLossReportService>();
        services.AddScoped<BalanceSheetReportService>();
        services.AddScoped<AgedReceivablesReportService>();
        services.AddSingleton<ReportPdfExporter>();
        services.AddSingleton<ReportExcelExporter>();
        return services;
    }
}
