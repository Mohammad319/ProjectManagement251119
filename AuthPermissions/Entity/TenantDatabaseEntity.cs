using ProjectManagement.Shared.Base.Users;
using System.ComponentModel.DataAnnotations;

namespace AuthPermissions.Entity
{
    public class TenantEntity : PMCustomerBase
    {
        [Key] public int Id { get; set; }

        public int? TenantDBId { get; set; }
        public TenantDatabaseEntity TenantDB { get; set; }
    }
    public class TenantDatabaseEntity
    {
        public List<TenantEntity> Tenants { get; set; } = [];
        public int Id { get; set; }
        public string Name { get; set; }

        public string ConnectionString { get; set; }
    }
}
