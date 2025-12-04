using ProjectManagement.Shared.Base.Account;
using ProjectManagement.Shared.DTO.Account;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Client.Shared.Model.Project.Calculation
{
    public class AccountModel : AccountBase
    {
        [Key] public int Id { get; set; }
        public bool IsVisible { get; set; } = true;
        public int AccountGroupId { get; set; }
        public AccountData Data { get; set; } = new();
    }
}
