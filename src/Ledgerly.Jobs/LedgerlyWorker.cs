namespace Ledgerly.Jobs;

public sealed class LedgerlyWorker : BackgroundService
{
    private readonly ILogger<LedgerlyWorker> _logger;

    public LedgerlyWorker(ILogger<LedgerlyWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Ledgerly worker heartbeat at {Time}", DateTimeOffset.UtcNow);
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
