using ProjectManagement.Shared.Helper.ML;
using System.ComponentModel.DataAnnotations;

namespace AuthPermissions.Entity;

public sealed class TenantMlSettingEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public TenantMlUsageMode UsageMode { get; set; } = TenantMlUsageMode.TenantWithGlobalFallback;
    public bool IncludeFeedbackInTraining { get; set; } = true;
    public bool AutoTrainingEnabled { get; set; }
    public int AutoTrainingIntervalDays { get; set; } = 14;
    public DateTime? NextTrainingAtUtc { get; set; }
    public DateTime? LastScheduledTrainingAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public TenantEntity? Tenant { get; set; }
}

public sealed class TenantMlTrainingRunEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public bool Success { get; set; }

    [MaxLength(4000)]
    public string ModelPath { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Message { get; set; } = string.Empty;

    public int PositiveExamples { get; set; }
    public int NegativeExamples { get; set; }
    public int TotalTasks { get; set; }
    public int TasksWithResources { get; set; }
    public int TasksWithoutResources { get; set; }
    public int TotalResources { get; set; }
    public int MissingUnitResources { get; set; }
    public int UnknownTypeResources { get; set; }
    public int FeedbackExamples { get; set; }
    public double? GlobalPositiveAverage { get; set; }
    public double? GlobalNegativeAverage { get; set; }
    public double? GlobalMargin { get; set; }
    public double? TenantPositiveAverage { get; set; }
    public double? TenantNegativeAverage { get; set; }
    public double? TenantMargin { get; set; }

    [MaxLength(50)]
    public string BetterModel { get; set; } = string.Empty;

    public string ResourceTypeBreakdownJson { get; set; } = "[]";
    public TenantEntity? Tenant { get; set; }
}
