using Application.Interfaces.Context;

namespace Persistence.Factory
{
    public interface IDbContextFactory
    {
        IShardingSingleDbContext CreateDbContext();
        public int? TenantID { get; }
        public int? DepartmentID { get; }
        public int UserID { get; }
    }
}
