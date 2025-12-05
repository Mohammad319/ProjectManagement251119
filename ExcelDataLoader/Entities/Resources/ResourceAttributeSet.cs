namespace TaskResourceBlueprints.Entities.Resources
{

    public class ResourceAttributeSet
    {
        public int Id { get; set; }

        /// <summary>اسم مجموعة الخصائص (Attributes) لهذا النوع من الموارد.</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>الخصائص (Attributes) المعرفة داخل هذه المجموعة.</summary>
        public List<ResourceAttribute> Attributes { get; set; } = [];
    }
}
