using Domain.Entities.Base;
using Domain.Entities.Calculation;
using Domain.Entities.Users;
using ProjectManagement.Shared.Base.Project;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Resource;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Project
{
    public class StatusEntity :  IDataKeyFilterReadOnly
    {
        public int Id { get; set; }
        public int Order { get; set; }
        public bool IsVisible { get; set; } = true;
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(ResLocalize))]
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(ResLocalize))]
        public string Name { get; set; }
        [StringLength(7, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(ResLocalize), MinimumLength = 7)]
        public string Color { get; set; } = "#00ff00";
        [JsonIgnore] public int TenantId { get; set; }
        public ICollection<CalculationEntity> Calculations { get; set; }

    }
}
