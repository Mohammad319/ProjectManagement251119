using ProjectImportHub.Entities.Questions.Conditions;
using ProjectImportHub.Entities.Questions.Groups;

namespace ProjectImportHub.Services.QuestionConditions;

public interface ITaskConditionsService
{
    // تحميل list الصفحة (الشروط + مجموعات المستوى task)
    Task<List<ConditionDefinition>> GetConditionsAsync(int taskId, CancellationToken ct);
    Task<List<ResourceSelectorDefinition>> GetResourceGroupsAsync(int taskId, CancellationToken ct);
    Task<List<QuestionGroupDefinition>> GetChoiceGroupsAsync(int taskId, CancellationToken ct);
    Task<List<NumericQuestionDefinition>> GetNumericGroupsAsync(int taskId, CancellationToken ct);

    // تحميل Bindings حسب ResourceAssignment Ids
    Task<Dictionary<int, List<OptionBindVM>>> GetOptionBindsByResourceAssignmentsAsync(int[] raIds, CancellationToken ct);
    Task<Dictionary<int, List<NumericBindVM>>> GetNumericBindsByResourceAssignmentsAsync(int[] raIds, CancellationToken ct);

    // CRUD لشرط واحد
    Task<ConditionDefinition?> GetConditionAsync(int id, CancellationToken ct);
    Task<int> UpsertConditionAsync(ConditionDefinition editing, CancellationToken ct);
    Task DeleteConditionAsync(int id, CancellationToken ct);

    // حفظ ربطات Option/Numeric لِـ ResourceAssignment محدد
    Task<int> UpsertOptionBindingAsync(int raId, OptionBindVM vm, CancellationToken ct);
    Task<int> UpsertNumericBindingAsync(int raId, NumericBindVM vm, CancellationToken ct);
}

// ViewModels المستخدمة في الصفحة (كما هي بأسمائها)
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
    public double? InputMinValue { get; set; }
    public double? InputMaxValue { get; set; }
    public List<string> Formulas { get; set; } = new();
    public bool SavedOk { get; set; }
    public string? LastError { get; set; }
}
