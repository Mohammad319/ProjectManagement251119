using Domain.Entities.Project;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

internal static class LookupChecks
{
    public const string HexColorCheck = "[Color] LIKE '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]'";
    public const string NonNegativeSortOrderCheck = "[SortOrder] >= 0";
}

internal sealed class StatusLookupConfiguration : IEntityTypeConfiguration<StatusEntity>
{
    public void Configure(EntityTypeBuilder<StatusEntity> builder)
    {
        builder.ToTable("Statuses");

        builder.HasIndex(x => new { x.TenantId, x.IsVisible, x.SortOrder })
            .HasDatabaseName("IX_Statuses_Tenant_Visible_Order");

        builder.HasIndex(x => new { x.TenantId, x.Name })
            .IsUnique()
            .HasDatabaseName("IX_Statuses_Tenant_Name");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Statuses_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
            t.HasCheckConstraint("CK_Statuses_Color_Hex", LookupChecks.HexColorCheck);
            t.HasCheckConstraint("CK_Statuses_SortOrder_NonNegative", LookupChecks.NonNegativeSortOrderCheck);
            t.HasCheckConstraint("CK_Statuses_BidResult_RequiresSubmitted", "[CountsAsSubmittedBid] = 1 OR ([CountsAsWonBid] = 0 AND [CountsAsLostBid] = 0)");
            t.HasCheckConstraint("CK_Statuses_BidResult_NotWonAndLost", "[CountsAsWonBid] = 0 OR [CountsAsLostBid] = 0");
        });
    }
}

internal sealed class ProjectStatusLookupConfiguration : IEntityTypeConfiguration<ProjectStatusEntity>
{
    public void Configure(EntityTypeBuilder<ProjectStatusEntity> builder)
    {
        builder.ToTable("ProjectStatuses");

        builder.HasIndex(x => new { x.TenantId, x.IsVisible, x.SortOrder })
            .HasDatabaseName("IX_ProjectStatuses_Tenant_Visible_Order");

        builder.HasIndex(x => new { x.TenantId, x.Name })
            .IsUnique()
            .HasDatabaseName("IX_ProjectStatuses_Tenant_Name");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_ProjectStatuses_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
            t.HasCheckConstraint("CK_ProjectStatuses_Color_Hex", LookupChecks.HexColorCheck);
            t.HasCheckConstraint("CK_ProjectStatuses_SortOrder_NonNegative", LookupChecks.NonNegativeSortOrderCheck);
            t.HasCheckConstraint("CK_ProjectStatuses_BidResult_RequiresSubmitted", "[CountsAsSubmittedBid] = 1 OR ([CountsAsWonBid] = 0 AND [CountsAsLostBid] = 0)");
            t.HasCheckConstraint("CK_ProjectStatuses_BidResult_NotWonAndLost", "[CountsAsWonBid] = 0 OR [CountsAsLostBid] = 0");
        });
    }
}

internal sealed class TypeLookupConfiguration : IEntityTypeConfiguration<TypeEntity>
{
    public void Configure(EntityTypeBuilder<TypeEntity> builder)
    {
        builder.ToTable("ProjectTypes");

        builder.HasIndex(x => new { x.TenantId, x.IsVisible, x.SortOrder })
            .HasDatabaseName("IX_ProjectTypes_Tenant_Visible_Order");

        builder.HasIndex(x => new { x.TenantId, x.Name })
            .IsUnique()
            .HasDatabaseName("IX_ProjectTypes_Tenant_Name");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_ProjectTypes_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
            t.HasCheckConstraint("CK_ProjectTypes_Color_Hex", LookupChecks.HexColorCheck);
            t.HasCheckConstraint("CK_ProjectTypes_SortOrder_NonNegative", LookupChecks.NonNegativeSortOrderCheck);
        });
    }
}

internal sealed class ContractLookupConfiguration : IEntityTypeConfiguration<ContractEntity>
{
    public void Configure(EntityTypeBuilder<ContractEntity> builder)
    {
        builder.ToTable("Contracts");

        builder.HasIndex(x => new { x.TenantId, x.IsVisible, x.SortOrder })
            .HasDatabaseName("IX_Contracts_Tenant_Visible_Order");

        builder.HasIndex(x => new { x.TenantId, x.Name })
            .IsUnique()
            .HasDatabaseName("IX_Contracts_Tenant_Name");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Contracts_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
            t.HasCheckConstraint("CK_Contracts_Color_Hex", LookupChecks.HexColorCheck);
            t.HasCheckConstraint("CK_Contracts_SortOrder_NonNegative", LookupChecks.NonNegativeSortOrderCheck);
        });
    }
}

internal sealed class CompensationLookupConfiguration : IEntityTypeConfiguration<CompensationEntity>
{
    public void Configure(EntityTypeBuilder<CompensationEntity> builder)
    {
        builder.ToTable("Compensations");

        builder.HasIndex(x => new { x.TenantId, x.IsVisible, x.SortOrder })
            .HasDatabaseName("IX_Compensations_Tenant_Visible_Order");

        builder.HasIndex(x => new { x.TenantId, x.Name })
            .IsUnique()
            .HasDatabaseName("IX_Compensations_Tenant_Name");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Compensations_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
            t.HasCheckConstraint("CK_Compensations_Color_Hex", LookupChecks.HexColorCheck);
            t.HasCheckConstraint("CK_Compensations_SortOrder_NonNegative", LookupChecks.NonNegativeSortOrderCheck);
        });
    }
}

internal sealed class ProcurementProcedureLookupConfiguration : IEntityTypeConfiguration<ProcurementProcedureEntity>
{
    public void Configure(EntityTypeBuilder<ProcurementProcedureEntity> builder)
    {
        builder.ToTable("ProcurementProcedures");

        builder.HasIndex(x => new { x.TenantId, x.IsVisible, x.SortOrder })
            .HasDatabaseName("IX_ProcurementProcedures_Tenant_Visible_Order");

        builder.HasIndex(x => new { x.TenantId, x.Name })
            .IsUnique()
            .HasDatabaseName("IX_ProcurementProcedures_Tenant_Name");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_ProcurementProcedures_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
            t.HasCheckConstraint("CK_ProcurementProcedures_Color_Hex", LookupChecks.HexColorCheck);
            t.HasCheckConstraint("CK_ProcurementProcedures_SortOrder_NonNegative", LookupChecks.NonNegativeSortOrderCheck);
        });
    }
}

internal sealed class ProcurementMethodLookupConfiguration : IEntityTypeConfiguration<ProcurementMethodEntity>
{
    public void Configure(EntityTypeBuilder<ProcurementMethodEntity> builder)
    {
        builder.ToTable("ProcurementMethods");

        builder.HasIndex(x => new { x.TenantId, x.IsVisible, x.SortOrder })
            .HasDatabaseName("IX_ProcurementMethods_Tenant_Visible_Order");

        builder.HasIndex(x => new { x.TenantId, x.Name })
            .IsUnique()
            .HasDatabaseName("IX_ProcurementMethods_Tenant_Name");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_ProcurementMethods_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
            t.HasCheckConstraint("CK_ProcurementMethods_Color_Hex", LookupChecks.HexColorCheck);
            t.HasCheckConstraint("CK_ProcurementMethods_SortOrder_NonNegative", LookupChecks.NonNegativeSortOrderCheck);
        });
    }
}
