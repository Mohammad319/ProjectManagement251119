using Domain.Entities.Base;
using Domain.Entities.Folder;
using Domain.Entities.ResourceType;
using ProjectManagement.Shared.Base.Account;
using ProjectManagement.Shared.DTO.Account;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    public class AccountEntity : AccountBase, IDataKeyFilterReadOnly
    {
        [Key] public int Id { get; set; }
        public bool IsVisible { get; set; } = true;
        public int AccountGroupId { get; set; }
        AccountData data;
        public AccountData Data { get { data ??= new AccountData(); return data; } set { data = value; } }

        [JsonIgnore] public int TenantId { get; set; }
        [JsonIgnore][ForeignKey(nameof(AccountGroupId))] public AccountGroupEntity AccountGroup { get; set; }
        [JsonIgnore] public ICollection<ResourceEntity> Resources { get; set; }
        [JsonIgnore] public ICollection<ResourceTypeEntity> ResourcesType { get; set; }
        [JsonIgnore] public ICollection<ResourceSortEntity> ResourcesSort { get; set; }
    }
}
