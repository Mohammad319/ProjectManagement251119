using ProjectImportHub.Entities.Tasks;

namespace ProjectImportHub.Entities.Questions.Groups
{
    public class ResourceSelectorDefinition
    {
        /// <summary>
        /// Unique identifier for the resource selector.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Display name shown to the user for this selector group.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Sorting order for displaying this selector within the task.
        /// </summary>
        public int SortOrder { get; set; } = 0;

        /// <summary>
        /// Optional UI section key used to categorize or filter this selector.
        /// </summary>
        public string? SectionKey { get; set; }

        /// <summary>
        /// The task this selector group belongs to.
        /// </summary>
        public int TaskId { get; set; }
        public TaskDefinition Task { get; set; } = null!;

        /// <summary>
        /// List of selectable resource items available within this selector.
        /// </summary>
        public List<ResourceOptionItem> Items { get; set; } = [];
    }

}
