using Domain.Entities.Calculation;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Persistence.Serialization;

namespace Persistence.Configurations;

using Domain.Entities.Calculation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class CalculationConfiguration : IEntityTypeConfiguration<CalculationEntity>
{
    public void Configure(EntityTypeBuilder<CalculationEntity> builder)
    {
        // -------------------------
        // JSON conversions
        // -------------------------
        builder.Property(e => e.Metadata).HasJsonConversion();

        builder.Property(e => e.HourlyPrice).HasJsonConversion();
        builder.Property(e => e.Factors).HasJsonConversion();

        // -------------------------
        // Relationships
        // -------------------------
        builder.HasOne(x => x.ProcurementMethods)
            .WithMany(x => x.Calculations)
            .HasForeignKey(x => x.ProcurementMethodsId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Type)
            .WithMany(x => x.Calculations)
            .HasForeignKey(x => x.TypeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Compensation)
            .WithMany(x => x.Calculations)
            .HasForeignKey(x => x.CompensationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Contract)
            .WithMany(x => x.Calculations)
            .HasForeignKey(x => x.ContractId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Status)
            .WithMany(x => x.Calculations)
            .HasForeignKey(x => x.StatusId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Template)
            .WithMany(x => x.Calculations)
            .HasForeignKey(x => x.TemplateId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Organisation)
            .WithMany(x => x.Calculations)
            .HasForeignKey(x => x.OrganisationId)
            .OnDelete(DeleteBehavior.SetNull);

        // إن رغبت بإعادة علاقة Projects لاحقاً: خليها واضحة وبسلوك حذف مقصود.
        // builder.HasOne(x => x.Project)....

        // -------------------------
        // Performance indexes (SQL Server + Global Tenant filter)
        // -------------------------
        // أهم Index: يخدم GetAllAsync (Project + Department + Order) مع شرط TenantId الإجباري
        builder.HasIndex(x => new { x.TenantId, x.ProjectId, x.DepartmentId, x.SortOrder })
            .HasDatabaseName("IX_Calculations_Tenant_Project_Department_Order");

        // مفيد إذا في شاشات/استعلامات تعتمد على Department فقط (مع Tenant)
        builder.HasIndex(x => new { x.TenantId, x.DepartmentId })
            .HasDatabaseName("IX_Calculations_Tenant_Department");

        // مفيد للقراءة المباشرة/التحقق (Id + Department) مع Tenant
        // ملاحظة: Id غالباً PK ومفهرس أصلاً، لكن هذا يفيد إذا عندك استعلامات كثيرة تشمل DepartmentId أيضاً.
        builder.HasIndex(x => new { x.TenantId, x.Id, x.DepartmentId })
            .HasDatabaseName("IX_Calculations_Tenant_Id_Department");
    }
}

