namespace Persistence.Factory
{
    public interface IDbContextFactory
    {
        ShardingSingleDbContext CreateDbContext();
        public int? TenantID { get; }
    }
}
