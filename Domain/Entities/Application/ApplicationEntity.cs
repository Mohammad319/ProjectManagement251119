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
        public List<AttributeBase> Attributes { get; set; } = [];
    }

    public class ApplicationDataEntity : ApplicationDataBase
    {
        public List<RowEntity> Rows { get; set; } = []; // كان Row
    }

    /// <summary>
    /// Application (form) created by a department.
    /// </summary>
    public sealed class ApplicationEntity : ApplicationBase, IDataKeyFilterReadOnly
    {
        [Key]
        public int Id { get; set; }

        private ApplicationDataEntity _data = new();

        /// <summary>
        /// Dynamic data associated with the application.
        /// </summary>
        public ApplicationDataEntity Data
        {
            get => _data;
            set => _data = value ?? new ApplicationDataEntity();
        }

        /// <summary>
        /// Department responsible for this application.
        /// </summary>
        public int DepartmentId { get; set; }

        [JsonIgnore]
        public DepartmentEntity Department { get; set; } = null!;

        [JsonIgnore]
        public int TenantId { get; set; }
    }
}
