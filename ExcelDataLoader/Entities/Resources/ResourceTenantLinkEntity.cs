using Microsoft.EntityFrameworkCore;
using TaskResourceBlueprints.Entities.Questions.Assignments;
using ProjectManagement.Shared.DTO.ProjectAppStorage;

namespace TaskResourceBlueprints.Entities.Resources
{
    [Index(nameof(ResourceId), nameof(TenantId), IsUnique = true, Name = "UX_ResourceTenantLink_Resource_Tenant")]
    [Index(nameof(TenantId), nameof(ResourceId), Name = "IX_ResourceTenantLink_Tenant_Resource")]
    public class ResourceTenantLinkEntity : ResourceTenantLinkBase
    {
        public int Id { get; set; }
        public int TenantId { get; set; }

        public int ResourceId { get; set; }
        public ResourceDefinition? Resource { get; set; }
    }
}
