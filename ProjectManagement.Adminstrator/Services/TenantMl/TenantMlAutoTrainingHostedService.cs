namespace ProjectManagement.Adminstrator.Services.TenantMl;

public sealed class TenantMlAutoTrainingHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<TenantMlAutoTrainingHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(6));

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunOnceAsync(stoppingToken);

            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ITenantMlTrainingService>();
            var trained = await service.RunDueAutoTrainingAsync(ct);
            if (trained > 0)
                logger.LogInformation("Tenant ML auto-training completed for {TenantCount} tenant(s).", trained);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Tenant ML auto-training check failed.");
        }
    }
}
