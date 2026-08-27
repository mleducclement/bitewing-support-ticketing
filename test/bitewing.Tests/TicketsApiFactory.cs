using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;

namespace bitewing.Tests;

// Boots the real app (real controller, real EF Core/Npgsql pipeline, real
// migrations and seeding) against a disposable Postgres container, rather
// than an in-memory or mocked database.
public class TicketsApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("bitewing_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _dbContainer.GetConnectionString()
            });
        });

        // Hosted services (e.g. AutoCancelBackgroundService) would otherwise run
        // against this same container during every test, mutating tickets other
        // tests are actively manipulating. Tests call the underlying service
        // methods directly instead of relying on the background job's timing.
        builder.ConfigureServices(services => services.RemoveAll<IHostedService>());
    }

    public Task InitializeAsync() => _dbContainer.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}