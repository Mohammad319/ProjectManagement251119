using ProjectManagement.Shared.DTO.ProjectAppStorage;

namespace TaskResourceBlueprints.Entities.Resources
{
    public class ResourceTenantLinkEntity : ResourceTenantLinkBase, ITenantScopedEntity
    {
        public int Id { get; set; }
        public int TenantId { get; set; }

        public int ResourceId { get; set; }
    }
}
