using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Base.Organisation;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using ProjectManagement.Shared.DTO.App;
using System;

namespace ProjectManagement.Shared.DTO.Organisation
{
    public class OrganisationData
    {
        [Url(ErrorMessageResourceName = ErrorsMessages.URL, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [MaxLength(250, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string URL { get; set; }
        [EmailAddress(ErrorMessageResourceName = ErrorsMessages.EmailAddress, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [MaxLength(250, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Email { get; set; }
        [Range(0, 9999, ErrorMessageResourceName = ErrorsMessages.Range, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public double? Rating { get; set; }
        public DateTime? VerificationDate { get; set; } = null;
        public DateTime? InvoiceVerificationDate { get; set; } = null;
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Status { get; set; }
        [Range(0, int.MaxValue, ErrorMessageResourceName = ErrorsMessages.Range, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public int? NumberOfWorkersCards { get; set; }
        public YesNoUnkown SocialLaborAgreement { get; set; }
        public YesNoUnkown QualitySystems { get; set; }
        public YesNoUnkown EnvironmentalSystems { get; set; }
        public string PIDNumber { get; set; }
        public string IDNumber { get; set; }
        [Phone(ErrorMessage = ErrorsMessages.Phone), DataType(DataType.PhoneNumber, ErrorMessage = ErrorsMessages.Phone)]
        [MaxLength(30, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Phone { get; set; }
        [Phone(ErrorMessage = ErrorsMessages.Phone), DataType(DataType.PhoneNumber, ErrorMessage = ErrorsMessages.Phone)]
        [MaxLength(30, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Mobile { get; set; }
        public List<string> Notes { get; set; } = [];
        public List<AddressDTO> Address { get; set; } = [];
        public List<UnderContactOrganisationBase> Contacts { get; set; } = [];
    }
    public class PostOrganisationDTO : OrganisationBaseData
    {
        [Required, Range(1, int.MaxValue, ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public int CategoryId { get; set; }
        public bool IsVisible { get; set; } = true;
        public int? OrganisationTypeID { get; set; }
    }
    public class OrganisationDetailsDTO : OrganisationBaseData
    {
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public string Type { get; set; }

    }
    public class OrganisationBaseData : OrganisationBase
    {
        [Url(ErrorMessageResourceName = ErrorsMessages.URL, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [MaxLength(250, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string URL { get; set; }
        [EmailAddress(ErrorMessageResourceName = ErrorsMessages.EmailAddress, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [MaxLength(250, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Email { get; set; }
        [Range(0, 9999, ErrorMessageResourceName = ErrorsMessages.Range, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public double? Rating { get; set; }
        public DateTime? VerificationDate { get; set; } = null;
        public DateTime? InvoiceVerificationDate { get; set; } = null;
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Status { get; set; }
        [Range(0, int.MaxValue, ErrorMessageResourceName = ErrorsMessages.Range, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public int? NumberOfWorkersCards { get; set; }
        public YesNoUnkown SocialLaborAgreement { get; set; }
        public YesNoUnkown QualitySystems { get; set; }
        public YesNoUnkown EnvironmentalSystems { get; set; }
        public string PIDNumber { get; set; }
        public string IDNumber { get; set; }
        [Phone(ErrorMessage = ErrorsMessages.Phone), DataType(DataType.PhoneNumber, ErrorMessage = ErrorsMessages.Phone)]
        [MaxLength(30, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Phone { get; set; }
        [Phone(ErrorMessage = ErrorsMessages.Phone), DataType(DataType.PhoneNumber, ErrorMessage = ErrorsMessages.Phone)]
        [MaxLength(30, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Mobile { get; set; }
        public List<string> Notes { get; set; } = [];
        public List<AddressDTO> Address { get; set; } = [];
        public List<UnderContactOrganisationBase> Contacts { get; set; } = [];
    }
    public class ShortListOrganisationDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public string Type { get; set; }
        public string Contacts{ get; set; }
    }
}
