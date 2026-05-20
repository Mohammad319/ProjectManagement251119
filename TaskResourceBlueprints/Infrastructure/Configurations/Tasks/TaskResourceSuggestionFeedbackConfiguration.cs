using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskResourceBlueprints.Entities.Tasks;
using TaskResourceBlueprints.Infrastructure.ConfigurationConstants;

namespace TaskResourceBlueprints.Infrastructure.Configurations.Tasks;

public sealed class TaskResourceSuggestionFeedbackConfiguration : IEntityTypeConfiguration<TaskResourceSuggestionFeedback>
{
    public void Configure(EntityTypeBuilder<TaskResourceSuggestionFeedback> b)
    {
        b.Property(x => x.TargetTaskName)
            .HasMaxLength(Lengths.DisplayName);

        b.Property(x => x.TargetTaskCode)
            .HasMaxLength(Lengths.DisplayName);

        b.Property(x => x.TargetTaskUnit)
            .HasMaxLength(64);

        b.Property(x => x.SourceTaskName)
            .HasMaxLength(Lengths.DisplayName);

        b.Property(x => x.SourceTaskUnit)
            .HasMaxLength(64);

        b.Property(x => x.TargetTaskQuantity)
            .HasPrecision(18, 6);

        b.Property(x => x.SourceTaskQuantity)
            .HasPrecision(18, 6);

        b.Property(x => x.Reason)
            .HasMaxLength(512);

        b.HasIndex(x => new { x.TenantId, x.TargetTaskId, x.Source, x.SourceTaskId })
            .IsUnique();

        b.HasIndex(x => new { x.Source, x.SourceTaskId, x.Feedback });

        b.HasIndex(x => new { x.TenantId, x.ReviewStatus, x.UpdatedAtUtc });

        b.HasIndex(x => new { x.TenantId, x.TargetTaskId, x.CreatedAtUtc })
            .HasDatabaseName("IX_Feedback_Tenant_Task_Date");

        b.ToTable(t =>
        {
            t.HasCheckConstraint("CK_TaskResourceSuggestionFeedbacks_Tenant_Positive", "[TenantId] > 0");
            t.HasCheckConstraint("CK_TaskResourceSuggestionFeedbacks_TargetTask_Positive", "[TargetTaskId] > 0");
            t.HasCheckConstraint("CK_TaskResourceSuggestionFeedbacks_SourceTask_Positive", "[SourceTaskId] > 0");
            t.HasCheckConstraint("CK_TaskResourceSuggestionFeedbacks_Score_NonNegative", "[Score] >= 0");
            t.HasCheckConstraint("CK_TaskResourceSuggestionFeedbacks_TargetQuantity_NonNegative", "[TargetTaskQuantity] IS NULL OR [TargetTaskQuantity] >= 0");
            t.HasCheckConstraint("CK_TaskResourceSuggestionFeedbacks_SourceQuantity_NonNegative", "[SourceTaskQuantity] IS NULL OR [SourceTaskQuantity] >= 0");
        });
    }
}
