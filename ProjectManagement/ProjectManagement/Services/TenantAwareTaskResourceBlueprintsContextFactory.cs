using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskResourceBlueprints.Infrastructure;

namespace ProjectManagement.Services;

public sealed class TenantAwareTaskResourceBlueprintsContextFactory(
    DbContextOptions<TaskResourceBlueprintsContext> options,
    TenantContext tenantContext,
    ITenantContextResolver resolver,
    ILogger<TenantAwareTaskResourceBlueprintsContextFactory> logger)
    : IDbContextFactory<TaskResourceBlueprintsContext>
{
    public TaskResourceBlueprintsContext CreateDbContext()
    {
        if (tenantContext.TenantId <= 0)
        {
            logger.LogDebug("Creating blueprint DbContext without TenantId; tenant filter remains disabled.");
            return new TaskResourceBlueprintsContext(options);
        }

        return new TaskResourceBlueprintsContext(options)
        {
            TenantId = tenantContext.TenantId
        };
    }

    public async Task<TaskResourceBlueprintsContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        if (tenantContext.TenantId <= 0)
            await resolver.EnsureResolvedAsync(cancellationToken).ConfigureAwait(false);

        return CreateDbContext();
    }
}
