using bitewing.Configuration;
using bitewing.Data;
using Microsoft.AspNetCore.Identity;

namespace bitewing.Extensions;

public static class SeedDataExtensions
{
    public static async Task SeedAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        await SeedRolesAsync(roleManager);
        await SeedUsersAsync(userManager, configuration, logger);
    }

    private static async Task SeedUsersAsync(UserManager<ApplicationUser> userManager, IConfiguration configuration, ILogger<Program> logger)
    {
        var users = configuration.GetSection("SeedUsers:Users").Get<List<SeedUserOption>>() ??
                    throw new InvalidOperationException("SeedUsers:Users configuration not set");
        var defaultPassword = configuration["SeedUsers:DefaultPassword"] ??
                              throw new InvalidOperationException("SeedUsers:DefaultPassword configuration not set");

        foreach (var user in users)
        {
            if (await userManager.FindByEmailAsync(user.Email) is not null) continue;

            var appUser = new ApplicationUser
            {
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserName = user.UserName,
            };

            var createResult = await userManager.CreateAsync(appUser, defaultPassword);

            if (!createResult.Succeeded)
            {
                var createErrors = string.Join(", ", createResult.Errors.Select(x => x.Description));
                logger.LogError("The user with email {Email} could not be created - Errors: {Errors}", user.Email,
                    createErrors);
                continue;
            }

            var addRoleResult = await userManager.AddToRoleAsync(appUser, user.Role);

            if (addRoleResult.Succeeded) continue;

            var roleErrors = string.Join(", ", addRoleResult.Errors.Select(x => x.Description));
            logger.LogError("The role {Role} could not be added to user {Email} - Errors: {Errors}", user.Role,
                user.Email, roleErrors);
        }
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in new[] { "Agent", "TeamLead" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}