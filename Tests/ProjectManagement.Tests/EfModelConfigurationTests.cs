using AuthPermissions.Context;
using AuthPermissions.Entity;
using Domain.Entities.Base;
using Domain.Entities.Calculation;
using Domain.Entities.Project;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Persistence.Context;
using TaskResourceBlueprints.Entities.Resources;
using TaskResourceBlueprints.Entities.Tasks;
using TaskResourceBlueprints.Infrastructure;
using Xunit;

namespace ProjectManagement.Tests;

public sealed class EfModelConfigurationTests
{
    [Fact]
    public void Tenant_model_has_expected_filters_indexes_and_concurrency_tokens()
    {
        using var db = new ShardingSingleDbContext(
            new DbContextOptionsBuilder<ShardingSingleDbContext>()
                .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=ModelOnly;Trusted_Connection=True;TrustServerCertificate=True")
                .Options)
        {
            TenantId = 42
        };

        var model = db.GetService<IDesignTimeModel>().Model;
        var project = model.FindEntityType(typeof(ProjectEntity))!;
        var task = model.FindEntityType(typeof(TaskEntity))!;
        var resource = model.FindEntityType(typeof(ResourceEntity))!;
        var offer = model.FindEntityType(typeof(OfferEntity))!;
        var tender = model.FindEntityType(typeof(TenderEntity))!;
        var opportunity = model.FindEntityType(typeof(OpportunityEntity))!;
        var shareCalc = model.FindEntityType(typeof(ShareCalcEntity))!;
        var status = model.FindEntityType(typeof(StatusEntity))!;
        var taskStatus = model.FindEntityType(typeof(TaskStatusEntity))!;

        Assert.NotEmpty(project.GetDeclaredQueryFilters());
        Assert.NotEmpty(task.GetDeclaredQueryFilters());
        Assert.NotEmpty(resource.GetDeclaredQueryFilters());
        Assert.NotEmpty(offer.GetDeclaredQueryFilters());
        Assert.NotEmpty(tender.GetDeclaredQueryFilters());
        Assert.NotEmpty(opportunity.GetDeclaredQueryFilters());
        Assert.NotEmpty(shareCalc.GetDeclaredQueryFilters());
        Assert.True(project.FindProperty(nameof(AuditableEntity<int>.RowVersion))!.IsConcurrencyToken);
        Assert.Contains(status.GetIndexes(), i => i.IsUnique && HasProperties(i, "TenantId", "Name"));
        Assert.Contains(taskStatus.GetIndexes(), i => i.IsUnique && HasProperties(i, "TenantId", "Name"));
        Assert.Contains(task.GetCheckConstraints(), c => c.Name == "CK_Tasks_Quantity_NonNegative");
        Assert.Contains(resource.GetCheckConstraints(), c => c.Name == "CK_Resources_Quantity_NonNegative");
    }

    [Fact]
    public void Blueprint_model_has_tenant_filter_unique_link_index_and_numeric_guards()
    {
        using var db = new TaskResourceBlueprintsContext(
            new DbContextOptionsBuilder<TaskResourceBlueprintsContext>()
                .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=BlueprintModelOnly;Trusted_Connection=True;TrustServerCertificate=True")
                .Options)
        {
            TenantId = 42
        };

        var model = db.GetService<IDesignTimeModel>().Model;
        var tenantLink = model.FindEntityType(typeof(ResourceTenantLinkEntity))!;
        var task = model.FindEntityType(typeof(TaskDefinition))!;
        var resourceLink = model.FindEntityType(typeof(TaskDefinitionResourceLink))!;
        var resource = model.FindEntityType(typeof(TaskResourceBlueprints.Entities.ResourceDefinition))!;
        var feedback = model.FindEntityType(typeof(TaskResourceSuggestionFeedback))!;

        Assert.NotEmpty(tenantLink.GetDeclaredQueryFilters());
        Assert.NotEmpty(feedback.GetDeclaredQueryFilters());
        Assert.Contains(tenantLink.GetIndexes(), i => i.IsUnique && HasProperties(i, "TenantId", "ResourceId"));
        Assert.Contains(task.GetIndexes(), i => HasProperties(i, "Status", "UsageCount", "Code"));
        Assert.Contains(resource.GetIndexes(), i => HasProperties(i, "IsActive", "IsVisible", "Unit"));
        Assert.Contains(task.GetCheckConstraints(), c => c.Name == "CK_Tasks_ChangeFactors_Positive");
        Assert.Contains(resourceLink.GetCheckConstraints(), c => c.Name == "CK_TaskDefinitionResourceLinks_Quantity_Positive");
        Assert.Contains(feedback.GetCheckConstraints(), c => c.Name == "CK_TaskResourceSuggestionFeedbacks_Score_NonNegative");
    }

    [Fact]
    public void Auth_model_guards_catalog_tables()
    {
        using var db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=AuthModelOnly;Trusted_Connection=True;TrustServerCertificate=True")
                .Options);

        var model = db.GetService<IDesignTimeModel>().Model;
        var tenantDatabase = model.FindEntityType(typeof(TenantDatabaseEntity))!;
        var tenant = model.FindEntityType(typeof(TenantEntity))!;

        Assert.True(tenantDatabase.FindProperty(nameof(TenantDatabaseEntity.RowVersion))!.IsConcurrencyToken);
        Assert.Contains(tenantDatabase.GetIndexes(), i => i.IsUnique && HasProperties(i, "Name"));
        Assert.Contains(tenantDatabase.GetCheckConstraints(), c => c.Name == "CK_TenantDatabase_Name_NotEmpty");
        Assert.Contains(tenant.GetCheckConstraints(), c => c.Name == "CK_Tenants_MaxUsers_Positive");
    }

    private static bool HasProperties(Microsoft.EntityFrameworkCore.Metadata.IIndex index, params string[] names)
        => index.Properties.Select(p => p.Name).SequenceEqual(names);
}
