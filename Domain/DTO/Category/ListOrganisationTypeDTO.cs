using Domain.Entities.Organisation;
using ProjectManagement.Shared.Constant;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json.Serialization;

namespace Domain.DTO.Category
{
    public class ListOrganisationTypeDTO
    {
        public int Id { get; set; } = default!;

        [Required, MaxLength(FieldLengths.Name)]
        public string Name { get; set; } = string.Empty;

        public bool IsVisible { get; set; } = true;

        [JsonIgnore]
        public ICollection<OrganisationEntity> Organisations { get; private set; } = [];

    }
}
