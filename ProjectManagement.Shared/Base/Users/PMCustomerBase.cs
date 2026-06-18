using ProjectManagement.Shared.Constant;
using System;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Shared.Base.Users
{
    public class PMCustomerBase
    {
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public  string Name { get; set; } = string.Empty;
        [Phone(ErrorMessageResourceName = ErrorsMessages.Phone, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string? Phone { get; set; }
        [Phone(ErrorMessageResourceName = ErrorsMessages.Phone, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string? Mobile { get; set; }
        public string? Fax { get; set; } 
        [EmailAddress(ErrorMessageResourceName = ErrorsMessages.EmailAddress, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string? Email { get; set; } 
        [Url(ErrorMessage = ErrorsMessages.URL), DataType(DataType.Url, ErrorMessage = ErrorsMessages.URL)]
        public string? Website { get; set; } 
        public string? Note { get; set; }
        public string? Country { get; set; } 
        public string? City { get; set; } 
        public string? PostCode { get; set; } 
        public string? Street { get; set; } 
        public string? BuildNumber { get; set; } 
        public DateTimeOffset? DateExpire { get; set; }
        [Range(1, int.MaxValue, ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public  int MaxUsers { get; set; } = 5;
    }
}
