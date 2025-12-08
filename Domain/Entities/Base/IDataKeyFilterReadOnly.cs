namespace Domain.Entities.Base
{
    /// <summary>
    /// Marks an entity that is bound to a specific tenant.
    /// </summary>
    public interface IDataKeyFilterReadOnly
    {
        int TenantId { get; set; }
    }
}

