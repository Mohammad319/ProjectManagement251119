using ProjectManagement.Shared.Constant;
using System;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Shared.Base.Application
{
    public enum AttributeType
    {
        Text, Int, Double, Date, Time, DateTime, Bool, Char, TextArea, Select
    }
    public class AttributeBase
    {
        public Guid ID { get; set; }

        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public AttributeType AttributeType { get; set; } = AttributeType.Text;
        public bool Required { get; set; }
        public int Order { get; set; }
        public string Validation { get; set; } = "";
        public string Style { get; set; } = string.Empty;
    }
    public class RowBase
    {
        public Guid ID { get; set; }
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Style { get; set; } = string.Empty;
        public string StyleRow { get; set; } = string.Empty;
        public bool IsVisible { get; set; } = true;
    }
}
