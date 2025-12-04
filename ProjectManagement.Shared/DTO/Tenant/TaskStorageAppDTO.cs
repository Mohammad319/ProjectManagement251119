using ProjectManagement.Shared.Base.AppTenant.Storage;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.Enums;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ProjectManagement.Shared.DTO.Tenant
{
    public class TaskStorageAppDTO : SGT_StorageAppBase
    {
        [JsonIgnore] public bool Colspan = false;
        public int Id { get; set; }
        public StorageSort StorageSort { get; set; } = StorageSort.Construction;
        public bool IsVisible { get; set; } = true;
        public int VersionId { get; set; }
        public int? TaskId { get; set; }
        //public List<TaskStorageAppModel> Tasks { get; set; }
        public List<string> KeyWord { get; set; } = [];
        public List<int> GroupIds { get; set; }
        public List<ResourceStorageListDTO> Resources { get; set; }
    }

}
