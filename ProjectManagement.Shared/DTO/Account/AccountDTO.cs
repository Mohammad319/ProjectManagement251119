using ProjectManagement.Shared.Base.Account;
using System.Collections.Generic;

namespace ProjectManagement.Shared.DTO.Account
{
    public class AccountData
    {
        public List<string> Comments { get; set; } = [];
    }
    public class PostAccountDTO : AccountBase
    {
        public bool IsVisible { get; set; } = true;
        public int AccountGroupId { get; set; }
        public AccountData Data { get; set; } = new();

    }

    public class ListAccountDTO
    {
        public int Id { get; init; }
        public string Name { get; init; }
        public string Account { get; init; }
    }
}
