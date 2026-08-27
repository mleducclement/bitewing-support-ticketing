namespace bitewing.Services;

public class AutoCancelBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AutoCancelBackgroundService> _logger;
    private readonly TimeSpan _interval;

    public AutoCancelBackgroundService(IServiceScopeFactory scopeFactory, ILogger<AutoCancelBackgroundService> logger, IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _interval = TimeSpan.FromMinutes(configuration.GetValue("AutoCancel:CheckIntervalMinutes", 60));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var ticketService = scope.ServiceProvider.GetRequiredService<ITicketService>();
                var cancelledCount = await ticketService.AutoCancelExpiredTicketsAsync();

                if (cancelledCount > 0)
                    _logger.LogInformation("Auto-cancelled {Count} expired blocked ticket(s)", cancelledCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Auto-cancel background check failed");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
