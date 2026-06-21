using Microsoft.Extensions.Hosting;

namespace ProjectManagement.Services.DemoSeed;

/// <summary>
/// Dev/test only. When <c>DemoSeed:Enabled = true</c> (and the environment is not Production)
/// this seeds three demo companies/tenants with realistic, idempotent test data so the team can
/// exercise multi-tenant isolation, department access, roles, sharing, and import/export mapping.
/// Runs once at startup after AuthPermissions has been initialized; a no-op when the flag is off.
/// </summary>
public sealed class DemoSeedHostedService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    IHostEnvironment environment,
    ILogger<DemoSeedHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!configuration.GetValue<bool>("DemoSeed:Enabled"))
            return;

        // Hard safety stop: never generate demo data in a production environment, even if the flag is on.
        if (environment.IsProduction())
        {
            logger.LogWarning("DemoSeed:Enabled is true but the environment is Production. Demo seeding skipped.");
            return;
        }

        logger.LogInformation("DemoSeed enabled — seeding demo tenants/data (idempotent).");

        try
        {
            using var scope = scopeFactory.CreateScope();
            var seeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
            var summary = await seeder.SeedAsync(cancellationToken);
            logger.LogInformation("DemoSeed finished. {Summary}", summary);
        }
        catch (Exception ex)
        {
            // Demo seeding must never take the app down — log and continue startup.
            logger.LogError(ex, "DemoSeed failed. The application will continue to start.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
