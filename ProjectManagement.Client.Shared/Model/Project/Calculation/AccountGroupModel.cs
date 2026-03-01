using ProjectManagement.Shared.Base.Account;
using System.Collections.Generic;

namespace ProjectManagement.Client.Shared.Model.Project.Calculation
{
    public class AccountGroupModel : AccountGroupBase
    {
        public int Id { get; set; }
        public List<AccountModel> Accounts { get; set; } = [];
    }
}
