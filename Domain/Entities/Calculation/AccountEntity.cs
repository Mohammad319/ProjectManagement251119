using Domain.Entities.Base;
using Domain.Entities.Folder;
using Domain.Entities.ResourceType;
using ProjectManagement.Shared.Base.Account;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Account;
using ProjectManagement.Shared.Resource;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    public class AccountEntity : IDataKeyFilterReadOnly
    {
        [Key] public int Id { get; set; }
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(ResLocalize))]
        [MaxLength(20, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(ResLocalize))]
        public string Account { get; set; }
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(ResLocalize))]
        public string Name { get; set; }

        public DateTime LastUpdate { get; set; } = DateTime.Now;
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
