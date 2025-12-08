using Application.Interfaces.Context;

namespace Persistence.Factory
{
    public interface IDbContextFactory
    {
        IShardingSingleDbContext CreateDbContext();
        public int? TenantID { get; }
    }
}
