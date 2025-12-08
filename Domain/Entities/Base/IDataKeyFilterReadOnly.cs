namespace Domain.Entities.Base
{
    /// <summary>
    /// Marks an entity that is bound to a specific tenant.
    /// </summary>
    /// <summary>
    /// تُستخدم لتمييز الكيانات التي يتم فلترتها حسب TenantId
    /// عن طريق Global Query Filter في DbContext.
    /// </summary>
    public interface IDataKeyFilterReadOnly
    {
        int TenantId { get; set; }
    }
}

