using Domain.Entities.Base;
using Domain.Entities.Project;
using Domain.Entities.ResourceType;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Account;
using ProjectManagement.Shared.Resource;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    public sealed class AccountEntity : AuditableEntity<int>
    {
        [Required, MaxLength(FieldLengths.Code)]
        public string Code { get; set; } = string.Empty;

        [Required, MaxLength(FieldLengths.Name)]
        public required string Name { get; set; }

        public bool IsVisible { get; set; } = true;

        public int AccountGroupId { get; set; }

        private AccountData? _metadata;
        public AccountData Metadata
        {
            get => _metadata ??= new AccountData();
            set => _metadata = value ?? new AccountData();
        }

        [JsonIgnore]
        [ForeignKey(nameof(AccountGroupId))]
        public AccountGroupEntity AccountGroup { get; set; } = null!;

        [JsonIgnore]
        public ICollection<ResourceEntity> Resources { get; set; } = [];

        [JsonIgnore]
        public ICollection<ResourceTypeEntity> ResourceTypes { get; set; } = [];   // كان اسمها ResourcesType

        [JsonIgnore]
        public ICollection<ResourceSortEntity> ResourceSorts { get; set; } = [];   // كان اسمها ResourcesSort
    }
}
