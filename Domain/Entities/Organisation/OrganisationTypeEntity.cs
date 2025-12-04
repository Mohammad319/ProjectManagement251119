using Domain.Entities.Base;
using Domain.Entities.Folder;
using Domain.Entities.Users;
using ProjectManagement.Shared.Base.Organisation;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Domain.Entities.Organisation
{
    public class OrganisationTypeEntity : OrganisationTypeBase, IDataKeyFilterReadOnly
    {
        public int Id { get; set; }
        [JsonIgnore] public int TenantId { get; set; }
        public ICollection<OrganisationEntity> Organisations { get; set; }

        //public int PMCustomerId { get; set; }
        //public PMCustomerEntity PMCustomer { get; set; }
    }
}
