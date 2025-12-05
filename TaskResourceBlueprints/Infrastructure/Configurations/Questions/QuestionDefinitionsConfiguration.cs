using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskResourceBlueprints.Entities.Questions.Groups;
using TaskResourceBlueprints.Infrastructure.ConfigurationConstants;
using TaskResourceBlueprints.Infrastructure.Extensions;

namespace TaskResourceBlueprints.Infrastructure.Configurations.Questions;

public class QuestionGroupConfiguration : IEntityTypeConfiguration<QuestionGroupDefinition>
{
    public void Configure(EntityTypeBuilder<QuestionGroupDefinition> b)
    {
        b.HasMany(g => g.Options)
         .WithOne(o => o.QuestionGroup)
         .HasForeignKey(o => o.QuestionGroupId)
         .OnDelete(DeleteBehavior.Cascade);

        b.Property(x => x.SelectionMode).HasConversion<int>();

        b.Property(x => x.SectionKey).HasMaxLength(Lengths.SectionKey);
        b.Property(x => x.DisplayName).HasMaxLength(Lengths.DisplayName);

        b.HasIndex(g => new { g.TaskId, g.SortOrder });
        b.HasIndex(g => new { g.TaskId, g.SectionKey, g.SortOrder });
    }
}

public class ResourceSelectorConfiguration : IEntityTypeConfiguration<ResourceSelectorDefinition>
{
    public void Configure(EntityTypeBuilder<ResourceSelectorDefinition> b)
    {
        b.HasMany(g => g.Items)
         .WithOne(i => i.Selector)
         .HasForeignKey(i => i.SelectorId)
         .OnDelete(DeleteBehavior.Cascade);

        b.Property(x => x.SectionKey).HasMaxLength(Lengths.SectionKey);
        b.Property(x => x.DisplayName).HasMaxLength(Lengths.DisplayName);

        b.HasIndex(g => new { g.TaskId, g.SortOrder });
        b.HasIndex(g => new { g.TaskId, g.SectionKey, g.SortOrder });
    }
}

public class NumericQuestionConfiguration : IEntityTypeConfiguration<NumericQuestionDefinition>
{
    public void Configure(EntityTypeBuilder<NumericQuestionDefinition> b)
    {
        b.Property(x => x.SectionKey).HasMaxLength(Lengths.SectionKey);
        b.Property(x => x.DisplayName).HasMaxLength(Lengths.DisplayName);

        b.HasIndex(g => new { g.TaskId, g.SortOrder });
        b.HasIndex(g => new { g.TaskId, g.SectionKey, g.SortOrder });
    }
}

public class QuestionOptionConfiguration : IEntityTypeConfiguration<QuestionOptionDefinition>
{
    public void Configure(EntityTypeBuilder<QuestionOptionDefinition> b)
    {
        b.HasIndex(o => o.QuestionGroupId);

        b.Property(x => x.RevealedSectionKeys)
            .HasJsonListComparer();
    }
}

public class ResourceChoiceOptionConfiguration : IEntityTypeConfiguration<ResourceOptionItem>
{
    public void Configure(EntityTypeBuilder<ResourceOptionItem> b)
    {
        b.HasIndex(i => new { i.SelectorId, i.ResourceId });
    }
}
