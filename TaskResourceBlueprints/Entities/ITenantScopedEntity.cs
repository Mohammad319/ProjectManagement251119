namespace TaskResourceBlueprints.Entities;

public interface ITenantScopedEntity
{
    int TenantId { get; set; }
}
