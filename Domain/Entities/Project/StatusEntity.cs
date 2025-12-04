using Domain.Entities.Base;
using Domain.Entities.Calculation;
using Domain.Entities.Users;
using ProjectManagement.Shared.Base.Project;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Domain.Entities.Project
{
    public class StatusEntity : StatusBase, IDataKeyFilterReadOnly
    {
        public int Id { get; set; }
        [JsonIgnore] public int TenantId { get; set; }
        public ICollection<CalculationEntity> Calculations { get; set; }

    }
}
