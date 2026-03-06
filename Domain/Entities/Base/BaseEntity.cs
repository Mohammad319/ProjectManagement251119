using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace Domain.Entities.Base
{
    /// <summary>
    /// قاعدة عامة لكل الكيانات المتعددة التينانت في هذه المكتبة.
    /// المفتاح الأساسي Generic (يمكن أن يكون Guid أو int).
    /// </summary>
    /// <typeparam name="TKey">نوع المفتاح الأساسي (Guid أو int مثلاً).</typeparam>
    // NOTE: Removed the global (TenantId, Id) index to reduce redundant indexes and write overhead.
    // Add explicit indexes per-entity based on real query patterns.
    //[Index(nameof(TenantId))] // enable only if you measure benefit for tenant-only scans
    public abstract class BaseEntity<TKey> : IDataKeyFilterReadOnly
    {
        /// <summary>
        /// المفتاح الأساسي للكيان.
        /// EF Core سيتعامل مع TKey حسب نوع الكلاس المشتق.
        /// </summary>
        public TKey Id { get; set; } = default!;

        /// <summary>
        /// معرّف التينانت، يُستخدم في الفلترة والتأمين متعدد التينانت.
        /// يتم تعيينه تلقائياً في DbContext داخل UpdateTenantId().
        /// </summary>
        [JsonIgnore] public int TenantId { get; set; }
    }

    /// <summary>
    /// كلاس أساس للكيانات التي تستخدم Guid كمفتاح أساسي.
    /// مثل: Project, Organisation, User, Account, ...
    /// </summary>
    public abstract class GuidBaseEntity : BaseEntity<Guid>
    {
        protected GuidBaseEntity()
        {
            Id = Guid.NewGuid();
        }
    }

    /// <summary>
    /// كلاس أساس للكيانات التي تستخدم int كمفتاح أساسي (Identity).
    /// مثل: Resource, Logs, التفاصيل كثيرة العدد...
    /// </summary>
    public abstract class IntBaseEntity : BaseEntity<int>
    {
        // لا نحتاج أي منطق إضافي هنا،
        // EF Core سيتولى توليد القيمة (Identity) من قاعدة البيانات.
    }
}

