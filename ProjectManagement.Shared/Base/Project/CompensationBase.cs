using ProjectManagement.Shared.Constant;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Shared.Base.Project
{
    public class CompensationBase
    {
        public int Order { get; set; }
        public bool IsVisible { get; set; } = true;
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Name { get; set; }
        [StringLength(7, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize), MinimumLength = 7)]
        public string Color { get; set; } = "#00ff00";
    }
}
