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
        public string FirstName { get; set; }
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [MaxLength(60, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string LastName { get; set; }
        [EmailAddress(ErrorMessageResourceName = ErrorsMessages.EmailAddress, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [MaxLength(250, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Email { get; set; }
        [Phone(ErrorMessage = ErrorsMessages.Phone), DataType(DataType.PhoneNumber, ErrorMessage = ErrorsMessages.Phone)]
        [MaxLength(30, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Telefone { get; set; }
        [Phone(ErrorMessage = ErrorsMessages.Phone), DataType(DataType.PhoneNumber, ErrorMessage = ErrorsMessages.Phone)]
        [MaxLength(30, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Mobile { get; set; }
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Department { get; set; }
        public string Note { get; set; }

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
