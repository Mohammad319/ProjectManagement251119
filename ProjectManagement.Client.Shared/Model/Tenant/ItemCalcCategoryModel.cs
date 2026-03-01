using ProjectManagement.Shared.Base.AppTenant.Storage;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Tenant;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace ProjectManagement.Client.Shared.Model.Tenant
{
    public class TaskStoragePostModel : TaskStorageAppDTO
    {
        public List<ResGroupPostModel> Groups { get; set; } = [];
    }
    public class ResGroupPostModel : ItemCategoryBase
    {
        public int Id { get; set; }
        [JsonIgnore] public bool Colspan = false;
        public List<ResourcePostDTO> Resources { get; set; } = [];
        [JsonIgnore] public List<ResourcePostDTO> ResourcesSelected { get; set; } = [];

        public void AddRemove(ResourcePostDTO res)
        {
            if (ResourcesSelected.Any(x => x.Id == res.Id)) ResourcesSelected.Remove(res);
            else ResourcesSelected.Add(res);
        }
        public string GetClass(int id) => "oi oi-" + (ResourcesSelected.Any(x => x.Id == id)
            ? "minus text-danger" : "plus text-primary");
    }

}
