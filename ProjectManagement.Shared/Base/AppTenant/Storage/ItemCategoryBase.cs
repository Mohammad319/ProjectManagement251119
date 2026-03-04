using ProjectManagement.Shared.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Shared.Base.AppTenant.Storage
{
    public class ItemCategoryBase
    {
        public string Name { get; set; } = string.Empty;
        public bool IsVisible { get; set; } = true;
        public StorageSort StorageSort { get; set; }
        [Required]public ResourceTypesEnum ResType { get; set; }

        public List<string> KeyWord { get; set; } = [];
        public List<string> HiddenKeyWord { get; set; } = [];
    }
}
