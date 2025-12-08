using Domain.Entities.Base;
using Domain.Entities.Folder;
using ProjectManagement.Shared.Base.Account;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Resource;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    public class AccountGroupEntity : IDataKeyFilterReadOnly
    {
        [Key]public int Id { get; set; }
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(ResLocalize))]
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(ResLocalize))]
        public string Name { get; set; }
        public DateTime LastUpdate { get; set; } = DateTime.Now;
        [JsonIgnore] public int TenantId { get; set; }
        public List<AccountEntity> Accounts { get; set; }
    }
}
