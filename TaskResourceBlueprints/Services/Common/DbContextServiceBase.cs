using Microsoft.EntityFrameworkCore;
using TaskResourceBlueprints.Infrastructure;

namespace TaskResourceBlueprints.Services.Common
{
    public abstract class DbContextServiceBase(IDbContextFactory<TaskResourceBlueprintsContext> contextFactory)
    {
        protected readonly IDbContextFactory<TaskResourceBlueprintsContext> ContextFactory = contextFactory;

        protected Task<TaskResourceBlueprintsContext> CreateDbContextAsync(
            CancellationToken cancellationToken = default)
            => ContextFactory.CreateDbContextAsync(cancellationToken);

        protected TaskResourceBlueprintsContext CreateDbContext()
            => ContextFactory.CreateDbContext();
    }
}
