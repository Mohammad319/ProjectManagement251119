namespace Application.Feature.Application
{
    public sealed class ApplicationListItemDto
    {
        public int Id { get; set; }
        public int DepartmentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsVisible { get; set; }
        public int UserId { get; set; }
        public DateTime LastUpdate { get; set; }
        public string Description { get; set; } = string.Empty;
        public int RowCount { get; set; }
    }

    public sealed class ApplicationValueListItemDto
    {
        public int Id { get; set; }
        public int CalculationId { get; set; }
        public int ApplicationId { get; set; }
        public string ApplicationName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Responsible { get; set; } = string.Empty;
        public int UserId { get; set; }
        public DateTime LastUpdate { get; set; }
        public int AttributeValueCount { get; set; }
    }
}
