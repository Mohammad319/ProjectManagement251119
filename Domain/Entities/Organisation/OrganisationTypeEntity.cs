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
            public required string Name { get; set; }

            public bool IsVisible { get; set; } = true;

            [JsonIgnore]
            public ICollection<OrganisationEntity> Organisations { get; set; } = [];
        }
    }
