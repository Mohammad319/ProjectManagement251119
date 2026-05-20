using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskResourceBlueprints.Entities.Lookups;
using TaskResourceBlueprints.Infrastructure.ConfigurationConstants;

namespace TaskResourceBlueprints.Infrastructure.Configurations.Tasks;

public class TaskStateGroupConfiguration : IEntityTypeConfiguration<TaskStateGroup>
{
    public void Configure(EntityTypeBuilder<TaskStateGroup> b)
    {
        b.Property(x => x.Name).HasMaxLength(Lengths.DisplayName);
        b.HasIndex(x => x.Name)
            .IsUnique()
            .HasDatabaseName("UX_TaskStateGroups_Name");

        b.HasMany(x => x.States)
         .WithOne(x => x.Group)
         .HasForeignKey(x => x.TaskStateGroupId)
         .OnDelete(DeleteBehavior.Cascade);

        b.ToTable(t =>
        {
            t.HasCheckConstraint("CK_TaskStateGroups_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
            t.HasCheckConstraint("CK_TaskStateGroups_SortOrder_NonNegative", "[SortOrder] >= 0");
        });
    }
}
