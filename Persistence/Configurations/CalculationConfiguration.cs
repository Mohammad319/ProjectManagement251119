using Persistence.Serialization;

namespace Persistence.Configurations;

using Domain.Entities.Calculation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class CalculationConfiguration : IEntityTypeConfiguration<CalculationEntity>
{
    public void Configure(EntityTypeBuilder<CalculationEntity> builder)
    {
        builder.ToTable(tb =>
        {
            // DB guardrails (نفس Range الموجودة في الـ DTO/Domain)
            tb.HasCheckConstraint("CK_Calculations_Tax_Range", "[Tax] >= 0 AND [Tax] <= 100");
        });

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
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(x => x.Type)
            .WithMany(x => x.Calculations)
            .HasForeignKey(x => x.TypeId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(x => x.Compensation)
            .WithMany(x => x.Calculations)
            .HasForeignKey(x => x.CompensationId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(x => x.Contract)
            .WithMany(x => x.Calculations)
            .HasForeignKey(x => x.ContractId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(x => x.Status)
            .WithMany(x => x.Calculations)
            .HasForeignKey(x => x.StatusId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(x => x.Template)
            .WithMany(x => x.Calculations)
            .HasForeignKey(x => x.TemplateId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(x => x.Organisation)
            .WithMany(x => x.Calculations)
            .HasForeignKey(x => x.OrganisationId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        // -------------------------
        // Performance indexes
        // -------------------------

        // موجود عندك: ممتاز لشاشات (Project + Department + Order)
        builder.HasIndex(x => new { x.TenantId, x.ProjectId, x.DepartmentId, x.SortOrder })
            .HasDatabaseName("IX_Calculations_Tenant_Project_Department_Order");

        builder.HasIndex(x => new { x.TenantId, x.DepartmentId })
            .HasDatabaseName("IX_Calculations_Tenant_Department");

        builder.HasIndex(x => new { x.TenantId, x.Id, x.DepartmentId })
            .HasDatabaseName("IX_Calculations_Tenant_Id_Department");

        // ✅ إضافات مفيدة جدًا لـ OfferService:
        // فلترة كثيرة تكون على ProjectId فقط (بدون DepartmentId)
        builder.HasIndex(x => new { x.TenantId, x.ProjectId })
            .HasDatabaseName("IX_Calculations_Tenant_Project");

        // ✅ يمنع تكرار نفس كود الحساب داخل نفس المشروع والتينانت
        builder.HasIndex(x => new { x.TenantId, x.ProjectId, x.Code })
            .IsUnique()
            .HasDatabaseName("UX_Calculations_Tenant_Project_Code");

        // (اختياري) لو عندك فلترة كثيرة مباشرة على StatusId في قائمة الحسابات
        builder.HasIndex(x => new { x.TenantId, x.StatusId })
            .HasDatabaseName("IX_Calculations_Tenant_Status");
    }
}
