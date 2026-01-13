using Domain.Entities.Base;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Persistence.Interceptors;

public sealed class TenantAuditSaveChangesInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private static void Apply(DbContext? context)
    {
        if (context is null) return;
        if (context is not ShardingSingleDbContext db) return;

        if (db.TenantId <= 0)
            throw new UnauthorizedAccessException("TenantId is not set on DbContext.");

        var now = DateTime.UtcNow;
        var userId = (db.CurrentUserId is > 0) ? db.CurrentUserId : null;

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted);

        foreach (var entry in entries)
        {
            ApplyTenant(entry, db);
            ApplyAudit(entry, now, userId);
            ApplySoftDelete(entry, now, userId);
        }
    }

    private static void ApplyTenant(EntityEntry entry, ShardingSingleDbContext db)
    {
        if (entry.Entity is not IDataKeyFilterReadOnly tenantEntity)
            return;

        if (entry.State == EntityState.Added)
        {
            tenantEntity.TenantId = db.TenantId;
            return;
        }

        // منع تعديل TenantId في التحديثات/الحذف
        entry.Property(nameof(IDataKeyFilterReadOnly.TenantId)).IsModified = false;

        // حماية إضافية ضد أي محاولة تحديث cross-tenant
        var current = tenantEntity.TenantId;
        if (current != db.TenantId)
            throw new UnauthorizedAccessException("Cross-tenant operation detected.");
    }

    private static void ApplyAudit(EntityEntry entry, DateTime now, int? userId)
    {
        if (entry.Entity is not IAuditable auditable)
            return;

        if (entry.State == EntityState.Added)
        {
            auditable.CreatedAt = now;
            auditable.CreatedBy = userId;
        }
        else if (entry.State == EntityState.Modified)
        {
            entry.Property(nameof(IAuditable.CreatedAt)).IsModified = false;
            entry.Property(nameof(IAuditable.CreatedBy)).IsModified = false;
        }

        if (entry.State is EntityState.Added or EntityState.Modified)
        {
            auditable.UpdatedAt = now;
            auditable.UpdatedBy = userId;
        }
    }

    private static void ApplySoftDelete(EntityEntry entry, DateTime now, int? userId)
    {
        if (entry.State != EntityState.Deleted)
            return;

        if (entry.Entity is not ISoftDeletable soft)
            return;

        // تحويل الحذف إلى soft delete
        entry.State = EntityState.Modified;

        soft.IsDeleted = true;
        soft.DeletedAt = now;
        soft.DeletedBy = userId;

        // لا نسمح بتغيير tenant ولا created audit
        if (entry.Metadata.FindProperty(nameof(IDataKeyFilterReadOnly.TenantId)) is not null)
            entry.Property(nameof(IDataKeyFilterReadOnly.TenantId)).IsModified = false;

        if (entry.Entity is IAuditable)
        {
            entry.Property(nameof(IAuditable.CreatedAt)).IsModified = false;
            entry.Property(nameof(IAuditable.CreatedBy)).IsModified = false;
        }
    }
}
