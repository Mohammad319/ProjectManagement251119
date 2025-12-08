using Domain.Entities.Base;
using Domain.Entities.Folder;
using Domain.Entities.Users;
using ProjectManagement.Shared.Base.Organisation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Resource;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Organisation
{
    public class OrganisationTypeEntity :  IDataKeyFilterReadOnly
    {
        public int Id { get; set; }
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(ResLocalize))]
        [MaxLength(50, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(ResLocalize))]
        public string Name { get; set; }
        public bool IsVisible { get; set; } = true;
        [JsonIgnore] public int TenantId { get; set; }
        public ICollection<OrganisationEntity> Organisations { get; set; }

        //public int PMCustomerId { get; set; }
        //public PMCustomerEntity PMCustomer { get; set; }
    }
}
