using ProjectManagement.Shared.Constant;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Shared.Base.Project
{
    public class FolderBase
    {
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [MaxLength(50, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Name { get; set; } = string.Empty;

        [StringLength(7, ErrorMessageResourceName = ErrorsMessages.StringLength, ErrorMessageResourceType = typeof(Resource.ResLocalize), MinimumLength = 7)]
        public string Color { get; set; } = "#08bf66";
        public double Order { get; set; }
        public bool IsVisible { get; set; } = true;
    }
}
