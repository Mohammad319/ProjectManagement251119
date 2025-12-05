using ProjectManagement.Shared.DTO.ProjectAppStorage;
using System;
using System.Collections.Generic;
using System.Text;

namespace TaskResourceBlueprints.Entities.Resources
{
    public class ResourceAttribute
    {
        public int Id { get; set; }
        public int AttributeSetId { get; set; }
        public ResourceAttributeSet? AttributeSet { get; set; }

        public string DisplayName { get; set; } = string.Empty;
        public bool IsUserEditable { get; set; } = true;
        public DataType DataType { get; set; } = DataType.Text;
        public string? DefaultTextValue { get; set; }
        public double? DefaultNumericValue { get; set; }
        public double? StepValue { get; set; }
        public double? MaxNumericValue { get; set; }
        public List<ResourceAttributeValue> Values { get; set; } = [];
    }

}
