using ProjectManagement.Shared.DTO.Project;

namespace ProjectManagement.Client.Services.Calculation;

public sealed class SelectionChangedEventArgs(IReadOnlySet<int> taskIds, IReadOnlySet<int> resourceIds)
{
    public static SelectionChangedEventArgs None { get; } =
        new SelectionChangedEventArgs(new HashSet<int>(), new HashSet<int>());

    public IReadOnlySet<int> TaskIds { get; } = taskIds;
    public IReadOnlySet<int> ResourceIds { get; } = resourceIds;

    public bool HasChanges => TaskIds.Count > 0 || ResourceIds.Count > 0;

    public bool Affects(CalculationItemType type, int id) =>
        type == CalculationItemType.task
            ? TaskIds.Contains(id)
            : ResourceIds.Contains(id);
}

public sealed class CalculationInteractionState
{
    private readonly record struct SelectionSnapshot(CalculationItemType? Type, HashSet<int> Ids);

    private readonly List<ResourceTaskItemDTO> _selectedItems = [];
    private readonly List<ResourceTaskItemDTO> _clipboardItems = [];

    public event Action? Changed;
    public event Action<SelectionChangedEventArgs>? SelectionChanged;

    public string ModifierKey { get; private set; } = string.Empty;
    public CalculationItemType? SelectedType { get; private set; }
    public IReadOnlyList<ResourceTaskItemDTO> SelectedItems => _selectedItems;

    public int ClipboardSourceCalculationId { get; private set; }
    public CalculationItemType? ClipboardType { get; private set; }
    public CopyType? ClipboardMode { get; private set; }
    public IReadOnlyList<ResourceTaskItemDTO> ClipboardItems => _clipboardItems;

    public bool IsSelected(CalculationItemType type, int id) =>
        SelectedType == type && _selectedItems.Any(x => x.Id == id);

    public void SetModifierKey(string key) => ModifierKey = key ?? string.Empty;

    public void ClearModifierKey() => ModifierKey = string.Empty;

    public void HandleItemSelected(int id, decimal? value, CalculationItemType type)
    {
        var before = CaptureSelection();

        if (ModifierKey is "Control" or "Meta")
            ToggleSelection(id, value, type);
        else
            ResetSelectionCore();

        NotifyStateChanged(before, notifySelection: true);
    }

    public void Copy(int calculationId, int id, decimal? value, CalculationItemType type) =>
        SetClipboard(calculationId, id, value, type, CopyType.Copy);

    public void Cut(int calculationId, int id, decimal? value, CalculationItemType type) =>
        SetClipboard(calculationId, id, value, type, CopyType.Move);

    public bool CanPaste(CalculationItemType type) =>
        _clipboardItems.Count > 0
        && ClipboardType == type
        && (ClipboardMode == CopyType.Copy || ClipboardMode == CopyType.Move);

    public void ResetSelection(bool notify = true)
    {
        if (!notify)
        {
            ResetSelectionCore();
            return;
        }

        var before = CaptureSelection();
        ResetSelectionCore();
        NotifyStateChanged(before, notifySelection: true);
    }

    public void ResetClipboard(bool notify = true)
    {
        bool hadClipboard = ClipboardType != null || ClipboardMode != null || ClipboardSourceCalculationId != 0 || _clipboardItems.Count > 0;

        ClipboardType = null;
        ClipboardMode = null;
        ClipboardSourceCalculationId = 0;
        _clipboardItems.Clear();

        if (notify && hadClipboard)
            Changed?.Invoke();
    }

    private void ResetSelectionCore()
    {
        SelectedType = null;
        _selectedItems.Clear();
    }

    private void ToggleSelection(int id, decimal? value, CalculationItemType type)
    {
        if (type != SelectedType)
        {
            _selectedItems.Clear();
            SelectedType = type;
        }

        var existing = _selectedItems.FirstOrDefault(x => x.Id == id);
        if (existing is null)
            _selectedItems.Add(new ResourceTaskItemDTO(id, value));
        else
            _selectedItems.Remove(existing);
    }

    private void SetClipboard(int calculationId, int id, decimal? value, CalculationItemType type, CopyType mode)
    {
        var before = CaptureSelection();

        ResetClipboard(notify: false);

        if (!IsSelected(type, id))
            _clipboardItems.Add(new ResourceTaskItemDTO(id, value));
        else
            _clipboardItems.AddRange(_selectedItems.Select(x => new ResourceTaskItemDTO(x.Id, x.Value)));

        ResetSelectionCore();

        ClipboardMode = mode;
        ClipboardType = type;
        ClipboardSourceCalculationId = calculationId;

        NotifyStateChanged(before, notifySelection: true);
    }

    private SelectionSnapshot CaptureSelection()
    {
        HashSet<int> ids = [];
        for (int i = 0; i < _selectedItems.Count; i++)
            ids.Add(_selectedItems[i].Id);

        return new SelectionSnapshot(SelectedType, ids);
    }

    private void NotifyStateChanged(SelectionSnapshot before, bool notifySelection)
    {
        if (notifySelection)
        {
            var selectionChange = BuildSelectionChangedArgs(before, CaptureSelection());
            if (selectionChange.HasChanges)
                SelectionChanged?.Invoke(selectionChange);
        }

        Changed?.Invoke();
    }

    private static SelectionChangedEventArgs BuildSelectionChangedArgs(SelectionSnapshot before, SelectionSnapshot after)
    {
        if (before.Type == after.Type)
        {
            if (before.Type is null)
                return SelectionChangedEventArgs.None;

            before.Ids.SymmetricExceptWith(after.Ids);
            if (before.Ids.Count == 0)
                return SelectionChangedEventArgs.None;

            return before.Type == CalculationItemType.task
                ? new SelectionChangedEventArgs(before.Ids, new HashSet<int>())
                : new SelectionChangedEventArgs(new HashSet<int>(), before.Ids);
        }

        HashSet<int> taskIds = [];
        HashSet<int> resourceIds = [];

        AddIds(taskIds, resourceIds, before.Type, before.Ids);
        AddIds(taskIds, resourceIds, after.Type, after.Ids);

        return (taskIds.Count == 0 && resourceIds.Count == 0)
            ? SelectionChangedEventArgs.None
            : new SelectionChangedEventArgs(taskIds, resourceIds);
    }

    private static void AddIds(
        HashSet<int> taskIds,
        HashSet<int> resourceIds,
        CalculationItemType? type,
        IEnumerable<int> ids)
    {
        if (type == CalculationItemType.task)
        {
            foreach (var id in ids)
                taskIds.Add(id);
            return;
        }

        if (type != CalculationItemType.resource)
            return;

        foreach (var id in ids)
            resourceIds.Add(id);
    }
}
