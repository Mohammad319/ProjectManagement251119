using AuthPermissions.Context;
using AuthPermissions.Entity;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Persistence.Context;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.Enums;
using ProjectManagement.Shared.Helper.ML;
using ProjectManagement.Shared.Helper.Text;
using System.Text.Json;
using TaskResourceBlueprints.Infrastructure;

namespace ProjectManagement.Adminstrator.Services.TenantMl;

public interface ITenantMlTrainingService
{
    Task<IReadOnlyList<TenantMlTrainingTenantItem>> GetTenantsAsync(CancellationToken ct = default);
    Task<TenantMlTrainingResult> TrainTenantAsync(int tenantId, int negativeExamplesPerPositive = 2, CancellationToken ct = default);
    Task<TenantMlTrainingResult> AnalyzeTenantAsync(int tenantId, int negativeExamplesPerPositive = 2, CancellationToken ct = default);
    Task<TenantMlComparisonReport> EvaluateTenantAsync(int tenantId, int negativeExamplesPerPositive = 2, CancellationToken ct = default);
    Task<bool> RollbackTenantModelAsync(int tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<TenantMlFeedbackItem>> GetFeedbackAsync(int tenantId, CancellationToken ct = default);
    Task<bool> SetFeedbackReviewStatusAsync(int feedbackId, TaskResourceSuggestionFeedbackReviewStatus status, CancellationToken ct = default);
    Task<bool> DeleteFeedbackAsync(int feedbackId, CancellationToken ct = default);
    Task<IReadOnlyList<TenantMlDashboardItem>> GetDashboardAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TenantMlSuggestionPreviewItem>> PreviewSuggestionsAsync(TenantMlSuggestionPreviewRequest request, CancellationToken ct = default);
    Task<string> ExportQualityReportAsync(CancellationToken ct = default);
    Task<TenantMlSettingDto> UpdateSettingsAsync(int tenantId, TenantMlSettingDto settings, CancellationToken ct = default);
    Task<int> RunDueAutoTrainingAsync(CancellationToken ct = default);
}

public sealed class TenantMlTrainingTenantItem
{
    public int TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public bool HasModel { get; set; }
    public string ModelPath { get; set; } = string.Empty;
    public DateTimeOffset? ModelUpdatedAt { get; set; }
    public TenantMlSettingDto Settings { get; set; } = new();
    public TenantMlTrainingRunDto? LastRun { get; set; }
    public string? LatestBackupPath { get; set; }
    public DateTimeOffset? LatestBackupAt { get; set; }
}

public sealed class TenantMlSettingDto
{
    public TenantMlUsageMode UsageMode { get; set; } = TenantMlUsageMode.TenantWithGlobalFallback;
    public bool IncludeFeedbackInTraining { get; set; } = true;
    public bool AutoTrainingEnabled { get; set; }
    public int AutoTrainingIntervalDays { get; set; } = 14;
    public DateTime? NextTrainingAtUtc { get; set; }
}

public sealed class TenantMlFeedbackItem
{
    public int Id { get; set; }
    public string TargetTaskName { get; set; } = string.Empty;
    public string SourceTaskName { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Feedback { get; set; } = string.Empty;
    public TaskResourceSuggestionFeedbackReviewStatus ReviewStatus { get; set; }
    public double Score { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public string? Reason { get; set; }
}

public sealed class TenantMlDashboardItem
{
    public int TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string UsageMode { get; set; } = string.Empty;
    public bool HasModel { get; set; }
    public bool HasBackup { get; set; }
    public bool AutoTrainingEnabled { get; set; }
    public DateTime? LastTrainingAtUtc { get; set; }
    public string LastQuality { get; set; } = string.Empty;
    public int PendingFeedback { get; set; }
    public int ApprovedFeedback { get; set; }
}

public sealed class TenantMlSuggestionPreviewRequest
{
    public int TenantId { get; set; }
    public string TaskName { get; set; } = string.Empty;
    public string? TaskCode { get; set; }
    public string? TaskUnit { get; set; }
    public decimal? TaskQuantity { get; set; }
    public int MaxResults { get; set; } = 10;
}

public sealed class TenantMlSuggestionPreviewItem
{
    public string Source { get; set; } = string.Empty;
    public string TaskName { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public double Score { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class TenantMlDueTrainingItem
{
    public int TenantId { get; set; }
    public int IntervalDays { get; set; }
}

public sealed class TenantMlTrainingRunDto
{
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int PositiveExamples { get; set; }
    public int NegativeExamples { get; set; }
    public int FeedbackExamples { get; set; }
    public TenantMlQualityReport Quality { get; set; } = new();
    public TenantMlComparisonReport Comparison { get; set; } = new();
}

public sealed class TenantMlTrainingResult
{
    public bool Trained { get; set; }
    public int TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string ModelPath { get; set; } = string.Empty;
    public int PositiveExamples { get; set; }
    public int NegativeExamples { get; set; }
    public int FeedbackExamples { get; set; }
    public int TotalExamples => PositiveExamples + NegativeExamples;
    public string Message { get; set; } = string.Empty;
    public TenantMlQualityReport Quality { get; set; } = new();
    public TenantMlComparisonReport Comparison { get; set; } = new();
}

public sealed class TenantMlQualityReport
{
    public int TotalTasks { get; set; }
    public int TasksWithResources { get; set; }
    public int TasksWithoutResources { get; set; }
    public int TotalResources { get; set; }
    public int MissingUnitResources { get; set; }
    public int UnknownTypeResources { get; set; }
    public List<TenantMlResourceTypeCount> ResourceTypes { get; set; } = [];
    public List<TenantMlTaskClusterItem> TaskClusters { get; set; } = [];
    public List<TenantMlMissingTokenItem> MissingTokens { get; set; } = [];

    public double TasksWithResourcesRatio => TotalTasks <= 0 ? 0d : Math.Round((double)TasksWithResources / TotalTasks, 4);

    public string QualityLabel
    {
        get
        {
            if (TasksWithResources < 50 || TotalResources < 100)
                return "Weak";
            if (TasksWithResourcesRatio < 0.35d || MissingUnitResources > TotalResources * 0.35d)
                return "Needs review";
            if (TasksWithResources >= 500 && TasksWithResourcesRatio >= 0.60d)
                return "Strong";
            return "Good";
        }
    }
}

public sealed class TenantMlResourceTypeCount
{
    public string ResourceType { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class TenantMlTaskClusterItem
{
    public string Cluster { get; set; } = string.Empty;
    public int TotalTasks { get; set; }
    public int TasksWithResources { get; set; }
    public List<string> Examples { get; set; } = [];
}

public sealed class TenantMlMissingTokenItem
{
    public string Token { get; set; } = string.Empty;
    public int Count { get; set; }
    public List<string> Examples { get; set; } = [];
}

public sealed class TenantMlComparisonReport
{
    public bool HasGlobalModel { get; set; }
    public bool HasTenantModel { get; set; }
    public double? GlobalPositiveAverage { get; set; }
    public double? GlobalNegativeAverage { get; set; }
    public double? GlobalMargin { get; set; }
    public double? TenantPositiveAverage { get; set; }
    public double? TenantNegativeAverage { get; set; }
    public double? TenantMargin { get; set; }
    public string BetterModel { get; set; } = string.Empty;
    public List<TenantMlEvaluationExample> BestExamples { get; set; } = [];
    public List<TenantMlEvaluationExample> WorstExamples { get; set; } = [];
}

public sealed class TenantMlEvaluationExample
{
    public string TaskName { get; set; } = string.Empty;
    public string ResourceName { get; set; } = string.Empty;
    public bool ExpectedGood { get; set; }
    public double Score { get; set; }
    public string Source { get; set; } = string.Empty;
}

public sealed class TenantMlTrainingService(
    IDbContextFactory<AuthPermissionDbContext> authDbFactory,
    IDbContextFactory<TaskResourceBlueprintsContext> blueprintDbFactory)
    : ITenantMlTrainingService
{
    public async Task<IReadOnlyList<TenantMlTrainingTenantItem>> GetTenantsAsync(CancellationToken ct = default)
    {
        await using var authDb = await authDbFactory.CreateDbContextAsync(ct);

        var tenants = await authDb.Tenants
            .AsNoTracking()
            .Include(x => x.TenantDB)
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Name,
                DatabaseName = x.TenantDB == null ? string.Empty : x.TenantDB.Name
            })
            .ToListAsync(ct);

        var tenantIds = tenants.Select(x => x.Id).ToList();
        var settings = await authDb.TenantMlSettings
            .AsNoTracking()
            .Where(x => tenantIds.Contains(x.TenantId))
            .ToDictionaryAsync(x => x.TenantId, ct);

        var lastRuns = await authDb.TenantMlTrainingRuns
            .AsNoTracking()
            .Where(x => tenantIds.Contains(x.TenantId))
            .GroupBy(x => x.TenantId)
            .Select(x => x.OrderByDescending(r => r.StartedAtUtc).First())
            .ToDictionaryAsync(x => x.TenantId, ct);

        return tenants.Select(x =>
        {
            var path = TaskResourceSuggestionMlModelPath.GetTenantModelPath(x.Id);
            var file = File.Exists(path) ? new FileInfo(path) : null;
            settings.TryGetValue(x.Id, out var setting);
            lastRuns.TryGetValue(x.Id, out var run);

            var dtoSetting = new TenantMlSettingDto
            {
                UsageMode = setting?.UsageMode ?? TenantMlUsageMode.TenantWithGlobalFallback,
                IncludeFeedbackInTraining = setting?.IncludeFeedbackInTraining ?? true,
                AutoTrainingEnabled = setting?.AutoTrainingEnabled ?? false,
                AutoTrainingIntervalDays = setting?.AutoTrainingIntervalDays is > 0 ? setting.AutoTrainingIntervalDays : 14,
                NextTrainingAtUtc = setting?.NextTrainingAtUtc
            };

            TenantMlModelConfigStore.Write(new TenantMlModelConfig
            {
                TenantId = x.Id,
                UsageMode = dtoSetting.UsageMode,
                IncludeFeedbackInTraining = dtoSetting.IncludeFeedbackInTraining,
                AutoTrainingEnabled = dtoSetting.AutoTrainingEnabled,
                AutoTrainingIntervalDays = dtoSetting.AutoTrainingIntervalDays,
                NextTrainingAtUtc = dtoSetting.NextTrainingAtUtc
            });
            var latestBackup = GetLatestBackupFile(x.Id);

            return new TenantMlTrainingTenantItem
            {
                TenantId = x.Id,
                TenantName = x.Name,
                DatabaseName = x.DatabaseName,
                HasModel = file is not null,
                ModelPath = path,
                ModelUpdatedAt = file is null ? null : file.LastWriteTimeUtc,
                Settings = dtoSetting,
                LastRun = run is null ? null : ToRunDto(run),
                LatestBackupPath = latestBackup?.FullName,
                LatestBackupAt = latestBackup?.LastWriteTimeUtc
            };
        }).ToList();
    }

    public async Task<TenantMlSettingDto> UpdateSettingsAsync(
        int tenantId,
        TenantMlSettingDto settings,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        await using var authDb = await authDbFactory.CreateDbContextAsync(ct);
        var tenantExists = await authDb.Tenants.AnyAsync(x => x.Id == tenantId, ct);
        if (!tenantExists)
            throw new InvalidOperationException("Tenant was not found.");

        var setting = await authDb.TenantMlSettings.FirstOrDefaultAsync(x => x.TenantId == tenantId, ct);
        if (setting is null)
        {
            setting = new TenantMlSettingEntity { TenantId = tenantId };
            authDb.TenantMlSettings.Add(setting);
        }

        setting.UsageMode = settings.UsageMode;
        setting.IncludeFeedbackInTraining = settings.IncludeFeedbackInTraining;
        setting.AutoTrainingEnabled = settings.AutoTrainingEnabled;
        setting.AutoTrainingIntervalDays = Math.Clamp(settings.AutoTrainingIntervalDays, 1, 365);
        setting.NextTrainingAtUtc = settings.AutoTrainingEnabled
            ? settings.NextTrainingAtUtc ?? DateTime.UtcNow.AddDays(setting.AutoTrainingIntervalDays)
            : null;
        setting.UpdatedAtUtc = DateTime.UtcNow;
        await authDb.SaveChangesAsync(ct);

        TenantMlModelConfigStore.Write(new TenantMlModelConfig
        {
            TenantId = tenantId,
            UsageMode = setting.UsageMode,
            IncludeFeedbackInTraining = setting.IncludeFeedbackInTraining,
            AutoTrainingEnabled = setting.AutoTrainingEnabled,
            AutoTrainingIntervalDays = setting.AutoTrainingIntervalDays,
            NextTrainingAtUtc = setting.NextTrainingAtUtc
        });

        return new TenantMlSettingDto
        {
            UsageMode = setting.UsageMode,
            IncludeFeedbackInTraining = setting.IncludeFeedbackInTraining,
            AutoTrainingEnabled = setting.AutoTrainingEnabled,
            AutoTrainingIntervalDays = setting.AutoTrainingIntervalDays,
            NextTrainingAtUtc = setting.NextTrainingAtUtc
        };
    }

    public async Task<TenantMlTrainingResult> TrainTenantAsync(
        int tenantId,
        int negativeExamplesPerPositive = 2,
        CancellationToken ct = default)
    {
        negativeExamplesPerPositive = Math.Clamp(negativeExamplesPerPositive, 0, 10);

        await using var authDb = await authDbFactory.CreateDbContextAsync(ct);
        var tenant = await authDb.Tenants
            .AsNoTracking()
            .Include(x => x.TenantDB)
            .FirstOrDefaultAsync(x => x.Id == tenantId, ct);

        var setting = await authDb.TenantMlSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, ct);
        var includeFeedback = setting?.IncludeFeedbackInTraining ?? true;
        var usageMode = setting?.UsageMode ?? TenantMlUsageMode.TenantWithGlobalFallback;

        if (tenant?.TenantDB is null || string.IsNullOrWhiteSpace(tenant.TenantDB.ConnectionString))
            return new TenantMlTrainingResult
            {
                TenantId = tenantId,
                Message = "Tenant database connection was not found."
            };

        TenantMlModelConfigStore.Write(new TenantMlModelConfig
        {
            TenantId = tenantId,
            UsageMode = usageMode,
            IncludeFeedbackInTraining = includeFeedback,
            AutoTrainingEnabled = setting?.AutoTrainingEnabled ?? false,
            AutoTrainingIntervalDays = setting?.AutoTrainingIntervalDays is > 0 ? setting.AutoTrainingIntervalDays : 14,
            NextTrainingAtUtc = setting?.NextTrainingAtUtc
        });

        var modelPath = TaskResourceSuggestionMlModelPath.GetTenantModelPath(tenantId);
        var result = new TenantMlTrainingResult
        {
            TenantId = tenantId,
            TenantName = tenant.Name,
            ModelPath = modelPath
        };

        var startedAtUtc = DateTime.UtcNow;
        var examples = await BuildTenantExamplesAsync(
            tenantId,
            tenant.TenantDB.ConnectionString,
            negativeExamplesPerPositive,
            includeFeedback,
            result,
            ct);

        if (result.PositiveExamples == 0 || result.NegativeExamples == 0)
        {
            result.Message = "Training skipped because both positive and negative tenant examples are required.";
            await SaveRunAsync(authDbFactory, tenantId, startedAtUtc, result, success: false, ct);
            return result;
        }

        var ml = new MLContext(seed: 251119);
        var data = ml.Data.LoadFromEnumerable(examples);
        var pipeline = ml.Transforms.Concatenate(
                "Features",
                nameof(ModelInput.HeuristicScore),
                nameof(ModelInput.TextScore),
                nameof(ModelInput.NameScore),
                nameof(ModelInput.QuantitySimilarity),
                nameof(ModelInput.HasQuantitySimilarity),
                nameof(ModelInput.UnitBonus),
                nameof(ModelInput.CodeBonus),
                nameof(ModelInput.HasEquivalentUnits),
                nameof(ModelInput.HasCompatibleUnits),
                nameof(ModelInput.IsBlueprintSource))
            .Append(ml.Regression.Trainers.Sdca(
                labelColumnName: nameof(ModelInput.Label),
                featureColumnName: "Features"));

        var model = pipeline.Fit(data);
        var directory = Path.GetDirectoryName(modelPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        BackupCurrentModel(tenantId);
        await using (var modelStream = File.Create(modelPath))
        {
            ml.Model.Save(model, data.Schema, modelStream);
        }

        result.Comparison = CompareModels(examples, modelPath);
        result.Trained = true;
        result.Message = $"Tenant ML model trained from {result.TotalExamples} examples.";
        await SaveRunAsync(authDbFactory, tenantId, startedAtUtc, result, success: true, ct);
        return result;
    }

    public async Task<TenantMlTrainingResult> AnalyzeTenantAsync(
        int tenantId,
        int negativeExamplesPerPositive = 2,
        CancellationToken ct = default)
    {
        var tenant = await GetTenantWithDatabaseAsync(tenantId, ct);
        if (tenant?.TenantDB is null || string.IsNullOrWhiteSpace(tenant.TenantDB.ConnectionString))
            return new TenantMlTrainingResult
            {
                TenantId = tenantId,
                Message = "Tenant database connection was not found."
            };

        var setting = await GetSettingAsync(tenantId, ct);
        var result = new TenantMlTrainingResult
        {
            TenantId = tenantId,
            TenantName = tenant.Name,
            ModelPath = TaskResourceSuggestionMlModelPath.GetTenantModelPath(tenantId)
        };

        await BuildTenantExamplesAsync(
            tenantId,
            tenant.TenantDB.ConnectionString,
            Math.Clamp(negativeExamplesPerPositive, 0, 10),
            setting?.IncludeFeedbackInTraining ?? true,
            result,
            ct);

        result.Message = "Analysis completed without training.";
        return result;
    }

    public async Task<TenantMlComparisonReport> EvaluateTenantAsync(
        int tenantId,
        int negativeExamplesPerPositive = 2,
        CancellationToken ct = default)
    {
        var tenant = await GetTenantWithDatabaseAsync(tenantId, ct);
        if (tenant?.TenantDB is null || string.IsNullOrWhiteSpace(tenant.TenantDB.ConnectionString))
            return new TenantMlComparisonReport { BetterModel = "Tenant database connection was not found" };

        var setting = await GetSettingAsync(tenantId, ct);
        var result = new TenantMlTrainingResult
        {
            TenantId = tenantId,
            TenantName = tenant.Name,
            ModelPath = TaskResourceSuggestionMlModelPath.GetTenantModelPath(tenantId)
        };
        var examples = await BuildTenantExamplesAsync(
            tenantId,
            tenant.TenantDB.ConnectionString,
            Math.Clamp(negativeExamplesPerPositive, 0, 10),
            setting?.IncludeFeedbackInTraining ?? true,
            result,
            ct);

        return CompareModels(examples, result.ModelPath);
    }

    public Task<bool> RollbackTenantModelAsync(int tenantId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var backup = GetLatestBackupFile(tenantId);
        if (backup is null)
            return Task.FromResult(false);

        var modelPath = TaskResourceSuggestionMlModelPath.GetTenantModelPath(tenantId);
        var directory = Path.GetDirectoryName(modelPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        if (File.Exists(modelPath))
            BackupCurrentModel(tenantId);

        File.Copy(backup.FullName, modelPath, overwrite: true);
        return Task.FromResult(true);
    }

    public async Task<IReadOnlyList<TenantMlFeedbackItem>> GetFeedbackAsync(int tenantId, CancellationToken ct = default)
    {
        await using var db = await blueprintDbFactory.CreateDbContextAsync(ct);
        return await db.TaskResourceSuggestionFeedbacks
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.UpdatedAtUtc)
            .Take(200)
            .Select(x => new TenantMlFeedbackItem
            {
                Id = x.Id,
                TargetTaskName = x.TargetTaskName,
                SourceTaskName = x.SourceTaskName,
                Source = x.Source.ToString(),
                Feedback = x.Feedback.ToString(),
                ReviewStatus = x.ReviewStatus,
                Score = x.Score,
                Reason = x.Reason,
                CreatedAtUtc = x.CreatedAtUtc,
                UpdatedAtUtc = x.UpdatedAtUtc
            })
            .ToListAsync(ct);
    }

    public async Task<bool> SetFeedbackReviewStatusAsync(
        int feedbackId,
        TaskResourceSuggestionFeedbackReviewStatus status,
        CancellationToken ct = default)
    {
        await using var db = await blueprintDbFactory.CreateDbContextAsync(ct);
        var feedback = await db.TaskResourceSuggestionFeedbacks.FirstOrDefaultAsync(x => x.Id == feedbackId, ct);
        if (feedback is null)
            return false;

        feedback.ReviewStatus = status;
        feedback.ReviewedAtUtc = DateTime.UtcNow;
        feedback.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteFeedbackAsync(int feedbackId, CancellationToken ct = default)
    {
        await using var db = await blueprintDbFactory.CreateDbContextAsync(ct);
        var feedback = await db.TaskResourceSuggestionFeedbacks.FirstOrDefaultAsync(x => x.Id == feedbackId, ct);
        if (feedback is null)
            return false;

        db.TaskResourceSuggestionFeedbacks.Remove(feedback);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<TenantMlDashboardItem>> GetDashboardAsync(CancellationToken ct = default)
    {
        var tenants = await GetTenantsAsync(ct);
        await using var feedbackDb = await blueprintDbFactory.CreateDbContextAsync(ct);
        var tenantIds = tenants.Select(x => x.TenantId).ToList();
        var feedbackCounts = await feedbackDb.TaskResourceSuggestionFeedbacks
            .AsNoTracking()
            .Where(x => tenantIds.Contains(x.TenantId))
            .GroupBy(x => x.TenantId)
            .Select(x => new
            {
                TenantId = x.Key,
                Pending = x.Count(f => f.ReviewStatus == TaskResourceSuggestionFeedbackReviewStatus.Pending),
                Approved = x.Count(f => f.ReviewStatus == TaskResourceSuggestionFeedbackReviewStatus.Approved)
            })
            .ToDictionaryAsync(x => x.TenantId, ct);

        return tenants.Select(x =>
        {
            feedbackCounts.TryGetValue(x.TenantId, out var feedback);
            return new TenantMlDashboardItem
            {
                TenantId = x.TenantId,
                TenantName = x.TenantName,
                UsageMode = x.Settings.UsageMode.ToString(),
                HasModel = x.HasModel,
                HasBackup = !string.IsNullOrWhiteSpace(x.LatestBackupPath),
                AutoTrainingEnabled = x.Settings.AutoTrainingEnabled,
                LastTrainingAtUtc = x.LastRun?.CompletedAtUtc ?? x.LastRun?.StartedAtUtc,
                LastQuality = x.LastRun?.Quality.QualityLabel ?? "No training",
                PendingFeedback = feedback?.Pending ?? 0,
                ApprovedFeedback = feedback?.Approved ?? 0
            };
        }).ToList();
    }

    public async Task<IReadOnlyList<TenantMlSuggestionPreviewItem>> PreviewSuggestionsAsync(
        TenantMlSuggestionPreviewRequest request,
        CancellationToken ct = default)
    {
        if (request.TenantId <= 0 || string.IsNullOrWhiteSpace(request.TaskName))
            return [];

        var tenant = await GetTenantWithDatabaseAsync(request.TenantId, ct);
        if (tenant?.TenantDB is null || string.IsNullOrWhiteSpace(tenant.TenantDB.ConnectionString))
            return [];

        var maxResults = Math.Clamp(request.MaxResults, 1, 30);
        var targetText = BuildTaskText(request.TaskName, request.TaskCode, request.TaskUnit, request.TaskQuantity);
        var targetNormalized = SwedishTaskTextNormalizer.Normalize(targetText);
        var targetNameNormalized = SwedishTaskTextNormalizer.Normalize(request.TaskName);
        var targetCodeDepth = GetCodeDepth(request.TaskCode);

        var tenantEngine = CreatePredictionEngine(TaskResourceSuggestionMlModelPath.GetTenantModelPath(request.TenantId));
        var globalEngine = CreatePredictionEngine(TaskResourceSuggestionMlModelPath.GetDefaultModelPath());
        var usageMode = TenantMlModelConfigStore.ReadUsageMode(request.TenantId);

        var candidates = new List<TenantMlSuggestionPreviewItem>();
        candidates.AddRange(await PreviewTenantSuggestionsAsync(
            request,
            tenant.TenantDB.ConnectionString,
            targetNormalized,
            targetNameNormalized,
            targetCodeDepth,
            tenantEngine,
            globalEngine,
            usageMode,
            ct));
        candidates.AddRange(await PreviewBlueprintSuggestionsAsync(
            request,
            targetNormalized,
            targetNameNormalized,
            targetCodeDepth,
            tenantEngine,
            globalEngine,
            usageMode,
            ct));

        return candidates
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.TaskName)
            .Take(maxResults)
            .ToList();
    }

    public async Task<string> ExportQualityReportAsync(CancellationToken ct = default)
    {
        var rows = await GetDashboardAsync(ct);
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "ProjectManagement",
            "ML",
            "reports");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"tenant-ml-quality-{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx");

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Tenant ML");
        var headers = new[]
        {
            "TenantId",
            "TenantName",
            "UsageMode",
            "HasModel",
            "HasBackup",
            "AutoTraining",
            "LastTrainingUtc",
            "LastQuality",
            "PendingFeedback",
            "ApprovedFeedback"
        };

        for (var i = 0; i < headers.Length; i++)
            sheet.Cell(1, i + 1).Value = headers[i];

        var rowIndex = 2;
        foreach (var row in rows)
        {
            sheet.Cell(rowIndex, 1).Value = row.TenantId;
            sheet.Cell(rowIndex, 2).Value = row.TenantName;
            sheet.Cell(rowIndex, 3).Value = row.UsageMode;
            sheet.Cell(rowIndex, 4).Value = row.HasModel;
            sheet.Cell(rowIndex, 5).Value = row.HasBackup;
            sheet.Cell(rowIndex, 6).Value = row.AutoTrainingEnabled;
            sheet.Cell(rowIndex, 7).Value = row.LastTrainingAtUtc;
            sheet.Cell(rowIndex, 8).Value = row.LastQuality;
            sheet.Cell(rowIndex, 9).Value = row.PendingFeedback;
            sheet.Cell(rowIndex, 10).Value = row.ApprovedFeedback;
            rowIndex++;
        }

        sheet.Columns().AdjustToContents();
        workbook.SaveAs(path);
        return path;
    }

    public async Task<int> RunDueAutoTrainingAsync(CancellationToken ct = default)
    {
        await using var authDb = await authDbFactory.CreateDbContextAsync(ct);
        var now = DateTime.UtcNow;
        var dueSettings = await authDb.TenantMlSettings
            .Where(x =>
                x.AutoTrainingEnabled &&
                (x.NextTrainingAtUtc == null || x.NextTrainingAtUtc <= now))
            .OrderBy(x => x.NextTrainingAtUtc)
            .Take(5)
            .ToListAsync(ct);

        var trained = 0;
        foreach (var setting in dueSettings)
        {
            ct.ThrowIfCancellationRequested();
            await TrainTenantAsync(setting.TenantId, negativeExamplesPerPositive: 2, ct);

            setting.LastScheduledTrainingAtUtc = now;
            setting.NextTrainingAtUtc = now.AddDays(Math.Clamp(setting.AutoTrainingIntervalDays, 1, 365));
            setting.UpdatedAtUtc = now;
            trained++;
        }

        await authDb.SaveChangesAsync(ct);
        return trained;
    }

    private async Task<List<ModelInput>> BuildTenantExamplesAsync(
        int tenantId,
        string connectionString,
        int negativeExamplesPerPositive,
        bool includeFeedback,
        TenantMlTrainingResult result,
        CancellationToken ct)
    {
        var options = new DbContextOptionsBuilder<ShardingSingleDbContext>()
            .UseSqlServer(connectionString, sql => sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
            .Options;

        await using var db = new ShardingSingleDbContext(options)
        {
            TenantId = tenantId
        };

        var tasks = await db.Tasks
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsActive)
            .Include(x => x.Resources)
            .OrderBy(x => x.Id)
            .ToListAsync(ct);

        var trainableTasks = tasks
            .Where(x => x.Resources.Any(r => r.TenantId == tenantId && r.IsActive))
            .ToList();

        result.Quality = BuildQualityReport(tasks);

        var resources = trainableTasks
            .SelectMany(x => x.Resources)
            .Where(x => x.TenantId == tenantId && x.IsActive)
            .GroupBy(x => x.Id)
            .Select(x => x.First())
            .OrderBy(x => x.Id)
            .ToList();

        var taskById = tasks.ToDictionary(x => x.Id);
        var linkedResourceIdsByTask = trainableTasks.ToDictionary(
            x => x.Id,
            x => x.Resources.Select(r => r.Id).ToHashSet());

        var examples = new List<ModelInput>();
        foreach (var task in trainableTasks)
        {
            ct.ThrowIfCancellationRequested();

            foreach (var resource in task.Resources.Where(x => x.TenantId == tenantId && x.IsActive))
            {
                examples.Add(CreateExample(
                    BuildContextualTaskName(task, taskById),
                    task.Code,
                    task.Unit,
                    task.Quantity,
                    resource.Name,
                    resource.Unit,
                    resource.Quantity,
                    label: true,
                    isBlueprintSource: false));
                result.PositiveExamples++;
            }

            foreach (var negative in SelectNegativeResources(
                         task,
                         resources,
                         linkedResourceIdsByTask.GetValueOrDefault(task.Id) ?? [],
                         negativeExamplesPerPositive))
            {
                examples.Add(CreateExample(
                    BuildContextualTaskName(task, taskById),
                    task.Code,
                    task.Unit,
                    task.Quantity,
                    negative.Name,
                    negative.Unit,
                    negative.Quantity,
                    label: false,
                    isBlueprintSource: false));
                result.NegativeExamples++;
            }
        }

        if (includeFeedback)
            await AddFeedbackExamplesAsync(db, tenantId, taskById, examples, result, ct);

        return examples;
    }

    private static async Task<List<TenantMlSuggestionPreviewItem>> PreviewTenantSuggestionsAsync(
        TenantMlSuggestionPreviewRequest request,
        string connectionString,
        string targetNormalized,
        string targetNameNormalized,
        int targetCodeDepth,
        PredictionEngine<ModelInput, ModelOutput>? tenantEngine,
        PredictionEngine<ModelInput, ModelOutput>? globalEngine,
        TenantMlUsageMode usageMode,
        CancellationToken ct)
    {
        var options = new DbContextOptionsBuilder<ShardingSingleDbContext>()
            .UseSqlServer(connectionString, sql => sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
            .Options;

        await using var db = new ShardingSingleDbContext(options)
        {
            TenantId = request.TenantId
        };

        var tasks = await db.Tasks
            .AsNoTracking()
            .Where(x => x.TenantId == request.TenantId && x.IsActive && x.Resources.Any())
            .OrderByDescending(x => x.Id)
            .Take(700)
            .ToListAsync(ct);

        return tasks
            .Select(task =>
            {
                var candidateText = SwedishTaskTextNormalizer.NormalizeTask(task.Name, task.Code, task.Unit, task.Quantity);
                var candidateName = SwedishTaskTextNormalizer.Normalize(task.Name);
                var score = ScorePreviewCandidate(
                    targetNormalized,
                    targetNameNormalized,
                    request.TaskUnit,
                    request.TaskQuantity,
                    request.TaskCode,
                    targetCodeDepth,
                    candidateText,
                    candidateName,
                    task.Unit,
                    task.Quantity,
                    task.Code,
                    isBlueprintSource: false,
                    tenantEngine,
                    globalEngine,
                    usageMode);

                return new TenantMlSuggestionPreviewItem
                {
                    Source = "TenantTask",
                    TaskName = task.Name,
                    Unit = task.Unit,
                    Score = score.Score,
                    Reason = score.Reason
                };
            })
            .Where(x => x.Score >= 0.30d)
            .OrderByDescending(x => x.Score)
            .Take(25)
            .ToList();
    }

    private async Task<List<TenantMlSuggestionPreviewItem>> PreviewBlueprintSuggestionsAsync(
        TenantMlSuggestionPreviewRequest request,
        string targetNormalized,
        string targetNameNormalized,
        int targetCodeDepth,
        PredictionEngine<ModelInput, ModelOutput>? tenantEngine,
        PredictionEngine<ModelInput, ModelOutput>? globalEngine,
        TenantMlUsageMode usageMode,
        CancellationToken ct)
    {
        await using var db = await blueprintDbFactory.CreateDbContextAsync(ct);
        var suggestionStatuses = new[]
        {
            TaskResourceBlueprints.Entities.Tasks.TaskStatusEnum.Ready,
            TaskResourceBlueprints.Entities.Tasks.TaskStatusEnum.SuggestionOnly
        };
        var tasks = await db.Tasks
            .AsNoTracking()
            .Where(x =>
                suggestionStatuses.Contains(x.Status) &&
                x.ResourceLinks.Any(link => link.Resource != null && link.Resource.IsActive && link.Resource.IsVisible))
            .OrderByDescending(x => x.UsageCount)
            .ThenBy(x => x.SortOrder)
            .Take(700)
            .ToListAsync(ct);

        return tasks
            .Select(task =>
            {
                var candidateText = string.IsNullOrWhiteSpace(task.NormalizedTextSv)
                    ? SwedishTaskTextNormalizer.NormalizeTask(task.Name, task.Code, task.UnitCode, task.Quantity)
                    : task.NormalizedTextSv;
                var candidateName = SwedishTaskTextNormalizer.Normalize(task.Name);
                var score = ScorePreviewCandidate(
                    targetNormalized,
                    targetNameNormalized,
                    request.TaskUnit,
                    request.TaskQuantity,
                    request.TaskCode,
                    targetCodeDepth,
                    candidateText,
                    candidateName,
                    task.UnitCode,
                    task.Quantity,
                    task.Code,
                    isBlueprintSource: true,
                    tenantEngine,
                    globalEngine,
                    usageMode);

                return new TenantMlSuggestionPreviewItem
                {
                    Source = "BlueprintTask",
                    TaskName = task.Name,
                    Unit = task.UnitCode,
                    Score = score.Score,
                    Reason = score.Reason
                };
            })
            .Where(x => x.Score >= 0.30d)
            .OrderByDescending(x => x.Score)
            .Take(25)
            .ToList();
    }

    private async Task<AuthPermissions.Entity.TenantEntity?> GetTenantWithDatabaseAsync(int tenantId, CancellationToken ct)
    {
        await using var authDb = await authDbFactory.CreateDbContextAsync(ct);
        return await authDb.Tenants
            .AsNoTracking()
            .Include(x => x.TenantDB)
            .FirstOrDefaultAsync(x => x.Id == tenantId, ct);
    }

    private async Task<TenantMlSettingEntity?> GetSettingAsync(int tenantId, CancellationToken ct)
    {
        await using var authDb = await authDbFactory.CreateDbContextAsync(ct);
        return await authDb.TenantMlSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, ct);
    }

    private async Task AddFeedbackExamplesAsync(
        ShardingSingleDbContext tenantDb,
        int tenantId,
        IReadOnlyDictionary<int, Domain.Entities.Calculation.TaskEntity> tenantTasksById,
        List<ModelInput> examples,
        TenantMlTrainingResult result,
        CancellationToken ct)
    {
        await using var blueprintDb = await blueprintDbFactory.CreateDbContextAsync(ct);
        var feedbacks = await blueprintDb.TaskResourceSuggestionFeedbacks
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.ReviewStatus == TaskResourceSuggestionFeedbackReviewStatus.Approved &&
                (x.Feedback == TaskResourceSuggestionFeedbackKind.Accepted ||
                 x.Feedback == TaskResourceSuggestionFeedbackKind.Rejected ||
                 x.Feedback == TaskResourceSuggestionFeedbackKind.WrongUnit ||
                 x.Feedback == TaskResourceSuggestionFeedbackKind.WrongResourceType))
            .ToListAsync(ct);

        if (feedbacks.Count == 0)
            return;

        var tenantSourceIds = feedbacks
            .Where(x => x.Source == TaskResourceSuggestionSource.TenantTask)
            .Select(x => x.SourceTaskId)
            .Distinct()
            .ToList();
        var blueprintSourceIds = feedbacks
            .Where(x => x.Source == TaskResourceSuggestionSource.BlueprintTask)
            .Select(x => x.SourceTaskId)
            .Distinct()
            .ToList();

        var tenantSourceTasks = await tenantDb.Tasks
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && tenantSourceIds.Contains(x.Id))
            .Include(x => x.Resources)
            .ToDictionaryAsync(x => x.Id, ct);

        var blueprintLinks = await blueprintDb.TaskDefinitionResourceLinks
            .AsNoTracking()
            .Include(x => x.Resource)
            .Where(x =>
                blueprintSourceIds.Contains(x.TaskDefinitionId) &&
                x.Resource != null &&
                x.Resource.IsActive &&
                x.Resource.IsVisible)
            .ToListAsync(ct);
        var blueprintLinksByTask = blueprintLinks
            .GroupBy(x => x.TaskDefinitionId)
            .ToDictionary(x => x.Key, x => x.ToList());

        foreach (var feedback in feedbacks)
        {
            ct.ThrowIfCancellationRequested();
            var label = feedback.Feedback == TaskResourceSuggestionFeedbackKind.Accepted;

            if (feedback.Source == TaskResourceSuggestionSource.TenantTask &&
                tenantSourceTasks.TryGetValue(feedback.SourceTaskId, out var sourceTask))
            {
                foreach (var resource in sourceTask.Resources.Where(x => x.TenantId == tenantId && x.IsActive))
                    AddFeedbackExample(examples, result, feedback, resource.Name, resource.Unit, resource.Quantity, label, isBlueprintSource: false);
            }
            else if (feedback.Source == TaskResourceSuggestionSource.BlueprintTask &&
                     blueprintLinksByTask.TryGetValue(feedback.SourceTaskId, out var sourceLinks))
            {
                foreach (var link in sourceLinks)
                {
                    if (link.Resource is null)
                        continue;

                    AddFeedbackExample(examples, result, feedback, link.Resource.Name, link.Resource.Unit, link.Quantity, label, isBlueprintSource: true);
                }
            }
        }
    }

    private static void AddFeedbackExample(
        List<ModelInput> examples,
        TenantMlTrainingResult result,
        TaskResourceBlueprints.Entities.Tasks.TaskResourceSuggestionFeedback feedback,
        string resourceName,
        string? resourceUnit,
        decimal? resourceQuantity,
        bool label,
        bool isBlueprintSource)
    {
        examples.Add(CreateExample(
            feedback.TargetTaskName,
            feedback.TargetTaskCode,
            feedback.TargetTaskUnit,
            feedback.TargetTaskQuantity,
            resourceName,
            resourceUnit,
            resourceQuantity,
            label,
            isBlueprintSource));

        if (label)
            result.PositiveExamples++;
        else
            result.NegativeExamples++;

        result.FeedbackExamples++;
    }

    private static IEnumerable<Domain.Entities.Calculation.ResourceEntity> SelectNegativeResources(
        Domain.Entities.Calculation.TaskEntity task,
        IReadOnlyList<Domain.Entities.Calculation.ResourceEntity> resources,
        IReadOnlySet<int> linkedResourceIds,
        int count)
    {
        if (count <= 0 || resources.Count == 0)
            return [];

        var selected = new List<Domain.Entities.Calculation.ResourceEntity>(count);
        var start = Math.Abs(HashCode.Combine(task.Id, task.Code, task.Name)) % resources.Count;

        for (var offset = 0; offset < resources.Count && selected.Count < count; offset++)
        {
            var resource = resources[(start + offset) % resources.Count];
            if (linkedResourceIds.Contains(resource.Id))
                continue;

            selected.Add(resource);
        }

        return selected;
    }

    private static ModelInput CreateExample(
        string taskName,
        string? taskCode,
        string? taskUnit,
        decimal? taskQuantity,
        string resourceName,
        string? resourceUnit,
        decimal? resourceQuantity,
        bool label,
        bool isBlueprintSource)
    {
        var taskText = BuildTaskText(taskName, taskCode, taskUnit, taskQuantity);
        var normalizedTask = SwedishTaskTextNormalizer.Normalize(taskText);
        var normalizedTaskName = SwedishTaskTextNormalizer.Normalize(taskName);
        var normalizedResource = SwedishTaskTextNormalizer.NormalizeTask(resourceName, null, resourceUnit, resourceQuantity);
        var normalizedResourceName = SwedishTaskTextNormalizer.Normalize(resourceName);

        var textSimilarity = SwedishTaskTextNormalizer.CalculateSimilarity(normalizedTask, normalizedResource);
        var nameSimilarity = SwedishTaskTextNormalizer.CalculateSimilarity(normalizedTaskName, normalizedResourceName);
        var quantitySimilarity = CalculateQuantitySimilarity(taskQuantity, resourceQuantity);
        var hasSameUnit = QuantityUnitNormalizer.AreEquivalentUnits(taskUnit, resourceUnit);
        var hasCompatibleUnit = hasSameUnit || QuantityUnitNormalizer.AreCompatibleUnits(taskUnit, resourceUnit);
        var unitBonus = hasSameUnit ? 0.15d : hasCompatibleUnit ? 0.10d : -0.04d;
        var codeBonus = SwedishTaskTextNormalizer.GetCodeHierarchyScore(taskCode, null);
        var heuristic = (textSimilarity * 0.45d) +
                        (nameSimilarity * 0.30d) +
                        (quantitySimilarity * 0.10d) +
                        unitBonus +
                        codeBonus +
                        (isBlueprintSource ? 0.015d : 0.04d);

        return new ModelInput
        {
            Label = label ? 1f : 0f,
            TaskName = taskName,
            ResourceName = resourceName,
            Source = isBlueprintSource ? "BlueprintTask" : "TenantTask",
            HeuristicScore = (float)Math.Clamp(heuristic, 0d, 1d),
            TextScore = (float)textSimilarity,
            NameScore = (float)nameSimilarity,
            QuantitySimilarity = (float)quantitySimilarity,
            HasQuantitySimilarity = taskQuantity.HasValue && resourceQuantity.HasValue ? 1f : 0f,
            UnitBonus = (float)unitBonus,
            CodeBonus = (float)codeBonus,
            HasEquivalentUnits = hasSameUnit ? 1f : 0f,
            HasCompatibleUnits = hasCompatibleUnit ? 1f : 0f,
            IsBlueprintSource = isBlueprintSource ? 1f : 0f
        };
    }

    private static (double Score, string Reason) ScorePreviewCandidate(
        string targetNormalized,
        string targetNameNormalized,
        string? targetUnit,
        decimal? targetQuantity,
        string? targetCode,
        int targetCodeDepth,
        string candidateNormalized,
        string candidateNameNormalized,
        string? candidateUnit,
        decimal? candidateQuantity,
        string? candidateCode,
        bool isBlueprintSource,
        PredictionEngine<ModelInput, ModelOutput>? tenantEngine,
        PredictionEngine<ModelInput, ModelOutput>? globalEngine,
        TenantMlUsageMode usageMode)
    {
        var textScore = SwedishTaskTextNormalizer.CalculateSimilarity(targetNormalized, candidateNormalized);
        var nameScore = SwedishTaskTextNormalizer.CalculateSimilarity(targetNameNormalized, candidateNameNormalized);
        var quantitySimilarity = CalculateQuantitySimilarity(targetQuantity, candidateQuantity);
        var hasSameUnit = QuantityUnitNormalizer.AreEquivalentUnits(targetUnit, candidateUnit);
        var hasCompatibleUnit = hasSameUnit || QuantityUnitNormalizer.AreCompatibleUnits(targetUnit, candidateUnit);
        var unitBonus = hasSameUnit ? 0.15d : hasCompatibleUnit ? 0.10d : -0.04d;
        var codeBonus = SwedishTaskTextNormalizer.GetCodeHierarchyScore(targetCode, candidateCode);
        var heuristic = Math.Clamp(
            (textScore * 0.46d) +
            (nameScore * 0.30d) +
            (quantitySimilarity * 0.12d) +
            unitBonus +
            codeBonus +
            (isBlueprintSource ? 0.015d : 0.04d),
            0d,
            1d);

        var input = new ModelInput
        {
            Label = 0f,
            HeuristicScore = (float)heuristic,
            TextScore = (float)textScore,
            NameScore = (float)nameScore,
            QuantitySimilarity = (float)quantitySimilarity,
            HasQuantitySimilarity = targetQuantity.HasValue && candidateQuantity.HasValue ? 1f : 0f,
            UnitBonus = (float)unitBonus,
            CodeBonus = (float)codeBonus,
            HasEquivalentUnits = hasSameUnit ? 1f : 0f,
            HasCompatibleUnits = hasCompatibleUnit ? 1f : 0f,
            IsBlueprintSource = isBlueprintSource ? 1f : 0f
        };

        var engine = usageMode switch
        {
            TenantMlUsageMode.GlobalOnly => globalEngine,
            TenantMlUsageMode.TenantOnly => tenantEngine,
            _ => tenantEngine ?? globalEngine
        };
        var mlScore = engine is null ? (double?)null : Math.Clamp(engine.Predict(input).Score, 0f, 1f);
        var finalScore = mlScore.HasValue
            ? Math.Round((heuristic * 0.75d) + (mlScore.Value * 0.25d), 4)
            : Math.Round(heuristic, 4);

        var parts = new List<string>
        {
            $"text {textScore:0%}",
            $"name {nameScore:0%}"
        };
        if (hasSameUnit)
            parts.Add("same unit");
        else if (hasCompatibleUnit)
            parts.Add("compatible unit");
        if (codeBonus > 0)
            parts.Add($"code +{codeBonus:0%}");
        if (targetCodeDepth > 0 || GetCodeDepth(candidateCode) > 0)
            parts.Add("parent/code context");
        if (mlScore.HasValue)
            parts.Add($"ML {mlScore.Value:0%}");

        return (finalScore, string.Join(", ", parts));
    }

    private static TenantMlQualityReport BuildQualityReport(IReadOnlyList<Domain.Entities.Calculation.TaskEntity> tasks)
    {
        var resources = tasks
            .SelectMany(x => x.Resources)
            .Where(x => x.IsActive)
            .ToList();

        return new TenantMlQualityReport
        {
            TotalTasks = tasks.Count,
            TasksWithResources = tasks.Count(x => x.Resources.Any(r => r.IsActive)),
            TasksWithoutResources = tasks.Count(x => !x.Resources.Any(r => r.IsActive)),
            TotalResources = resources.Count,
            MissingUnitResources = resources.Count(x => string.IsNullOrWhiteSpace(x.Unit)),
            UnknownTypeResources = resources.Count(x => !Enum.IsDefined(typeof(ResourceTypesEnum), x.ResType)),
            ResourceTypes = resources
                .GroupBy(x => x.ResType.ToString())
                .OrderByDescending(x => x.Count())
                .Take(8)
                .Select(x => new TenantMlResourceTypeCount
                {
                    ResourceType = x.Key,
                    Count = x.Count()
                })
                .ToList(),
            TaskClusters = BuildTaskClusters(tasks),
            MissingTokens = BuildMissingTokens(tasks)
        };
    }

    private static List<TenantMlTaskClusterItem> BuildTaskClusters(IReadOnlyList<Domain.Entities.Calculation.TaskEntity> tasks)
    {
        var definitions = new (string Name, string[] Tokens)[]
        {
            ("Excavation", ["schakt", "jord", "grav", "berg"]),
            ("Filling", ["fyll", "aterfyll", "lager", "grus", "sand"]),
            ("Cable and pipes", ["kabel", "ledning", "ror", "fiber", "tele", "vatten", "avlopp"]),
            ("Road and surfaces", ["vag", "gata", "gang", "cykel", "asfalt", "belagg", "yta"]),
            ("Green areas", ["gron", "gras", "plantering", "trad", "buske"]),
            ("Concrete", ["betong", "gjut", "armering", "fundament"]),
            ("Drainage", ["dagvatten", "dranering", "brunn", "dike", "infiltrat"])
        };

        var rows = tasks
            .Select(task => new
            {
                Task = task,
                Tokens = SwedishTaskTextNormalizer.ExtractNormalizedTokens(
                    SwedishTaskTextNormalizer.NormalizeTask(task.Name, task.Code, task.Unit, task.Quantity))
            })
            .ToList();

        return definitions
            .Select(definition =>
            {
                var matched = rows
                    .Where(row => row.Tokens.Any(token => definition.Tokens.Contains(token, StringComparer.OrdinalIgnoreCase)))
                    .ToList();

                return new TenantMlTaskClusterItem
                {
                    Cluster = definition.Name,
                    TotalTasks = matched.Count,
                    TasksWithResources = matched.Count(row => row.Task.Resources.Any(resource => resource.IsActive)),
                    Examples = matched
                        .Select(row => row.Task.Name)
                        .Where(name => !string.IsNullOrWhiteSpace(name))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Take(3)
                        .ToList()
                };
            })
            .Where(x => x.TotalTasks > 0)
            .OrderByDescending(x => x.TotalTasks)
            .ToList();
    }

    private static List<TenantMlMissingTokenItem> BuildMissingTokens(IReadOnlyList<Domain.Entities.Calculation.TaskEntity> tasks)
    {
        var ignored = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "task", "tenanttask", "m2", "m3", "st", "kg", "m", "mm", "cm",
            "typ", "klass", "fall", "minsta", "galler", "utan", "med", "och"
        };

        return tasks
            .Where(task => !task.Resources.Any(resource => resource.IsActive))
            .Select(task => new
            {
                task.Name,
                Tokens = SwedishTaskTextNormalizer.ExtractNormalizedTokens(
                    SwedishTaskTextNormalizer.NormalizeTask(task.Name, task.Code, task.Unit, task.Quantity))
            })
            .SelectMany(row => row.Tokens
                .Where(token => token.Length >= 4 && !char.IsDigit(token[0]) && !ignored.Contains(token))
                .Select(token => new { Token = token, row.Name }))
            .GroupBy(x => x.Token, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(x => x.Count())
            .Take(12)
            .Select(group => new TenantMlMissingTokenItem
            {
                Token = group.Key,
                Count = group.Count(),
                Examples = group
                    .Select(x => x.Name)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(3)
                    .ToList()
            })
            .ToList();
    }

    private static TenantMlComparisonReport CompareModels(IReadOnlyList<ModelInput> examples, string tenantModelPath)
    {
        var sample = examples
            .OrderBy(x => x.Label)
            .ThenByDescending(x => x.HeuristicScore)
            .Take(2000)
            .ToList();

        var globalEngine = CreatePredictionEngine(TaskResourceSuggestionMlModelPath.GetDefaultModelPath());
        var tenantEngine = CreatePredictionEngine(tenantModelPath);

        var report = new TenantMlComparisonReport
        {
            HasGlobalModel = globalEngine is not null,
            HasTenantModel = tenantEngine is not null
        };

        if (globalEngine is not null)
            FillComparison(sample, globalEngine, isTenant: false, report);

        if (tenantEngine is not null)
            FillComparison(sample, tenantEngine, isTenant: true, report);
        else if (globalEngine is not null)
            FillExamples(sample, globalEngine, report);

        report.BetterModel = report switch
        {
            { TenantMargin: not null, GlobalMargin: not null } when report.TenantMargin > report.GlobalMargin => "Tenant",
            { TenantMargin: not null, GlobalMargin: not null } when report.GlobalMargin > report.TenantMargin => "Global",
            { TenantMargin: not null } => "Tenant",
            { GlobalMargin: not null } => "Global",
            _ => "Unknown"
        };

        return report;
    }

    private static void FillComparison(
        IReadOnlyList<ModelInput> sample,
        PredictionEngine<ModelInput, ModelOutput> engine,
        bool isTenant,
        TenantMlComparisonReport report)
    {
        var scored = sample
            .Select(x => new { x.Label, Score = Math.Clamp(engine.Predict(x).Score, 0f, 1f) })
            .ToList();
        var positives = scored.Where(x => x.Label > 0.5f).Select(x => (double)x.Score).ToList();
        var negatives = scored.Where(x => x.Label <= 0.5f).Select(x => (double)x.Score).ToList();
        double? positiveAverage = positives.Count == 0 ? null : Math.Round(positives.Average(), 4);
        double? negativeAverage = negatives.Count == 0 ? null : Math.Round(negatives.Average(), 4);
        var margin = positiveAverage.HasValue && negativeAverage.HasValue
            ? Math.Round(positiveAverage.Value - negativeAverage.Value, 4)
            : (double?)null;

        if (isTenant)
        {
            report.TenantPositiveAverage = positiveAverage;
            report.TenantNegativeAverage = negativeAverage;
            report.TenantMargin = margin;
            FillExamples(sample, engine, report);
        }
        else
        {
            report.GlobalPositiveAverage = positiveAverage;
            report.GlobalNegativeAverage = negativeAverage;
            report.GlobalMargin = margin;
        }
    }

    private static void FillExamples(
        IReadOnlyList<ModelInput> sample,
        PredictionEngine<ModelInput, ModelOutput> engine,
        TenantMlComparisonReport report)
    {
        var scored = sample
            .Select(x => new ScoredEvaluationExample(x, Math.Clamp(engine.Predict(x).Score, 0f, 1f)))
            .ToList();

        report.BestExamples = scored
            .Where(x => x.Input.Label > 0.5f)
            .OrderByDescending(x => x.Score)
            .Take(8)
            .Select(ToEvaluationExample)
            .ToList();

        var falseNegatives = scored
            .Where(x => x.Input.Label > 0.5f)
            .OrderBy(x => x.Score)
            .Take(4);
        var falsePositives = scored
            .Where(x => x.Input.Label <= 0.5f)
            .OrderByDescending(x => x.Score)
            .Take(4);

        report.WorstExamples = falseNegatives
            .Concat(falsePositives)
            .Select(ToEvaluationExample)
            .ToList();
    }

    private static TenantMlEvaluationExample ToEvaluationExample(ScoredEvaluationExample row)
        => new()
        {
            TaskName = row.Input.TaskName,
            ResourceName = row.Input.ResourceName,
            ExpectedGood = row.Input.Label > 0.5f,
            Score = Math.Round((double)row.Score, 4),
            Source = row.Input.Source
        };

    private static PredictionEngine<ModelInput, ModelOutput>? CreatePredictionEngine(string modelPath)
    {
        if (!File.Exists(modelPath))
            return null;

        try
        {
            var ml = new MLContext(seed: 251119);
            using var stream = File.OpenRead(modelPath);
            var model = ml.Model.Load(stream, out _);
            return ml.Model.CreatePredictionEngine<ModelInput, ModelOutput>(model);
        }
        catch
        {
            return null;
        }
    }

    private static void BackupCurrentModel(int tenantId)
    {
        var modelPath = TaskResourceSuggestionMlModelPath.GetTenantModelPath(tenantId);
        if (!File.Exists(modelPath))
            return;

        var backupDirectory = GetBackupDirectory(tenantId);
        Directory.CreateDirectory(backupDirectory);
        var backupPath = Path.Combine(
            backupDirectory,
            $"task-resource-suggestions-{DateTime.UtcNow:yyyyMMddHHmmss}.zip");
        File.Copy(modelPath, backupPath, overwrite: false);
        CleanupBackupFiles(backupDirectory, keep: 5);
    }

    private static FileInfo? GetLatestBackupFile(int tenantId)
    {
        var backupDirectory = GetBackupDirectory(tenantId);
        if (!Directory.Exists(backupDirectory))
            return null;

        return new DirectoryInfo(backupDirectory)
            .GetFiles("task-resource-suggestions-*.zip")
            .OrderByDescending(x => x.LastWriteTimeUtc)
            .FirstOrDefault();
    }

    private static string GetBackupDirectory(int tenantId)
    {
        var modelPath = TaskResourceSuggestionMlModelPath.GetTenantModelPath(tenantId);
        return Path.Combine(Path.GetDirectoryName(modelPath) ?? AppContext.BaseDirectory, "backups");
    }

    private static void CleanupBackupFiles(string backupDirectory, int keep)
    {
        if (!Directory.Exists(backupDirectory))
            return;

        foreach (var file in new DirectoryInfo(backupDirectory)
                     .GetFiles("task-resource-suggestions-*.zip")
                     .OrderByDescending(x => x.LastWriteTimeUtc)
                     .Skip(Math.Max(keep, 1)))
        {
            try
            {
                file.Delete();
            }
            catch
            {
                // Backup cleanup is best-effort; training should not fail because an old file is locked.
            }
        }
    }

    private static async Task SaveRunAsync(
        IDbContextFactory<AuthPermissionDbContext> authDbFactory,
        int tenantId,
        DateTime startedAtUtc,
        TenantMlTrainingResult result,
        bool success,
        CancellationToken ct)
    {
        await using var authDb = await authDbFactory.CreateDbContextAsync(ct);
        authDb.TenantMlTrainingRuns.Add(new TenantMlTrainingRunEntity
        {
            TenantId = tenantId,
            StartedAtUtc = startedAtUtc,
            CompletedAtUtc = DateTime.UtcNow,
            Success = success,
            ModelPath = result.ModelPath,
            Message = result.Message,
            PositiveExamples = result.PositiveExamples,
            NegativeExamples = result.NegativeExamples,
            FeedbackExamples = result.FeedbackExamples,
            TotalTasks = result.Quality.TotalTasks,
            TasksWithResources = result.Quality.TasksWithResources,
            TasksWithoutResources = result.Quality.TasksWithoutResources,
            TotalResources = result.Quality.TotalResources,
            MissingUnitResources = result.Quality.MissingUnitResources,
            UnknownTypeResources = result.Quality.UnknownTypeResources,
            GlobalPositiveAverage = result.Comparison.GlobalPositiveAverage,
            GlobalNegativeAverage = result.Comparison.GlobalNegativeAverage,
            GlobalMargin = result.Comparison.GlobalMargin,
            TenantPositiveAverage = result.Comparison.TenantPositiveAverage,
            TenantNegativeAverage = result.Comparison.TenantNegativeAverage,
            TenantMargin = result.Comparison.TenantMargin,
            BetterModel = result.Comparison.BetterModel,
            ResourceTypeBreakdownJson = JsonSerializer.Serialize(result.Quality.ResourceTypes)
        });
        await authDb.SaveChangesAsync(ct);
    }

    private static TenantMlTrainingRunDto ToRunDto(TenantMlTrainingRunEntity run)
        => new()
        {
            StartedAtUtc = run.StartedAtUtc,
            CompletedAtUtc = run.CompletedAtUtc,
            Success = run.Success,
            Message = run.Message,
            PositiveExamples = run.PositiveExamples,
            NegativeExamples = run.NegativeExamples,
            FeedbackExamples = run.FeedbackExamples,
            Quality = new TenantMlQualityReport
            {
                TotalTasks = run.TotalTasks,
                TasksWithResources = run.TasksWithResources,
                TasksWithoutResources = run.TasksWithoutResources,
                TotalResources = run.TotalResources,
                MissingUnitResources = run.MissingUnitResources,
                UnknownTypeResources = run.UnknownTypeResources,
                ResourceTypes = DeserializeResourceTypes(run.ResourceTypeBreakdownJson)
            },
            Comparison = new TenantMlComparisonReport
            {
                GlobalPositiveAverage = run.GlobalPositiveAverage,
                GlobalNegativeAverage = run.GlobalNegativeAverage,
                GlobalMargin = run.GlobalMargin,
                TenantPositiveAverage = run.TenantPositiveAverage,
                TenantNegativeAverage = run.TenantNegativeAverage,
                TenantMargin = run.TenantMargin,
                BetterModel = run.BetterModel,
                HasGlobalModel = run.GlobalMargin.HasValue,
                HasTenantModel = run.TenantMargin.HasValue
            }
        };

    private static List<TenantMlResourceTypeCount> DeserializeResourceTypes(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<TenantMlResourceTypeCount>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static string BuildTaskText(
        string taskName,
        string? taskCode,
        string? taskUnit,
        decimal? taskQuantity)
        => string.Join(' ', new[]
        {
            taskName,
            taskCode,
            taskUnit,
            taskQuantity?.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)
        }.Where(x => !string.IsNullOrWhiteSpace(x)));

    private static int GetCodeDepth(string? code)
        => string.IsNullOrWhiteSpace(code)
            ? 0
            : code.Split(['.', '-', '/', '\\'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;

    private static string BuildContextualTaskName(
        Domain.Entities.Calculation.TaskEntity task,
        IReadOnlyDictionary<int, Domain.Entities.Calculation.TaskEntity> taskById)
    {
        var names = new List<string>();
        var seen = new HashSet<int>();
        var current = task;

        while (current.ParentTaskId is { } parentId &&
               seen.Add(parentId) &&
               taskById.TryGetValue(parentId, out var parent))
        {
            names.Add(parent.Name);
            current = parent;
        }

        names.Reverse();
        names.Add(task.Name);
        return string.Join(' ', names.Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static double CalculateQuantitySimilarity(decimal? taskQuantity, decimal? resourceQuantity)
    {
        if (!taskQuantity.HasValue || !resourceQuantity.HasValue)
            return 0d;

        var max = Math.Max(Math.Abs(taskQuantity.Value), Math.Abs(resourceQuantity.Value));
        if (max <= 0m)
            return 1d;

        var diff = Math.Abs(taskQuantity.Value - resourceQuantity.Value);
        return Math.Round((double)Math.Clamp(1m - (diff / max), 0m, 1m), 4);
    }

    private sealed record ModelInput
    {
        public float Label { get; init; }
        public string TaskName { get; init; } = string.Empty;
        public string ResourceName { get; init; } = string.Empty;
        public string Source { get; init; } = string.Empty;
        public float HeuristicScore { get; init; }
        public float TextScore { get; init; }
        public float NameScore { get; init; }
        public float QuantitySimilarity { get; init; }
        public float HasQuantitySimilarity { get; init; }
        public float UnitBonus { get; init; }
        public float CodeBonus { get; init; }
        public float HasEquivalentUnits { get; init; }
        public float HasCompatibleUnits { get; init; }
        public float IsBlueprintSource { get; init; }
    }

    private sealed class ModelOutput
    {
        public float Score { get; set; }
    }

    private sealed record ScoredEvaluationExample(ModelInput Input, float Score);
}
