using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.DTO.Calculation;
using TaskResourceBlueprints.Infrastructure;

namespace TaskResourceBlueprints.Services.Feedback;

public interface ITaskResourceSuggestionFeedbackReviewService
{
    Task<TaskResourceSuggestionFeedbackReviewResult> GetAsync(
        TaskResourceSuggestionFeedbackKind? feedback = null,
        int take = 200,
        CancellationToken ct = default);

    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}

public sealed class TaskResourceSuggestionFeedbackReviewResult
{
    public int Total { get; set; }
    public Dictionary<TaskResourceSuggestionFeedbackKind, int> CountsByFeedback { get; set; } = [];
    public List<TaskResourceSuggestionFeedbackReviewItem> Items { get; set; } = [];
}

public sealed class TaskResourceSuggestionFeedbackReviewItem
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int TargetTaskId { get; set; }
    public string TargetTaskName { get; set; } = string.Empty;
    public string? TargetTaskCode { get; set; }
    public string? TargetTaskUnit { get; set; }
    public decimal? TargetTaskQuantity { get; set; }
    public int SourceTaskId { get; set; }
    public string SourceTaskName { get; set; } = string.Empty;
    public string? SourceTaskUnit { get; set; }
    public decimal? SourceTaskQuantity { get; set; }
    public TaskResourceSuggestionSource Source { get; set; }
    public double Score { get; set; }
    public TaskResourceSuggestionFeedbackKind Feedback { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class TaskResourceSuggestionFeedbackReviewService(
    IDbContextFactory<TaskResourceBlueprintsContext> dbContextFactory)
    : ITaskResourceSuggestionFeedbackReviewService
{
    public async Task<TaskResourceSuggestionFeedbackReviewResult> GetAsync(
        TaskResourceSuggestionFeedbackKind? feedback = null,
        int take = 200,
        CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 1000);
        await using var db = await dbContextFactory.CreateDbContextAsync(ct);

        var baseQuery = db.TaskResourceSuggestionFeedbacks.AsNoTracking();
        var total = await baseQuery.CountAsync(ct);
        var counts = await baseQuery
            .GroupBy(x => x.Feedback)
            .Select(x => new { Feedback = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.Feedback, x => x.Count, ct);

        var query = feedback.HasValue
            ? baseQuery.Where(x => x.Feedback == feedback.Value)
            : baseQuery;

        var items = await query
            .OrderByDescending(x => x.UpdatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Take(take)
            .Select(x => new TaskResourceSuggestionFeedbackReviewItem
            {
                Id = x.Id,
                TenantId = x.TenantId,
                TargetTaskId = x.TargetTaskId,
                TargetTaskName = x.TargetTaskName,
                TargetTaskCode = x.TargetTaskCode,
                TargetTaskUnit = x.TargetTaskUnit,
                TargetTaskQuantity = x.TargetTaskQuantity,
                SourceTaskId = x.SourceTaskId,
                SourceTaskName = x.SourceTaskName,
                SourceTaskUnit = x.SourceTaskUnit,
                SourceTaskQuantity = x.SourceTaskQuantity,
                Source = x.Source,
                Score = x.Score,
                Feedback = x.Feedback,
                Reason = x.Reason,
                CreatedAtUtc = x.CreatedAtUtc,
                UpdatedAtUtc = x.UpdatedAtUtc,
            })
            .ToListAsync(ct);

        return new TaskResourceSuggestionFeedbackReviewResult
        {
            Total = total,
            CountsByFeedback = counts,
            Items = items,
        };
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        if (id <= 0)
            return false;

        await using var db = await dbContextFactory.CreateDbContextAsync(ct);
        var feedback = await db.TaskResourceSuggestionFeedbacks.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (feedback is null)
            return false;

        db.TaskResourceSuggestionFeedbacks.Remove(feedback);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
