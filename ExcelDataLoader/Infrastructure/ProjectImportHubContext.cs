using Microsoft.EntityFrameworkCore;
using ProjectImportHub.Entities;
using ProjectImportHub.Entities.Lookups;
using ProjectImportHub.Entities.Questions.Assignments;
using ProjectImportHub.Entities.Questions.Conditions;
using ProjectImportHub.Entities.Questions.Groups;
using ProjectImportHub.Entities.Resources;
using ProjectImportHub.Entities.Tasks;
using ProjectManagement.Shared.Base.AppTenant;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.App.Dataloader;

namespace ProjectImportHub.Infrastructure;

public class ProjectImportHubContext : DbContext
{
    public ProjectImportHubContext(DbContextOptions<ProjectImportHubContext> options)
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
    public DbSet<ResourceChoiceOptionDefinition> ResourceChoiceOptions { get; set; }
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
        modelBuilder.Ignore<ResourceData>();
        modelBuilder.Ignore<ExternalVariable>();
        modelBuilder.Ignore<Equation>();

        // Apply all IEntityTypeConfiguration<T> in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProjectImportHubContext).Assembly);
    }
}
