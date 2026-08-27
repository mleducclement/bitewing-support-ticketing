using bitewing.Services;

namespace bitewing.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ITicketService, TicketService>();
        services.AddHostedService<AutoCancelBackgroundService>();

        return services;
    }
}