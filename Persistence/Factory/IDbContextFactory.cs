namespace Persistence.Factory
{
    public interface IDbContextFactory
    {
        ShardingSingleDbContext CreateDbContext();
    }
}
