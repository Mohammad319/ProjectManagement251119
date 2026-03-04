using ProjectManagement.Shared.Constant;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProjectManagement.Shared.Base.Organisation
{
    public class UnderContactOrganisationBase
    {
        [JsonIgnore] public bool CommentIsVisible;
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [MaxLength(60, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string FirstName { get; set; } = string.Empty;
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [MaxLength(60, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string LastName { get; set; } = string.Empty;
        [EmailAddress(ErrorMessageResourceName = ErrorsMessages.EmailAddress, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [MaxLength(250, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Email { get; set; } = string.Empty;
        [Phone(ErrorMessage = ErrorsMessages.Phone), DataType(DataType.PhoneNumber, ErrorMessage = ErrorsMessages.Phone)]
        [MaxLength(30, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Telefone { get; set; } = string.Empty;
        [Phone(ErrorMessage = ErrorsMessages.Phone), DataType(DataType.PhoneNumber, ErrorMessage = ErrorsMessages.Phone)]
        [MaxLength(30, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Mobile { get; set; } = string.Empty;
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Department { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public UnderContactStatus Status { get; set; }

        public string GetFullName()
        {
            return FirstName + " " + LastName;
        }
    }
    public enum UnderContactStatus
    {
        Aktive, Resigned, OutOfService, Retired, Vacation
    }
}
