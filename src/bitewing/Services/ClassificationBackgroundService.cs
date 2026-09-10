namespace bitewing.Services;

public class ClassificationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ClassificationBackgroundService> _logger;
    private readonly TimeSpan _interval;

    public ClassificationBackgroundService(IServiceScopeFactory scopeFactory,
        ILogger<ClassificationBackgroundService> logger, IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _interval = TimeSpan.FromSeconds(configuration.GetValue("Classification:PollIntervalSeconds", 60));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var classificationService = scope.ServiceProvider.GetRequiredService<IClassificationService>();
                var processedCount = await classificationService.ProcessPendingJobsAsync(stoppingToken);

                if (processedCount > 0)
                    _logger.LogInformation("Processed {Count} classification job(s)", processedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Classification background check failed");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
