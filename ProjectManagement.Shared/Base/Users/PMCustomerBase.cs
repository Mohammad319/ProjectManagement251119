using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Helper;
using System;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Shared.Base.Users
{
    public class PMCustomerBase
    {
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Name { get; set; } = string.Empty;

        [Phone(ErrorMessageResourceName = nameof(Resource.ResLocalize.Phone), ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [MaxLength(FieldLengths.Phone, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Phone { get; set; } = string.Empty;

        [Phone(ErrorMessageResourceName = nameof(Resource.ResLocalize.Phone), ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [MaxLength(FieldLengths.Phone, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Mobile { get; set; } = string.Empty;

        [MaxLength(FieldLengths.Phone, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Fax { get; set; } = string.Empty;

        [EmailAddress(ErrorMessageResourceName = ErrorsMessages.EmailAddress, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [MaxLength(FieldLengths.Email, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Email { get; set; } = string.Empty;

        [OptionalUrl(ErrorMessageResourceName = ErrorsMessages.URL, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [MaxLength(200, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Website { get; set; } = string.Empty;

        [MaxLength(FieldLengths.Note, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Note { get; set; } = string.Empty;

        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Country { get; set; } = string.Empty;

        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string City { get; set; } = string.Empty;

        [MaxLength(20, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string PostCode { get; set; } = string.Empty;

        [MaxLength(120, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Street { get; set; } = string.Empty;

        [MaxLength(20, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string BuildNumber { get; set; } = string.Empty;
        public DateTimeOffset? DateExpire { get; set; }
        public int MaxUsers { get; set; } = 3;
        public int MaxCalculations { get; set; } = 100;
    }
}
