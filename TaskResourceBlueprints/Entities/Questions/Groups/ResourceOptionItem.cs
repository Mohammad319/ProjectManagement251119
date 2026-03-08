namespace TaskResourceBlueprints.Entities.Questions.Groups
{
    public class ResourceOptionItem
    {
        /// <summary>
        /// Unique identifier for this selector item (resource option).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Sort order used to keep selector item ordering stable across saves.
        /// </summary>
        public int SortOrder { get; set; } = 0;

        /// <summary>
        /// The resource selector group that this option belongs to.
        /// </summary>
        public int SelectorId { get; set; }

        /// <summary>
        /// Reference to the parent selector group.
        /// </summary>
        public ResourceSelectorDefinition Selector { get; set; } = null!;

        /// <summary>
        /// The resource associated with this selector option.
        /// </summary>
        public int ResourceId { get; set; }

        /// <summary>
        /// The actual resource represented by this option.
        /// </summary>
        public ResourceDefinition Resource { get; set; } = null!;
    }

}
