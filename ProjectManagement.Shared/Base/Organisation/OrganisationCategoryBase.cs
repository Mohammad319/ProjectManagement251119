using ProjectManagement.Shared.Constant;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Shared.Base.Organisation
{
    public class OrganisationCategoryBase
    {
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Name { get; set; } = string.Empty;
    }
}
