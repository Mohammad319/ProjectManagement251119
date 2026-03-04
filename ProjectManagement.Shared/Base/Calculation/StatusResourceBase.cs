using ProjectManagement.Shared.Constant;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Shared.Base.Calculation
{
    public class StatusResourceBase
    {
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Name { get; set; } = string.Empty;
        [StringLength(7, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize), MinimumLength = 7)]
        public string Color { get; set; } = "#00ff00";
        public int Order { get; set; }
    }
}
