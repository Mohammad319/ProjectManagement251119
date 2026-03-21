using ProjectManagement.Shared.Base.Application;

namespace ProjectManagement.Shared.DTO.App
{
    public class AttributeDTO : AttributeBase
    {
        public string Value { get; set; } = string.Empty;

        public AttributeDTO Clone()
        {
            return new AttributeDTO
            {
                ID = ID,
                AttributeType = AttributeType,
                Required = Required,
                Order = Order,
                Validation = Validation ?? string.Empty,
                Style = Style ?? string.Empty,
                Value = Value ?? string.Empty
            };
        }
    }

    public class RowDTO : RowBase
    {
        public List<AttributeDTO> Attributes { get; set; } = [];

        public RowDTO Clone()
        {
            return new RowDTO
            {
                ID = ID,
                Name = Name ?? string.Empty,
                Description = Description ?? string.Empty,
                Style = Style ?? string.Empty,
                StyleRow = StyleRow ?? string.Empty,
                IsVisible = IsVisible,
                Attributes = Attributes?.Select(x => x.Clone()).ToList() ?? []
            };
        }
    }

    public class ApplicationDataDTO : ApplicationDataBase
    {
        public List<RowDTO> Rows { get; set; } = [];

        public ApplicationDataDTO Clone()
        {
            return new ApplicationDataDTO
            {
                Description = Description ?? string.Empty,
                Rows = Rows?.Select(x => x.Clone()).ToList() ?? []
            };
        }
    }

    public class ApplicationDTO : ApplicationBase
    {
        private ApplicationDataDTO? data = new();

        public int Id { get; set; }
        public int DepartmentId { get; set; }

        public ApplicationDataDTO Data
        {
            get
            {
                data ??= new ApplicationDataDTO();
                return data;
            }
            set => data = value?.Clone() ?? new ApplicationDataDTO();
        }
    }

    public class ApplicationValuesDTO : ApplicationValuesBase
    {
        public int Id { get; set; }
        public int CalculationId { get; set; }
        public int ApplicationId { get; set; }
        public ApplicationDTO? Application { get; set; }
    }
}
