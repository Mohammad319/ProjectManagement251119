using Domain.Entities.Calculation;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Persistence.Serialization;
using ProjectManagement.Shared.DTO.Calculation;

namespace Persistence.Configurations;

internal sealed class CalculationConfiguration : IEntityTypeConfiguration<CalculationEntity>
{
    public void Configure(EntityTypeBuilder<CalculationEntity> builder)
    {
        builder.Property(e => e.Metadata).HasJsonConversion();

        builder.Property(e => e.HourlyPriceFactorData).HasJsonConversion();

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
    }
}
