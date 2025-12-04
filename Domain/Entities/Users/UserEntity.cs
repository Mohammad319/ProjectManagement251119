using Domain.Entities.Base;
using Domain.Entities.Calculation;
using Domain.Entities.Folder;
using Domain.Entities.Project;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Users
{
    public class UserEntity: IDataKeyFilterReadOnly
    {
        public int Id { get; set; }
        public string IdAuth { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public int? DepartmentId { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public DepartmentEntity Department { get; set; }
        public ICollection<FolderEntity> Folders { get; set; }
        public ICollection<ProjectEntity> Projects { get; set; }
        public ICollection<CalculationEntity> Calculations { get; set; }
        [JsonIgnore] public int TenantId { get; set; }
    }
}
