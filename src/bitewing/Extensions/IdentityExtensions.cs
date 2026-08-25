using bitewing.Data;
using Microsoft.AspNetCore.Identity;

namespace bitewing.Extensions;

public static class IdentityExtensions
{
    public static IServiceCollection AddAppIdentity(this IServiceCollection services)
    {
        services
            .AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddAuthorizationBuilder()
            .AddPolicy("TeamLeadOnly", policy => policy.RequireRole("TeamLead"))
            .AddPolicy("AgentAccess", policy => policy.RequireRole(["TeamLead", "Agent"]));

        services.ConfigureApplicationCookie(options =>
        {
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = 401;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = 403;
                return Task.CompletedTask;
            };
        });

        return services;
    }
}