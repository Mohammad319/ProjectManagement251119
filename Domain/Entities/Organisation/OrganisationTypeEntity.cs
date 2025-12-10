using Domain.Entities.Base;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Resource;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Organisation
{
        public sealed class OrganisationTypeEntity : AuditableEntity<int>
        {
            [Required, MaxLength(FieldLengths.Name)]
        public string Name { get; set; } = string.Empty;

        public bool IsVisible { get; set; } = true;

            [JsonIgnore]
            public ICollection<OrganisationEntity> Organisations { get; set; } = [];
        }
    }
