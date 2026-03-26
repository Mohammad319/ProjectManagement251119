using ProjectManagement.Shared.Base.Users;
using System.ComponentModel.DataAnnotations;

namespace AuthPermissions.Entity
{
    public class TenantEntity : PMCustomerBase
    {
        [Key]
        public int Id { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Please select a database.")]
        public required int TenantDBId { get; set; }

        public TenantDatabaseEntity? TenantDB { get; set; }
    }

    public class TenantDatabaseEntity
    {
        public List<TenantEntity> Tenants { get; set; } = [];

        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(4000)]
        public string ConnectionString { get; set; } = string.Empty;
    }
}
