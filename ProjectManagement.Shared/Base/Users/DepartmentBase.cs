using ProjectManagement.Shared.Constant;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Shared.Base.Users
{
    public class DepartmentBase
    {
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [MaxLength(60, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Name { get; set; } = string.Empty;
        [MaxLength(500, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Description { get; set; } = string.Empty;
    }
}
