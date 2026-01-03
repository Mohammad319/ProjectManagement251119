using Microsoft.EntityFrameworkCore;
using TaskResourceBlueprints.Entities;
using TaskResourceBlueprints.Entities.Lookups;
using TaskResourceBlueprints.Entities.Questions.Assignments;
using TaskResourceBlueprints.Entities.Questions.Conditions;
using TaskResourceBlueprints.Entities.Questions.Groups;
using TaskResourceBlueprints.Entities.Resources;
using TaskResourceBlueprints.Entities.Tasks;
using ProjectManagement.Shared.Base.AppTenant;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.App.Dataloader;

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
    public DbSet<ResourceAttributeValue> ResourceAttributeValues { get; set; }
    public DbSet<ResourceAttributeSet> ResourceAttributeSets { get; set; }
    public DbSet<ResourceAttribute> ResourceAttributes { get; set; }

    public DbSet<TaskDefinition> Tasks { get; set; }
    public DbSet<ResourceCategory> ResourceCategories { get; set; }
    public DbSet<TaskResourceAssignment> TaskResourceAssignments { get; set; }
    public DbSet<ActionEntity> Actions { get; set; }
    public DbSet<LocationEntity> Locations { get; set; }
    public DbSet<ActionTypeEntity> ActionTypes { get; set; }
    public DbSet<FallEntity> Falls { get; set; }

    // === Questions & Options ===
    public DbSet<QuestionGroupDefinition> QuestionGroups { get; set; }
    public DbSet<QuestionOptionDefinition> QuestionOptions { get; set; }
    public DbSet<ResourceSelectorDefinition> ResourceSelectors { get; set; }
    public DbSet<ResourceOptionItem> ResourceChoiceOptions { get; set; }
    public DbSet<NumericQuestionDefinition> NumericQuestions { get; set; }

    // === Conditions & Requirements ===
    public DbSet<ConditionDefinition> Conditions { get; set; }
    public DbSet<OptionConditionRule> OptionRequirements { get; set; }
    public DbSet<ResourceConditionRule> ResourceRequirements { get; set; }
    public DbSet<NumericConditionRule> NumericRequirements { get; set; }
    public DbSet<ConditionResourceAssignment> ConditionResourceAssignments { get; set; }

    // === Resource Assignments ===
    public DbSet<OptionResourceAssignment> OptionResourceAssignments { get; set; }
    public DbSet<NumericResourceAssignment> NumericResourceAssignments { get; set; }

    // === Tenant Links ===
    public DbSet<ResourceTenantLinkEntity> ResourceTenantLinks { get; set; }

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
