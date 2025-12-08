using Domain.Entities.Base;
using Domain.Entities.Calculation;
using Domain.Entities.Folder;
using ProjectManagement.Shared.Base.Organisation;
using ProjectManagement.Shared.Constant;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Organisation
{
    public class OrganisationCategoryEntity : IDataKeyFilterReadOnly
    {
        public int Id { get; set; }
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(ResLocalize))]
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(ResLocalize))]
        public string Name { get; set; }
        [JsonIgnore] public int TenantId { get; set; }
        [JsonIgnore] public int? CategoryId { get; set; }
        [JsonIgnore] public OrganisationCategoryEntity Category { get; set; }
        public ICollection<OrganisationCategoryEntity> Categories { get; set; }
        [JsonIgnore] public ICollection<OrganisationEntity> Organisations { get; set; }
    }
}
