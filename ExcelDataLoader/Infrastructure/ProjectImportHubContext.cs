using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ProjectImportHub.Entities;
using ProjectImportHub.Entities.Assignments;
using ProjectImportHub.Entities.Questions.Assignments;
using ProjectImportHub.Entities.Questions.Conditions;
using ProjectImportHub.Entities.Questions.Groups;
using ProjectManagement.Shared.Base.AppTenant;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.App.Dataloader;
using System.Text.Json;

namespace ProjectImportHub.Infrastructure
{
    public class ProjectImportHubContext(DbContextOptions<ProjectImportHubContext> options) : DbContext(options)
    {
        public DbSet<UnitGroupEntity> UnitGroups { get; set; }
        public DbSet<ResourceEntity> Resources { get; set; }
        public DbSet<ResourcePropertyBindEntity> ResourceProperty { get; set; }
        public DbSet<ResourcePropertySetEntity> ResourcePropertySets { get; set; }
        public DbSet<ResourcePropertyEntity> ResourceProperties { get; set; }

        public DbSet<ProjectTaskEntity> Tasks { get; set; }
        public DbSet<ResourceFolderEntity> Folders { get; set; } // بدل Folders
        public DbSet<TaskResourceAssignmentEntity> TaskResourceAssignments { get; set; }
        public DbSet<ActionEntity> Actions { get; set; }
        public DbSet<LocationEntity> Locations { get; set; }
        public DbSet<ActionTypeEntity> ActionTypes { get; set; }
        public DbSet<FallEntity> Falls { get; set; }

        // === Gruppen und Optionen ===
        public DbSet<OptionGroupEntity> OptionGroups { get; set; }
        public DbSet<OptionItemEntity> OptionItems { get; set; }
        public DbSet<ResourceOptionGroupEntity> ResourceOptionGroups { get; set; }
        public DbSet<ResourceOptionItemEntity> ResourceOptionItems { get; set; }
        public DbSet<NumericInputEntity> NumericInputGroups { get; set; }

        // === Bedingungen, Anforderungen, Ergebnisse ===
        public DbSet<TaskConditionEntity> QuestionConditions { get; set; }
        public DbSet<ConditionOptionRequirementEntity> ChoiceRequirements { get; set; }
        public DbSet<ConditionResourceRequirementEntity> ResourceRequirements { get; set; }
        public DbSet<ConditionNumericRequirementEntity> NumericRequirements { get; set; }
        public DbSet<ResourceAssignmentEntity> ConditionResourceAssignments { get; set; }

        // === Ressourcen-Zuordnungen ===
        public DbSet<OptionResourceAssignmentEntity> OptionResourceAssignments { get; set; }
        public DbSet<NumericResourceAssignmentEntity> NumericGroupResourceAssignments { get; set; }

        public DbSet<ResourceTenantLinkEntity> ResourceTenant { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            IgnoreNonEntities(modelBuilder);
            ConfigureJsonConversions(modelBuilder);
            ConfigureTasks(modelBuilder);
            ConfigureGroupsAndItems(modelBuilder);
            ConfigureConditions(modelBuilder);
            ConfigureRequirements(modelBuilder);
            ConfigureBindings(modelBuilder);
            ConfigureIndexes(modelBuilder);
        }

        // ————— helpers —————

        private static void IgnoreNonEntities(ModelBuilder modelBuilder)
        {
            modelBuilder.Ignore<RoleDTO>();
            modelBuilder.Ignore<ResourceData>();
            modelBuilder.Ignore<ExternalVariable>();
            modelBuilder.Ignore<Equation>();
        }

        private static void ConfigureJsonConversions(ModelBuilder modelBuilder)
        {
            // TaskResourceModel.CapRole
            modelBuilder.Entity<TaskResourceAssignmentEntity>().Property(e => e.CapRole).HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<List<RoleDTO>>(v, JsonSerializerOptions.Default) ?? new List<RoleDTO>())
                .Metadata.SetValueComparer(new ValueComparer<List<RoleDTO>>(
                    (c1, c2) => c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, e) => a ^ e.GetHashCode()),
                    c => c.ToList()));
            modelBuilder.Entity<ResourceEntity>().Property(e => e.CostRole).HasConversion(
    v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
    v => JsonSerializer.Deserialize<List<RoleDTO>>(v, JsonSerializerOptions.Default) ?? new List<RoleDTO>())
    .Metadata.SetValueComparer(new ValueComparer<List<RoleDTO>>(
        (c1, c2) => c1.SequenceEqual(c2),
        c => c.Aggregate(0, (a, e) => a ^ e.GetHashCode()),
        c => c.ToList()));


            // ResourceAssignment.CapRole
            modelBuilder.Entity<ResourceAssignmentEntity>().Property(e => e.CapRole).HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<List<RoleDTO>>(v, JsonSerializerOptions.Default) ?? new List<RoleDTO>())
                .Metadata.SetValueComparer(new ValueComparer<List<RoleDTO>>(
                    (c1, c2) => c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, e) => a ^ e.GetHashCode()),
                    c => c.ToList()));

            // ResourceEXModel.Data (كائن)
            modelBuilder.Entity<ResourceEntity>().Property(e => e.Data).HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<ResourceData>(v, JsonSerializerOptions.Default) ?? new ResourceData())
                .Metadata.SetValueComparer(new ValueComparer<ResourceData>(
                    (d1, d2) => JsonSerializer.Serialize(d1, JsonSerializerOptions.Default) == JsonSerializer.Serialize(d2, JsonSerializerOptions.Default),
                    d => JsonSerializer.Serialize(d, JsonSerializerOptions.Default).GetHashCode(),
                    d => JsonSerializer.Deserialize<ResourceData>(JsonSerializer.Serialize(d, JsonSerializerOptions.Default), JsonSerializerOptions.Default) ?? new ResourceData()));
            modelBuilder.Entity<ResourceEntity>().Property(e => e.CalcResCost).HasConversion(
    v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
    v => JsonSerializer.Deserialize<CalcResCost>(v, JsonSerializerOptions.Default) ?? new CalcResCost())
    .Metadata.SetValueComparer(new ValueComparer<CalcResCost>(
        (d1, d2) => JsonSerializer.Serialize(d1, JsonSerializerOptions.Default) == JsonSerializer.Serialize(d2, JsonSerializerOptions.Default),
        d => JsonSerializer.Serialize(d, JsonSerializerOptions.Default).GetHashCode(),
        d => JsonSerializer.Deserialize<CalcResCost>(JsonSerializer.Serialize(d, JsonSerializerOptions.Default), JsonSerializerOptions.Default) ?? new CalcResCost()));

            modelBuilder.Entity<OptionItemEntity>()
    .Property(x => x.RevealedSectionKeys)
    .HasConversion(
        v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
        v => JsonSerializer.Deserialize<List<string>>(v, JsonSerializerOptions.Default) ?? new List<string>()
    )
    .Metadata.SetValueComparer(
        new ValueComparer<List<string>>(
            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
            c => c == null ? 0 : c.Aggregate(0, (a, s) => HashCode.Combine(a, s == null ? 0 : s.GetHashCode())),
            c => c == null ? new List<string>() : c.ToList()
        )
    );
        }

        private static void ConfigureTasks(ModelBuilder modelBuilder)
        {
            // === Task → Groups ===
            modelBuilder.Entity<ProjectTaskEntity>()
                .HasMany(t => t.OptionGroups)
                .WithOne(g => g.Task)
                .HasForeignKey(g => g.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProjectTaskEntity>()
                .HasMany(t => t.ResourceOptionGroups)
                .WithOne(g => g.Task)
                .HasForeignKey(g => g.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProjectTaskEntity>()
                .HasMany(t => t.NumericInputs)
                .WithOne(g => g.Task)
                .HasForeignKey(g => g.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            // === Task → Conditions ===
            modelBuilder.Entity<ProjectTaskEntity>()
                .HasMany(t => t.Conditions)
                .WithOne(c => c.Task)
                .HasForeignKey(c => c.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        private static void ConfigureGroupsAndItems(ModelBuilder modelBuilder)
        {
            // ChoiceGroup → Options
            modelBuilder.Entity<OptionGroupEntity>()
                .HasMany(g => g.Options)
                .WithOne(o => o.OptionGroup)
                .HasForeignKey(o => o.OptionGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            // ResourceChoiceGroup → Items
            modelBuilder.Entity<ResourceOptionGroupEntity>()
                .HasMany(g => g.Items)
                .WithOne(i => i.ResourceOptionGroup)
                .HasForeignKey(i => i.ResourceChoiceGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            // SelectionMode enum كأعداد صحيحة
            modelBuilder.Entity<OptionGroupEntity>(b =>
            {
                b.Property(x => x.SelectionMode).HasConversion<int>();
                // حدود اختيارية للأسماء/المفاتيح
                b.Property(x => x.SectionKey).HasMaxLength(128);
                b.Property(x => x.DisplayName).HasMaxLength(256);
            });

            modelBuilder.Entity<ResourceOptionGroupEntity>(b =>
            {
                b.Property(x => x.SectionKey).HasMaxLength(128);
                b.Property(x => x.DisplayName).HasMaxLength(256);
            });

            modelBuilder.Entity<NumericInputEntity>(b =>
            {
                b.Property(x => x.SectionKey).HasMaxLength(128);
                b.Property(x => x.DisplayName).HasMaxLength(256);
            });
        }

        private static void ConfigureConditions(ModelBuilder modelBuilder)
        {
            // تخزين ConditionLogic كـ int
            modelBuilder.Entity<TaskConditionEntity>(b =>
            {
                b.Property(x => x.OptionToResourceLogic).HasConversion<int>();
                b.Property(x => x.OptionToNumericLogic).HasConversion<int>();
                b.Property(x => x.NumericToResourceLogic).HasConversion<int>();
            });
        }

        private static void ConfigureRequirements(ModelBuilder modelBuilder)
        {
            // Choice requirements
            modelBuilder.Entity<ConditionOptionRequirementEntity>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedOnAdd();

                b.HasOne(x => x.QuestionCondition)
                 .WithMany(c => c.OptionRequirements)
                 .HasForeignKey(x => x.QuestionConditionId)
                 .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(x => x.OptionGroup)
                 .WithMany()
                 .HasForeignKey(x => x.OptionGroupId)
                 .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(x => x.OptionItem)
                 .WithMany()
                 .HasForeignKey(x => x.OptionItemId)
                 .OnDelete(DeleteBehavior.Restrict);

                b.HasIndex(x => new { x.QuestionConditionId, x.OptionGroupId, x.OptionItemId, x.SetKey }).IsUnique();
            });

            // Resource requirements
            modelBuilder.Entity<ConditionResourceRequirementEntity>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedOnAdd();

                b.HasOne(x => x.QuestionCondition)
                 .WithMany(c => c.ResourceRequirements)
                 .HasForeignKey(x => x.QuestionConditionId)
                 .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(x => x.ResourceOptionGroup)
                 .WithMany()
                 .HasForeignKey(x => x.ResourceOptionGroupId)
                 .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(x => x.ResourceOptionItem)
                 .WithMany()
                 .HasForeignKey(x => x.ResourceOptionItemId)
                 .OnDelete(DeleteBehavior.Restrict);

                b.HasIndex(x => new { x.QuestionConditionId, x.ResourceOptionGroupId, x.ResourceOptionItemId, x.SetKey }).IsUnique();
            });

            // Numeric requirements
            modelBuilder.Entity<ConditionNumericRequirementEntity>(b =>
            {
                b.HasOne(x => x.QuestionCondition)
                 .WithMany(c => c.NumericRequirements)
                 .HasForeignKey(x => x.QuestionConditionId);

                b.HasOne(x => x.NumericInput)
                 .WithMany()
                 .HasForeignKey(x => x.NumericInputId)
                 .OnDelete(DeleteBehavior.Restrict);

                b.HasIndex(x => new { x.QuestionConditionId, x.SetKey });
            });
        }

        private static void ConfigureBindings(ModelBuilder modelBuilder)
        {
            // OptionResourceBinding
            modelBuilder.Entity<OptionResourceAssignmentEntity>(b =>
            {
                b.HasKey(x => x.Id);

                b.HasOne(x => x.ChoiceOption)
                 .WithMany(o => o.OptionResourceAssignments)
                 .HasForeignKey(x => x.ChoiceOptionId)
                 .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(x => x.ResourceAssignment)
                 .WithMany(r => r.OptionResourceFormulas)      // <-- كان WithMany()
                 .HasForeignKey(x => x.ResourceAssignmentId)
                 .OnDelete(DeleteBehavior.Restrict);

                b.HasIndex(x => new { x.ChoiceOptionId, x.ResourceAssignmentId }).IsUnique();

                b.Property(x => x.Formulas)
                 .HasConversion(
                     v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                     v => JsonSerializer.Deserialize<List<string>>(v, JsonSerializerOptions.Default) ?? new List<string>()
                 ).Metadata.SetValueComparer(
                    new ValueComparer<List<string>>(
                        (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                        c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v == null ? 0 : v.GetHashCode())),
                        c => c == null ? new List<string>() : c.ToList()
                    )
                );
            });

            // NumericGroupResourceBinding
            modelBuilder.Entity<NumericResourceAssignmentEntity>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedOnAdd();

                b.HasOne(x => x.Numeric)
                 .WithMany(g => g.ResourceAssignments)
                 .HasForeignKey(x => x.NumericId)
                 .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(x => x.ResourceAssignment)
                 .WithMany(r => r.NumericResourceFormulas)     // <-- كان WithMany()
                 .HasForeignKey(x => x.ResourceAssignmentId)
                 .OnDelete(DeleteBehavior.Restrict);

                b.HasIndex(x => new { x.NumericId, x.ResourceAssignmentId }).IsUnique();

                b.Property(x => x.Formulas)
                 .HasConversion(
                     v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                     v => JsonSerializer.Deserialize<List<string>>(v, JsonSerializerOptions.Default) ?? new List<string>()
                 ).Metadata.SetValueComparer(
                    new ValueComparer<List<string>>(
                        (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                        c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v == null ? 0 : v.GetHashCode())),
                        c => c == null ? new List<string>() : c.ToList()
                    )
                );
            });

            // ResourceAssignment — دقة أرقام اختيارية
            modelBuilder.Entity<ResourceAssignmentEntity>(b =>
            {
                b.Property(x => x.BaseCost).HasPrecision(18, 4);
                b.Property(x => x.ChangeFactor1).HasPrecision(18, 6);
                b.Property(x => x.ChangeFactor2).HasPrecision(18, 6);
                b.Property(x => x.CapWaste).HasPrecision(18, 6);
            });
        }

        private static void ConfigureIndexes(ModelBuilder modelBuilder)
        {
            // شيوع الاستعلام حسب الترتيب/المهمة
            modelBuilder.Entity<OptionGroupEntity>().HasIndex(g => new { g.TaskId, g.SortOrder });
            modelBuilder.Entity<ResourceOptionGroupEntity>().HasIndex(g => new { g.TaskId, g.SortOrder });
            modelBuilder.Entity<NumericInputEntity>().HasIndex(g => new { g.TaskId, g.SortOrder });

            // (اختياري) لو تستخدم SectionKey
            modelBuilder.Entity<OptionGroupEntity>().HasIndex(g => new { g.TaskId, g.SectionKey, g.SortOrder });
            modelBuilder.Entity<ResourceOptionGroupEntity>().HasIndex(g => new { g.TaskId, g.SectionKey, g.SortOrder });
            modelBuilder.Entity<NumericInputEntity>().HasIndex(g => new { g.TaskId, g.SectionKey, g.SortOrder });

            // علاقات فرعية
            modelBuilder.Entity<OptionItemEntity>().HasIndex(o => o.OptionGroupId);
            modelBuilder.Entity<ResourceOptionItemEntity>().HasIndex(i => new { i.ResourceChoiceGroupId, i.ResourceId });
        }
    }
}
