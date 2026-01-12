namespace Persistence.Factory
{

    public interface IDbContextFactoryTenant
    {
        Task<ShardingSingleDbContext> CreateDbContextAsync(CancellationToken ct = default);
    }

}
