using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectImportHub.Entities;

namespace ProjectImportHub.Infrastructure.Configurations.Tasks;

public class TaskUnitGroupConfiguration : IEntityTypeConfiguration<TaskUnitGroup>
{
    public void Configure(EntityTypeBuilder<TaskUnitGroup> builder)
    {
        // لا يوجد شيء خاص الآن، لكن الملف جاهز للتوسعة مستقبلاً
    }
}
