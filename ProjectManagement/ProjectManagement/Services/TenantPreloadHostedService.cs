namespace ProjectManagement.Services
{
    public sealed class TenantPreloadHostedService(ITenantConnectionStringStore store) : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
            => store.ReloadAsync(cancellationToken);

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

}
