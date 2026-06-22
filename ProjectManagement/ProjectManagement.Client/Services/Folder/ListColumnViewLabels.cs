using BlazorMHD.UI.Core.Data;

namespace ProjectManagement.Client.Services.Folder;

/// <summary>
/// Swedish UI strings for the right-panel lists' shared column-view manager
/// (<see cref="MhdColumnViewManager"/>). Centralized so the project and
/// calculation lists stay in sync instead of each carrying its own copy.
/// </summary>
public static class ListColumnViewLabels
{
    public static MhdColumnViewLabels Swedish { get; } = new()
    {
        StandardViewName = "Standard",
        CustomViewLabel = "Anpassad vy",
        ModifiedLabelFormat = "{0} · ändrad",
        AlreadySavedReasonFormat = "Denna kolumnvy är redan sparad: {0}",
        NameRequiredError = "Namn krävs.",
        StandardNameReservedError = "Namnet Standard är reserverat. Välj ett annat namn.",
        NameAlreadyExistsError = "En kolumnvy med detta namn finns redan. Välj ett annat namn.",
        DuplicateContentError = "Denna kolumnvy är redan sparad.",
    };
}
