using Domain.Entities.Base;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Resource;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Organisation
{
        public sealed class OrganisationTypeEntity : AuditableEntity<int>
        {
            [Required(
                ErrorMessageResourceName = ErrorsMessages.FieldIsRequred,
                ErrorMessageResourceType = typeof(ResLocalize))]
            [MaxLength(
                50,
                ErrorMessageResourceName = ErrorsMessages.MaxLength,
                ErrorMessageResourceType = typeof(ResLocalize))]
            public string Name { get; set; } = string.Empty;

            public bool IsVisible { get; set; } = true;

            [JsonIgnore]
            public ICollection<OrganisationEntity> Organisations { get; set; } = [];
        }
    }
