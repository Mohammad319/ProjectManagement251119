using ProjectManagement.Shared.Base.Users;
using System.ComponentModel.DataAnnotations;

namespace AuthPermissions.Entity
{
    public class TenantEntity : PMCustomerBase
    {
        [Key] public int Id { get; set; }

        // كل Tenant يجب أن يكون مربوط بقاعدة بيانات (DB shard / catalog)
        public int TenantDBId { get; set; }

        public TenantDatabaseEntity TenantDB { get; set; } = null!;
    }
    public class TenantDatabaseEntity
    {
        public List<TenantEntity> Tenants { get; set; } = [];
        public int Id { get; set; }

        [MaxLength(80)]
        public string Name { get; set; } = string.Empty;

        // Connection strings قد تكون طويلة نسبيًا (خصوصًا مع خصائص SQL Server)
        [MaxLength(1000)]
        public string ConnectionString { get; set; } = string.Empty;
    }
}
