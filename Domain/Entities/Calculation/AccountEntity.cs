using Domain.Entities.Base;
using Domain.Entities.ResourceType;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Account;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    public sealed class AccountEntity : AuditableEntity<int>
    {
        [Required, MaxLength(FieldLengths.Code)]
        public string Code { get; private set; } = string.Empty;

        [Required, MaxLength(FieldLengths.Name)]
        public string Name { get; private set; } = string.Empty;

        public bool IsVisible { get; private set; } = true;

        public int AccountGroupId { get; private set; }

        private AccountData? _metadata;
        public AccountData Metadata
        {
            get => _metadata ??= new();
            private set => _metadata = CloneMetadata(value);
        }

        [JsonIgnore, ForeignKey(nameof(AccountGroupId))]
        public AccountGroupEntity AccountGroup { get; private set; } = null!;

        [JsonIgnore]
        public ICollection<ResourceEntity> Resources { get; private set; } = [];

        [JsonIgnore]
        public ICollection<ResourceTypeEntity> ResourceTypes { get; private set; } = [];

        [JsonIgnore]
        public ICollection<ResourceSortEntity> ResourceSorts { get; private set; } = [];

        private AccountEntity() { }

        public AccountEntity(string code, string name, int accountGroupId, bool isVisible, AccountData? data)
        {
            SetCode(code);
            SetName(name);
            SetGroup(accountGroupId);
            SetVisibility(isVisible);
            Metadata = data ?? new AccountData();
        }

        public void SetCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ValidationException("Account code is required.");
            Code = code.Trim();
        }

        public void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ValidationException("Account name is required.");
            Name = name.Trim();
        }

        public void SetGroup(int groupId)
        {
            if (groupId <= 0)
                throw new ValidationException("AccountGroupId is required.");
            AccountGroupId = groupId;
        }

        public void SetVisibility(bool isVisible) => IsVisible = isVisible;

        public void Update(string code, string name, int groupId, bool isVisible, AccountData? data)
        {
            SetCode(code);
            SetName(name);
            SetGroup(groupId);
            SetVisibility(isVisible);
            Metadata = data ?? new AccountData();
        }

        public AccountData GetMetadataSnapshot()
            => CloneMetadata(_metadata);

        public void UpdateMetadata(Action<AccountData> update)
        {
            ArgumentNullException.ThrowIfNull(update);

            var snapshot = GetMetadataSnapshot();
            update(snapshot);
            Metadata = snapshot;
        }

        private static AccountData CloneMetadata(AccountData? data)
        {
            data ??= new AccountData();

            return new AccountData
            {
                Comments = data.Comments?.ToList() ?? []
            };
        }
    }
}
