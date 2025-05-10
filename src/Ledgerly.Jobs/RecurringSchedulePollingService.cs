namespace Ledgerly.Jobs;

public sealed class RecurringSchedulePollingService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RecurringSchedulePollingService> _logger;

    public RecurringSchedulePollingService(IServiceScopeFactory scopeFactory, ILogger<RecurringSchedulePollingService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<Ledgerly.Application.Recurring.RecurringTransactionProcessor>();
                var processed = await processor.ProcessDueAsync(DateTimeOffset.UtcNow, stoppingToken);
                if (processed > 0)
                    _logger.LogInformation("Processed {Count} recurring schedules at {Time}", processed, DateTimeOffset.UtcNow);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Recurring transaction processing failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
