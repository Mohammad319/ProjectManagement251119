namespace ProjectImportHub.Entities
{
    /// <summary>
    /// Represents a hierarchical category used to organize resources.
    /// </summary>
    public class ResourceCategory
    {
        /// <summary>
        /// Unique identifier for the resource category.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Display name shown to users for this category.
        /// </summary>
        public required string DisplayName { get; set; }

        /// <summary>
        /// Optional internal note describing this category.
        /// </summary>
        public string? Note { get; set; }

        /// <summary>
        /// Indicates whether this category is visible in selection lists.
        /// </summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// Sort order used to arrange categories visually.
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// Parent category in the hierarchy (if any).
        /// </summary>
        public int? ParentCategoryId { get; set; }

        /// <summary>
        /// Navigation reference to the parent category.
        /// </summary>
        public ResourceCategory? ParentCategory { get; set; }

        /// <summary>
        /// List of resources assigned to this category.
        /// </summary>
        public List<ResourceDefinition> Resources { get; set; } = [];
    }
}
