using Domain.Entities.Base;
using Domain.Entities.Calculation;
using Domain.Entities.Folder;
using ProjectManagement.Shared.Base.Organisation;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Domain.Entities.Organisation
{
    public class OrganisationCategoryEntity : OrganisationCategoryBase, IDataKeyFilterReadOnly
    {
        public int Id { get; set; }
        [JsonIgnore] public int TenantId { get; set; }
        [JsonIgnore] public int? CategoryId { get; set; }
        [JsonIgnore] public OrganisationCategoryEntity Category { get; set; }
        public ICollection<OrganisationCategoryEntity> Categories { get; set; }
        [JsonIgnore] public ICollection<OrganisationEntity> Organisations { get; set; }
    }
}
