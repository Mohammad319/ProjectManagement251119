using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ProjectManagement.Shared.Base.AppTenant;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.App.Dataloader;
using System.Linq.Expressions;
using TaskResourceBlueprints.Entities;
using TaskResourceBlueprints.Entities.Lookups;
using TaskResourceBlueprints.Entities.Resources;
using TaskResourceBlueprints.Entities.Tasks;

namespace TaskResourceBlueprints.Infrastructure;

public class TaskResourceBlueprintsContext : DbContext
{
    public TaskResourceBlueprintsContext(DbContextOptions<TaskResourceBlueprintsContext> options)
        : base(options)
    {
    }

    public int TenantId { get; set; }

    // Core
    public DbSet<ResourceDefinition> Resources { get; set; }
    public DbSet<TaskDefinition> Tasks { get; set; }
    public DbSet<ResourceCategory> ResourceCategories { get; set; }

    // Tenant Links
    public DbSet<ResourceTenantLinkEntity> ResourceTenantLinks { get; set; }

    // State Groups
    public DbSet<TaskStateGroup> TaskStateGroups { get; set; }
    public DbSet<TaskState> TaskStates { get; set; }
    public DbSet<TaskDefinitionStateLink> TaskDefinitionStateLinks { get; set; }

    // Resource Links
    public DbSet<TaskDefinitionResourceLink> TaskDefinitionResourceLinks { get; set; }
    public DbSet<TaskResourceSuggestionFeedback> TaskResourceSuggestionFeedbacks { get; set; }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyTenantProtection();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ApplyTenantProtection();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ignore non-entity types
        modelBuilder.Ignore<RoleDTO>();
        modelBuilder.Ignore<ResourceMetadata>();
        modelBuilder.Ignore<ExternalVariable>();
        modelBuilder.Ignore<Equation>();

        // Apply all IEntityTypeConfiguration<T> in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TaskResourceBlueprintsContext).Assembly);

        ConfigureTenantFilters(modelBuilder);
    }

    private void ConfigureTenantFilters(ModelBuilder modelBuilder)
    {
        var tenantEntityTypes = modelBuilder.Model.GetEntityTypes()
            .Where(t => typeof(ITenantScopedEntity).IsAssignableFrom(t.ClrType))
            .Select(t => t.ClrType)
            .Distinct()
            .ToList();

        foreach (var clrType in tenantEntityTypes)
        {
            var parameter = Expression.Parameter(clrType, "e");

            var tenantIdProperty = Expression.Call(
                typeof(EF),
                nameof(EF.Property),
                new[] { typeof(int) },
                parameter,
                Expression.Constant(nameof(ITenantScopedEntity.TenantId)));

            var tenantIdValue = Expression.Property(Expression.Constant(this), nameof(TenantId));
            var tenantFilterDisabled = Expression.LessThanOrEqual(tenantIdValue, Expression.Constant(0));
            var sameTenant = Expression.Equal(tenantIdProperty, tenantIdValue);

            var body = Expression.OrElse(tenantFilterDisabled, sameTenant);
            modelBuilder.Entity(clrType).HasQueryFilter(Expression.Lambda(body, parameter));
        }
    }

    private void ApplyTenantProtection()
    {
        var now = DateTime.UtcNow;
        var tenantScopedEntries = ChangeTracker.Entries()
            .Where(e => e.Entity is ITenantScopedEntity
                && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        foreach (var entry in tenantScopedEntries)
        {
            var entity = (ITenantScopedEntity)entry.Entity;

            if (TenantId > 0)
            {
                if (entry.State == EntityState.Added && entity.TenantId <= 0)
                {
                    entity.TenantId = TenantId;
                }
                else if (entity.TenantId != TenantId)
                {
                    throw new UnauthorizedAccessException("Cross-tenant blueprint operation detected.");
                }

                if (entry.State is EntityState.Modified or EntityState.Deleted)
                    entry.Property(nameof(ITenantScopedEntity.TenantId)).IsModified = false;
            }

            SetTimestamp(entry, "CreatedAtUtc", now, onlyWhenDefault: true);
            SetTimestamp(entry, "UpdatedAtUtc", now);
        }
    }

    private static void SetTimestamp(EntityEntry entry, string propertyName, DateTime now, bool onlyWhenDefault = false)
    {
        var property = entry.Metadata.FindProperty(propertyName);
        if (property is null)
            return;

        if (entry.State == EntityState.Added || propertyName == "UpdatedAtUtc")
        {
            var propertyEntry = entry.Property(propertyName);
            if (onlyWhenDefault && propertyEntry.CurrentValue is DateTime current && current != default)
                return;

            propertyEntry.CurrentValue = now;
        }
    }
}
