using ProjectManagement.Shared.Base.Organisation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.App;
using ProjectManagement.Shared.Helper;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProjectManagement.Shared.DTO.Organisation
{
    public class OrganisationData
    {
        [Url(ErrorMessageResourceName = ErrorsMessages.URL, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [MaxLength(250, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string URL { get; set; } = string.Empty;

        [EmailAddress(ErrorMessageResourceName = ErrorsMessages.EmailAddress, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [MaxLength(250, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Email { get; set; } = string.Empty;

        [Range(0, 9999, ErrorMessageResourceName = ErrorsMessages.Range, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public double? Rating { get; set; }

        public DateTime? VerificationDate { get; set; }
        public DateTime? InvoiceVerificationDate { get; set; }

        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Status { get; set; } = string.Empty;

        [Range(0, int.MaxValue, ErrorMessageResourceName = ErrorsMessages.Range, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public int? NumberOfWorkersCards { get; set; }

        public YesNoUnkown SocialLaborAgreement { get; set; } = YesNoUnkown.notSpecified;
        public YesNoUnkown QualitySystems { get; set; } = YesNoUnkown.notSpecified;
        public YesNoUnkown EnvironmentalSystems { get; set; } = YesNoUnkown.notSpecified;
        public string WarningReason { get; set; } = string.Empty;
        public string PIDNumber { get; set; } = string.Empty;
        public string IDNumber { get; set; } = string.Empty;

        /// <summary>Person-/organisationstyp as a fixed catalog value (see <see cref="OrganisationTypeCatalog"/>).
        /// Stored in the metadata JSON so no schema change is needed; replaces the legacy free-form
        /// <c>OrganisationTypeId</c> lookup that let tenants seed nonsense values like city names.</summary>
        public string OrganisationType { get; set; } = string.Empty;

        [Phone(ErrorMessage = ErrorsMessages.Phone), DataType(DataType.PhoneNumber, ErrorMessage = ErrorsMessages.Phone)]
        [MaxLength(30, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Phone { get; set; } = string.Empty;

        [Phone(ErrorMessage = ErrorsMessages.Phone), DataType(DataType.PhoneNumber, ErrorMessage = ErrorsMessages.Phone)]
        [MaxLength(30, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Mobile { get; set; } = string.Empty;

        public List<string> Notes { get; set; } = [];
        public List<AddressDTO> Address { get; set; } = [];
        public List<UnderContactOrganisationBase> Contacts { get; set; } = [];

        public OrganisationData Clone()
        {
            return new OrganisationData
            {
                URL = MetadataCloneHelper.CopyText(URL),
                Email = MetadataCloneHelper.CopyText(Email),
                Rating = Rating,
                VerificationDate = VerificationDate,
                InvoiceVerificationDate = InvoiceVerificationDate,
                Status = MetadataCloneHelper.CopyText(Status),
                NumberOfWorkersCards = NumberOfWorkersCards,
                SocialLaborAgreement = SocialLaborAgreement,
                QualitySystems = QualitySystems,
                EnvironmentalSystems = EnvironmentalSystems,
                WarningReason = MetadataCloneHelper.CopyText(WarningReason),
                PIDNumber = MetadataCloneHelper.CopyText(PIDNumber),
                IDNumber = MetadataCloneHelper.CopyText(IDNumber),
                OrganisationType = MetadataCloneHelper.CopyText(OrganisationType),
                Phone = MetadataCloneHelper.CopyText(Phone),
                Mobile = MetadataCloneHelper.CopyText(Mobile),
                Notes = MetadataCloneHelper.CloneStrings(Notes),
                Address = MetadataCloneHelper.CloneAddresses(Address),
                Contacts = MetadataCloneHelper.CloneContacts(Contacts)
            };
        }
    }

    public class OrganisationBaseData : OrganisationBase
    {
        private OrganisationData? data = new();

        [JsonIgnore]
        public OrganisationData Data
        {
            get
            {
                data ??= new OrganisationData();
                return data;
            }
            set => data = value?.Clone() ?? new OrganisationData();
        }

        [Url(ErrorMessageResourceName = ErrorsMessages.URL, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [MaxLength(250, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string URL
        {
            get => Data.URL;
            set => Data.URL = MetadataCloneHelper.CopyText(value);
        }

        [EmailAddress(ErrorMessageResourceName = ErrorsMessages.EmailAddress, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [MaxLength(250, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Email
        {
            get => Data.Email;
            set => Data.Email = MetadataCloneHelper.CopyText(value);
        }

        [Range(0, 9999, ErrorMessageResourceName = ErrorsMessages.Range, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public double? Rating
        {
            get => Data.Rating;
            set => Data.Rating = value;
        }

        public DateTime? VerificationDate
        {
            get => Data.VerificationDate;
            set => Data.VerificationDate = value;
        }

        public DateTime? InvoiceVerificationDate
        {
            get => Data.InvoiceVerificationDate;
            set => Data.InvoiceVerificationDate = value;
        }

        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Status
        {
            get => Data.Status;
            set => Data.Status = MetadataCloneHelper.CopyText(value);
        }

        [MaxLength(500, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string WarningReason
        {
            get => Data.WarningReason;
            set => Data.WarningReason = MetadataCloneHelper.CopyText(value);
        }

        [Range(0, int.MaxValue, ErrorMessageResourceName = ErrorsMessages.Range, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public int? NumberOfWorkersCards
        {
            get => Data.NumberOfWorkersCards;
            set => Data.NumberOfWorkersCards = value;
        }

        public YesNoUnkown SocialLaborAgreement
        {
            get => Data.SocialLaborAgreement;
            set => Data.SocialLaborAgreement = value;
        }

        public YesNoUnkown QualitySystems
        {
            get => Data.QualitySystems;
            set => Data.QualitySystems = value;
        }

        public YesNoUnkown EnvironmentalSystems
        {
            get => Data.EnvironmentalSystems;
            set => Data.EnvironmentalSystems = value;
        }

        public string PIDNumber
        {
            get => Data.PIDNumber;
            set => Data.PIDNumber = MetadataCloneHelper.CopyText(value);
        }

        public string IDNumber
        {
            get => Data.IDNumber;
            set => Data.IDNumber = MetadataCloneHelper.CopyText(value);
        }

        public string OrganisationType
        {
            get => Data.OrganisationType;
            set => Data.OrganisationType = MetadataCloneHelper.CopyText(value);
        }

        [Phone(ErrorMessage = ErrorsMessages.Phone), DataType(DataType.PhoneNumber, ErrorMessage = ErrorsMessages.Phone)]
        [MaxLength(30, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Phone
        {
            get => Data.Phone;
            set => Data.Phone = MetadataCloneHelper.CopyText(value);
        }

        [Phone(ErrorMessage = ErrorsMessages.Phone), DataType(DataType.PhoneNumber, ErrorMessage = ErrorsMessages.Phone)]
        [MaxLength(30, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Mobile
        {
            get => Data.Mobile;
            set => Data.Mobile = MetadataCloneHelper.CopyText(value);
        }

        public List<string> Notes
        {
            get => Data.Notes;
            set => Data.Notes = MetadataCloneHelper.CloneStrings(value);
        }

        public List<AddressDTO> Address
        {
            get => Data.Address;
            set => Data.Address = MetadataCloneHelper.CloneAddresses(value);
        }

        public List<UnderContactOrganisationBase> Contacts
        {
            get => Data.Contacts;
            set => Data.Contacts = MetadataCloneHelper.CloneContacts(value);
        }
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
        public string Category { get; set; } = string.Empty;
        public string SubCategory { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    public class ShortListOrganisationDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string SubCategory { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Contacts { get; set; } = string.Empty;
    }

    /// <summary>
    /// Flat "Kunder &amp; leverantörer" table row: one company/person with the columns shown in the
    /// admin list (namn, huvudgrupp, underkategori, kontaktuppgifter, status, senast ändrad).
    /// <see cref="MainGroup"/> is the top-level category (huvudgrupp) and <see cref="SubCategory"/> the
    /// child category (underkategori); <see cref="IsUsed"/> is true when the post is referenced by a
    /// projekt/kalkyl/anbud so the UI offers Arkivera instead of Ta bort.
    /// </summary>
    public class OrganisationRowDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public int? MainGroupId { get; set; }
        public string MainGroup { get; set; } = string.Empty;

        public int? SubCategoryId { get; set; }
        public string SubCategory { get; set; } = string.Empty;

        public int? TypeId { get; set; }
        public string Type { get; set; } = string.Empty;

        public string OrganisationNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        /// <summary>false = arkiverad (visas inte som standard i nya val).</summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>Refererad av projekt/kalkyl/anbud → får inte tas bort, endast arkiveras.</summary>
        public bool IsUsed { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
