using ProjectManagement.Shared.DTO.Account;
using ProjectManagement.Shared.DTO.ResourceType;
using System.Collections.Generic;

namespace ProjectManagement.Shared.DTO.General
{
    public class AccountGroupsListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<ListAccountDTO> Accounts { get; set; } = [];
    }
    public class ResourceFormDTO
    {
        public List<ListDTO> Statues { get; set; } = [];
        public List<ListResourceTypeDTO> ResourceTypes { get; set; } = [];
        public List<AccountGroupsListDto> AccountGroups { get; set; } = [];
    }
}
