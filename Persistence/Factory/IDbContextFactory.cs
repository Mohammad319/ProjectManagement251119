namespace Persistence.Factory
{

    public interface IDbContextFactory
    {
        ShardingSingleDbContext CreateDbContext();
        Task<ShardingSingleDbContext> CreateDbContextAsync(CancellationToken ct = default);
    }

}
