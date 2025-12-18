using Domain.Entities.Calculation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using System.Collections.Generic;
using System.Text.Json;
using System.Xml;

namespace Persistence.Configurations
{
    class CalculationConfiguration : IEntityTypeConfiguration<CalculationEntity>
    {
        public void Configure(EntityTypeBuilder<CalculationEntity> modelBuilder)
        {
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            modelBuilder.Property(e => e.Metadata).HasConversion(v => JsonSerializer.Serialize(v, jsonOptions),v => JsonSerializer.Deserialize<CalculationData>(v, jsonOptions) ?? new CalculationData());

            modelBuilder.Property(e => e.HourlyPriceFactorData).HasConversion(v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<CalculationHourlyPriceFactorData>(v, jsonOptions) ?? new CalculationHourlyPriceFactorData());

            modelBuilder.HasOne(pt => pt.ProcurementMethods).WithMany(p => p.Calculations).HasForeignKey(pt => pt.ProcurementMethodsId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.HasOne(pt => pt.Type).WithMany(p => p.Calculations).HasForeignKey(pt => pt.TypeId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.HasOne(pt => pt.Compensation).WithMany(p => p.Calculations).HasForeignKey(pt => pt.CompensationId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.HasOne(pt => pt.Contract).WithMany(p => p.Calculations).HasForeignKey(pt => pt.ContractId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.HasOne(pt => pt.Status).WithMany(p => p.Calculations).HasForeignKey(pt => pt.StatusId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.HasOne(pt => pt.Template).WithMany(p => p.Calculations).HasForeignKey(pt => pt.TemplateId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.HasOne(pt => pt.Organisation).WithMany(p => p.Calculations).HasForeignKey(pt => pt.OrganisationId).OnDelete(DeleteBehavior.SetNull);

            //modelBuilder.HasOne(pt => pt.Projects).WithMany(p => p.Calculations).HasForeignKey(pt => pt.ProjectId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
