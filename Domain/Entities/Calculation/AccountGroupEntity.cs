using Domain.Entities.Base;
using Domain.Entities.Folder;
using ProjectManagement.Shared.Base.Account;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    public class AccountGroupEntity : AccountGroupBase, IDataKeyFilterReadOnly
    {
        [Key]public int Id { get; set; }
        [JsonIgnore] public int TenantId { get; set; }
        public List<AccountEntity> Accounts { get; set; }
    }
}
