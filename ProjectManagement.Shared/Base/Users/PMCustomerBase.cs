using ProjectManagement.Shared.Constant;
using System;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Shared.Base.Users
{
    public class PMCustomerBase
    {
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Name { get; set; } = string.Empty;
        [Phone(ErrorMessageResourceName = ErrorsMessages.Phone, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Phone { get; set; } = string.Empty;
        [Phone(ErrorMessageResourceName = ErrorsMessages.Phone, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Mobile { get; set; } = string.Empty;
        public string Fax { get; set; } = string.Empty;
        [EmailAddress(ErrorMessageResourceName = ErrorsMessages.EmailAddress, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Email { get; set; } = string.Empty;
        [Url(ErrorMessage = ErrorsMessages.URL), DataType(DataType.Url, ErrorMessage = ErrorsMessages.URL)]
        public string Website { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string PostCode { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string BuildNumber { get; set; } = string.Empty;
        public DateTimeOffset? DateExpire { get; set; }
        public int MaxUsers { get; set; } = 3;
        public int MaxCalculations { get; set; } = 100;
    }
}
