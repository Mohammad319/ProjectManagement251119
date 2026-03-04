using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Shared.Base.Organisation
{
    //# ID Number, Person/Organisation nr., Person/Organisation Type (a droplist,
    //admin can add the values CustomerGroup), Address (street, post number, city, country), 
    public enum YesNoUnkown { yes, no, unkown }
    public class OrganisationBase
    {
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Name { get; set; } = string.Empty;
    }
}