using System.Text.Json;
using Ledgerly.Application.Abstractions;
using Ledgerly.Application.Bills;
using Ledgerly.Application.Invoices;
using Ledgerly.Application.Journals;
using Ledgerly.Domain.Entities;
using Ledgerly.Domain.Enums;

namespace Ledgerly.Application.Recurring;

public sealed class RecurringTransactionProcessor
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IRecurringScheduleRepository _repository;
    private readonly InvoiceService _invoiceService;
    private readonly BillService _billService;
    private readonly JournalService _journalService;

    public RecurringTransactionProcessor(
        IRecurringScheduleRepository repository,
        InvoiceService invoiceService,
        BillService billService,
        JournalService journalService)
    {
        _repository = repository;
        _invoiceService = invoiceService;
        _billService = billService;
        _journalService = journalService;
    }

    public async Task<int> ProcessDueAsync(DateTimeOffset asOfUtc, CancellationToken cancellationToken = default)
    {
        var dueSchedules = await _repository.ListDueAsync(asOfUtc, cancellationToken);
        var processed = 0;
        foreach (var item in dueSchedules)
        {
            var schedule = item;
            while (!schedule.IsPaused && schedule.NextRunUtc <= asOfUtc)
            {
                if (await ProcessOccurrenceAsync(schedule, schedule.NextRunUtc, cancellationToken))
                    processed++;
                schedule = (await _repository.GetByIdAsync(schedule.OrganizationId, schedule.Id, cancellationToken))!;
            }
        }

        return processed;
    }

    private async Task<bool> ProcessOccurrenceAsync(RecurringSchedule schedule, DateTimeOffset occurrenceUtc, CancellationToken cancellationToken)
    {
        if (await _repository.GetRunByOccurrenceAsync(schedule.Id, occurrenceUtc, cancellationToken) is not null)
        {
            schedule.AdvanceNextRun(CronScheduleHelper.GetNextOccurrence(schedule.CronExpression, occurrenceUtc));
            await _repository.UpdateAsync(schedule, cancellationToken);
            return false;
        }

        var generatedEntityId = await GenerateAsync(schedule, occurrenceUtc, cancellationToken);
        if (generatedEntityId is null)
            return false;

        await _repository.AddRunAsync(
            new RecurringScheduleRun(schedule.Id, schedule.OrganizationId, occurrenceUtc, generatedEntityId.Value, schedule.TransactionType, RecurringScheduleRunStatus.Succeeded),
            cancellationToken);
        schedule.AdvanceNextRun(CronScheduleHelper.GetNextOccurrence(schedule.CronExpression, occurrenceUtc));
        await _repository.UpdateAsync(schedule, cancellationToken);
        return true;
    }

    private Task<Guid?> GenerateAsync(RecurringSchedule schedule, DateTimeOffset occurrenceUtc, CancellationToken cancellationToken)
        => schedule.TransactionType switch
        {
            RecurringTransactionType.Invoice => GenerateInvoiceAsync(schedule, occurrenceUtc, cancellationToken),
            RecurringTransactionType.Bill => GenerateBillAsync(schedule, occurrenceUtc, cancellationToken),
            RecurringTransactionType.Journal => GenerateJournalAsync(schedule, occurrenceUtc, cancellationToken),
            _ => Task.FromResult<Guid?>(null),
        };

    private async Task<Guid?> GenerateInvoiceAsync(RecurringSchedule schedule, DateTimeOffset occurrenceUtc, CancellationToken cancellationToken)
    {
        var template = JsonSerializer.Deserialize<RecurringInvoiceTemplate>(schedule.TemplateJson, JsonOptions);
        if (template is null || template.Lines.Count == 0)
            return null;

        var issueDate = DateOnly.FromDateTime(occurrenceUtc.UtcDateTime);
        var created = await _invoiceService.CreateAsync(
            new CreateInvoiceRequest(
                schedule.OrganizationId,
                template.CustomerId,
                $"{template.NumberPrefix}-{occurrenceUtc:yyyyMMddHHmmss}",
                issueDate,
                issueDate.AddDays(template.DueDaysOffset),
                template.Currency,
                template.IncomeAccountId,
                template.ReceivableAccountId),
            cancellationToken);
        if (created.IsFailure)
            return null;

        foreach (var line in template.Lines)
        {
            var added = await _invoiceService.AddLineAsync(
                schedule.OrganizationId,
                created.Value!.Id,
                new AddInvoiceLineRequest(line.Description, line.Quantity, line.UnitPriceAmount, line.TaxCodeId, line.DiscountAmount),
                cancellationToken);
            if (added.IsFailure)
                return null;
        }

        var sent = await _invoiceService.SendAsync(schedule.OrganizationId, created.Value.Id, cancellationToken);
        return sent.IsSuccess ? sent.Value!.Id : null;
    }

    private async Task<Guid?> GenerateBillAsync(RecurringSchedule schedule, DateTimeOffset occurrenceUtc, CancellationToken cancellationToken)
    {
        var template = JsonSerializer.Deserialize<RecurringBillTemplate>(schedule.TemplateJson, JsonOptions);
        if (template is null || template.Lines.Count == 0)
            return null;

        var issueDate = DateOnly.FromDateTime(occurrenceUtc.UtcDateTime);
        var created = await _billService.CreateAsync(
            new CreateBillRequest(
                schedule.OrganizationId,
                template.VendorId,
                $"{template.NumberPrefix}-{occurrenceUtc:yyyyMMddHHmmss}",
                issueDate,
                issueDate.AddDays(template.DueDaysOffset),
                template.Currency,
                template.ExpenseAccountId,
                template.PayableAccountId),
            cancellationToken);
        if (created.IsFailure)
            return null;

        foreach (var line in template.Lines)
        {
            var added = await _billService.AddLineAsync(
                schedule.OrganizationId,
                created.Value!.Id,
                new AddBillLineRequest(line.Description, line.Quantity, line.UnitPriceAmount, line.TaxCodeId, line.DiscountAmount),
                cancellationToken);
            if (added.IsFailure)
                return null;
        }

        var approved = await _billService.ApproveAsync(schedule.OrganizationId, created.Value.Id, cancellationToken);
        return approved.IsSuccess ? approved.Value!.Id : null;
    }

    private async Task<Guid?> GenerateJournalAsync(RecurringSchedule schedule, DateTimeOffset occurrenceUtc, CancellationToken cancellationToken)
    {
        var template = JsonSerializer.Deserialize<RecurringJournalTemplate>(schedule.TemplateJson, JsonOptions);
        if (template is null || template.Lines.Count < 2)
            return null;

        var reference = string.IsNullOrWhiteSpace(template.ReferencePrefix)
            ? occurrenceUtc.ToString("yyyyMMddHHmmss")
            : $"{template.ReferencePrefix}-{occurrenceUtc:yyyyMMddHHmmss}";
        var created = await _journalService.CreateDraftAsync(
            new CreateJournalRequest(
                schedule.OrganizationId,
                DateOnly.FromDateTime(occurrenceUtc.UtcDateTime),
                reference,
                template.Description,
                template.BaseCurrency),
            cancellationToken);
        if (created.IsFailure)
            return null;

        foreach (var line in template.Lines)
        {
            var added = await _journalService.AddLineAsync(
                schedule.OrganizationId,
                created.Value!.Id,
                new AddJournalLineRequest(line.AccountId, line.DebitAmount, line.CreditAmount, line.Memo),
                cancellationToken);
            if (added.IsFailure)
                return null;
        }

        var posted = await _journalService.PostAsync(schedule.OrganizationId, created.Value.Id, cancellationToken);
        return posted.IsSuccess ? posted.Value!.Id : null;
    }
}
