using Domain.Entities.Calculation;
using Domain.Entities.ResourceType;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Persistence.Serialization;
using ProjectManagement.Shared.DTO.ResourceType;

namespace Persistence.Configurations;

internal sealed class ResourceTypeConfiguration : IEntityTypeConfiguration<ResourceTypeEntity>
{
    public void Configure(EntityTypeBuilder<ResourceTypeEntity> builder)
    {
        builder.Property(e => e.Metadata)
            .HasJsonConversion();

        builder.HasOne(x => x.Account)
            .WithMany(x => x.ResourceTypes)
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}

internal sealed class ResourceSortConfiguration : IEntityTypeConfiguration<ResourceSortEntity>
{
    public void Configure(EntityTypeBuilder<ResourceSortEntity> builder)
    {
        builder.Property(e => e.Metadata)
            .HasJsonConversion();

        builder.HasOne(x => x.ResourceType)
            .WithMany(x => x.ResourcesSort)
            .HasForeignKey(x => x.ResourceTypeId)
            .HasPrincipalKey(x => x.Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Account)
            .WithMany(x => x.ResourceSorts)
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}

internal sealed class ResourceConfiguration : IEntityTypeConfiguration<ResourceEntity>
{
    public void Configure(EntityTypeBuilder<ResourceEntity> builder)
    {
        builder.ToTable("Resources");

        builder.HasIndex(x => new { x.TenantId, x.TaskId, x.SortOrder })
            .HasDatabaseName("IX_Resources_Tenant_Task_Sort");

        builder.HasIndex(x => new { x.TenantId, x.ResourceTypeId })
            .HasDatabaseName("IX_Resources_Tenant_ResourceType");

        builder.HasIndex(x => new { x.TenantId, x.ResourceSortId })
            .HasDatabaseName("IX_Resources_Tenant_ResourceSort");

        builder.HasIndex(x => new { x.TenantId, x.AccountId })
            .HasDatabaseName("IX_Resources_Tenant_Account");

        builder.HasIndex(x => new { x.TenantId, x.StatusId })
            .HasDatabaseName("IX_Resources_Tenant_Status");

        builder.HasIndex(x => new { x.TenantId, x.OpportunityId })
            .HasDatabaseName("IX_Resources_Tenant_Opportunity");

        builder.Property(e => e.Metadata)
            .HasJsonConversion();

        builder.HasOne(x => x.ResourceSort)
            .WithMany(x => x.Resources)
            .HasForeignKey(x => x.ResourceSortId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(x => x.ResourceType)
            .WithMany(x => x.Resources)
            .HasForeignKey(x => x.ResourceTypeId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(x => x.Account)
            .WithMany(x => x.Resources)
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(x => x.Status)
            .WithMany(x => x.Resources)
            .HasForeignKey(x => x.StatusId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(x => x.Opportunity)
            .WithMany(x => x.Resources)
            .HasForeignKey(x => x.OpportunityId)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}

internal sealed class TaskConfiguration : IEntityTypeConfiguration<TaskEntity>
{
    public void Configure(EntityTypeBuilder<TaskEntity> builder)
    {
        builder.ToTable("Tasks");

        builder.Property(e => e.Metadata)
            .HasJsonConversion();

        builder.HasOne(x => x.Status)
            .WithMany(x => x.Tasks)
            .HasForeignKey(x => x.StatusId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(x => x.Opportunity)
    .WithMany(x => x.Tasks)
    .HasForeignKey(x => x.OpportunityId)
    .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(x => x.ParentTask)
            .WithMany(x => x.Tasks)
            .HasForeignKey(x => x.ParentTaskId)
            .OnDelete(DeleteBehavior.ClientCascade);

        builder.HasOne(x => x.Calculation)
            .WithMany(c => c.Tasks)
            .HasForeignKey(x => x.CalculationId)
            .OnDelete(DeleteBehavior.Cascade);

        // ✅ مهم جدًا (شجرة + ترتيب داخل Calculation) + Tenant filter
        builder.HasIndex(x => new { x.TenantId, x.CalculationId, x.ParentTaskId, x.SortOrder })
            .HasDatabaseName("IX_Tasks_Tenant_Calc_Parent_Sort");

        // ✅ اختياري إذا لديك فلترة كثيرة على Status ضمن Calculation
        builder.HasIndex(x => new { x.TenantId, x.CalculationId, x.StatusId })
            .HasDatabaseName("IX_Tasks_Tenant_Calc_Status");
    }
}

internal sealed class OfferConfiguration : IEntityTypeConfiguration<OfferEntity>
{
    public void Configure(EntityTypeBuilder<OfferEntity> builder)
    {
        builder.ToTable("Offers");

        builder.HasOne(o => o.Resource)
            .WithMany(r => r.Offers)
            .HasForeignKey(o => o.ResourceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(o => o.Organisation)
            .WithMany(p => p.Offers)
            .HasForeignKey(o => o.OrganisationId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.Property(e => e.Metadata)
            .HasJsonConversion();

        // ✅ يخدم OfferService.GetByFilterAsync (Organisation filter + Date sort)
        builder.HasIndex(x => new { x.TenantId, x.OrganisationId, x.Date })
            .HasDatabaseName("IX_Offers_Tenant_Org_Date");

        // ✅ اختياري إذا عندك شاشة تعرض عروض مورد معيّن مرتبة بالتاريخ:
        // builder.HasIndex(x => new { x.TenantId, x.ResourceId, x.Date })
        //     .HasDatabaseName("IX_Offers_Tenant_Resource_Date");
    }
}
