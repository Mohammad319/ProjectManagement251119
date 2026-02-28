using ProjectManagement.Shared.Base.Account;
using System.Collections.Generic;

namespace ProjectManagement.Shared.DTO.Account
{
    public class PostAccountGroupDTO : AccountGroupBase
    {
    }
    public class PostAccountGroupWithAccountsDTO : AccountGroupBase
    {
        public List<PostAccountDTO> Accounts { get; set; } = [];
    }
    public class ListAccountGroupIncludeAccountDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<ListAccountDTO> Accounts { get; set; } = [];
    }
}
