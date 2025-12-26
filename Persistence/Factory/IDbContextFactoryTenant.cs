namespace Persistence.Factory
{

    public interface IDbContextFactoryTenant
    {
        ShardingSingleDbContext CreateDbContext();
        Task<ShardingSingleDbContext> CreateDbContextAsync(CancellationToken ct = default);
    }

}
