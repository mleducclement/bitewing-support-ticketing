using bitewing.Options;
using bitewing.Services;

namespace bitewing.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<ITicketService, TicketService>();
        services.AddHostedService<AutoCancelBackgroundService>();

        services.Configure<ClaudeOptions>(options =>
        {
            options.ApiKey = configuration["ANTHROPIC_API_KEY"] ?? string.Empty;
            options.Model = configuration.GetValue("Claude:Model", options.Model)!;
        });
        services.AddHttpClient<IClassifier, ClaudeClassifier>(client =>
        {
            client.BaseAddress = new Uri("https://api.anthropic.com/");
        });
        services.AddScoped<IClassificationService, ClassificationService>();
        services.AddHostedService<ClassificationBackgroundService>();

        return services;
    }
}