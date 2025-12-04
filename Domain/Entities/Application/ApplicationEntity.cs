using Domain.Entities.Base;
using Domain.Entities.Users;
using ProjectManagement.Shared.Base.Application;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Application
{
    public class RowEntity : RowBase
    {
        public List<AttributeBase> Attributes { get; set; }
    }
    public class ApplicationDataEntity : ApplicationDataBase
    {
        public List<RowEntity> Row { get; set; } = [];
    }
    public class ApplicationEntity : ApplicationBase, IDataKeyFilterReadOnly
    {
        [Key] public int Id { get; set; }
        ApplicationDataEntity data;
        public ApplicationDataEntity Data { get { data ??= new ApplicationDataEntity(); return data; } set { data = value; } }

        public int DepartmentId { get; set; }
        [JsonIgnore] public DepartmentEntity Department { get; set; }

        [JsonIgnore] public int TenantId { get; set; }
    }
}
