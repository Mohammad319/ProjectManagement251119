using Domain.Entities.Calculation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagement.Shared.Constant;
using Persistence.Serialization;

namespace Persistence.Configurations;

internal sealed class TemplateColumnConfiguration : IEntityTypeConfiguration<TemplateColumnEntity>
{
    public void Configure(EntityTypeBuilder<TemplateColumnEntity> builder)
    {
        builder.ToTable("TemplateColumns");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(FieldLengths.Name);

        builder.Property(x => x.IsVisible).IsRequired();
        builder.Property(x => x.DepartmentId);

        builder.Property(x => x.Columns)
            .HasJsonConversion()
            .IsRequired();

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasOne(x => x.Department)
            .WithMany()
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TenantId, x.DepartmentId, x.Id })
            .HasDatabaseName("IX_TemplateColumns_Tenant_Department_Id");

        builder.HasIndex(x => new { x.TenantId, x.DepartmentId, x.Name })
            .HasDatabaseName("IX_TemplateColumns_Tenant_Department_Name");

        builder.ToTable(t =>
            t.HasCheckConstraint("CK_TemplateColumns_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0")
        );
    }
}
