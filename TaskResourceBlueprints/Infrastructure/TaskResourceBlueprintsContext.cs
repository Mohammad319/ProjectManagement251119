using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Base.AppTenant;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.App.Dataloader;
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

    // === Core ===
    public DbSet<TaskUnitGroup> TaskUnitGroups { get; set; }
    public DbSet<ResourceDefinition> Resources { get; set; }
    public DbSet<TaskDefinition> Tasks { get; set; }
    public DbSet<ResourceCategory> ResourceCategories { get; set; }


    // === Conditions & Requirements ===


    // === Tenant Links ===
    public DbSet<ResourceTenantLinkEntity> ResourceTenantLinks { get; set; }

    // === State Groups ===
    public DbSet<TaskStateGroup> TaskStateGroups { get; set; }
    public DbSet<TaskState> TaskStates { get; set; }
    public DbSet<TaskDefinitionStateLink> TaskDefinitionStateLinks { get; set; }

    // === Resource Links ===
    public DbSet<TaskDefinitionResourceLink> TaskDefinitionResourceLinks { get; set; }

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
    }
}
