using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectImportHub.Entities.Questions.Groups;
using ProjectImportHub.Infrastructure.ConfigurationConstants;
using ProjectImportHub.Infrastructure.Extensions;

namespace ProjectImportHub.Infrastructure.Configurations.Questions;

public class QuestionGroupConfiguration : IEntityTypeConfiguration<QuestionGroupDefinition>
{
    public void Configure(EntityTypeBuilder<QuestionGroupDefinition> b)
    {
        b.HasMany(g => g.Options)
         .WithOne(o => o.OptionGroup)
         .HasForeignKey(o => o.OptionGroupId)
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
         .WithOne(i => i.ResourceOptionGroup)
         .HasForeignKey(i => i.ResourceChoiceGroupId)
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
        b.HasIndex(o => o.OptionGroupId);

        b.Property(x => x.RevealedSectionKeys)
            .HasJsonListComparer();
    }
}

public class ResourceChoiceOptionConfiguration : IEntityTypeConfiguration<ResourceChoiceOptionDefinition>
{
    public void Configure(EntityTypeBuilder<ResourceChoiceOptionDefinition> b)
    {
        b.HasIndex(i => new { i.ResourceChoiceGroupId, i.ResourceId });
    }
}
