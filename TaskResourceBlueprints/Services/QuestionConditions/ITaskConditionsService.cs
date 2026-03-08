using TaskResourceBlueprints.Entities.Questions.Conditions;
using TaskResourceBlueprints.Entities.Questions.Groups;

namespace TaskResourceBlueprints.Services.QuestionConditions;

public interface ITaskConditionsService
{
    Task<List<ConditionDefinition>> GetConditionsAsync(int taskId, CancellationToken ct);
    Task<List<ResourceSelectorDefinition>> GetResourceGroupsAsync(int taskId, CancellationToken ct);
    Task<List<QuestionGroupDefinition>> GetChoiceGroupsAsync(int taskId, CancellationToken ct);
    Task<List<NumericQuestionDefinition>> GetNumericGroupsAsync(int taskId, CancellationToken ct);

    Task<Dictionary<int, List<OptionBindVM>>> GetOptionBindsByResourceAssignmentsAsync(int[] raIds, CancellationToken ct);
    Task<Dictionary<int, List<NumericBindVM>>> GetNumericBindsByResourceAssignmentsAsync(int[] raIds, CancellationToken ct);

    Task<ConditionDefinition?> GetConditionAsync(int id, CancellationToken ct);
    Task<int> UpsertConditionAsync(ConditionDefinition editing, CancellationToken ct);
    Task DeleteConditionAsync(int id, CancellationToken ct);

    Task<int> UpsertOptionBindingAsync(int raId, OptionBindVM vm, CancellationToken ct);
    Task<int> UpsertNumericBindingAsync(int raId, NumericBindVM vm, CancellationToken ct);
    Task DeleteOptionBindingAsync(int id, CancellationToken ct);
    Task DeleteNumericBindingAsync(int id, CancellationToken ct);
}

public sealed class OptionBindVM
{
    public int Id { get; set; }
    public int ChoiceGroupId { get; set; }
    public int ChoiceOptionId { get; set; }
    public List<string> Formulas { get; set; } = new();
    public bool SavedOk { get; set; }
    public string? LastError { get; set; }
}

public sealed class NumericBindVM
{
    public int Id { get; set; }
    public int NumericId { get; set; }
    public decimal? InputMinValue { get; set; }
    public decimal? InputMaxValue { get; set; }
    public List<string> Formulas { get; set; } = new();
    public bool SavedOk { get; set; }
    public string? LastError { get; set; }
}
