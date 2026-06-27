using Domain.Entities.Base;
using ProjectManagement.Shared.Constant;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities.Calculation
{
    public sealed class AccountGroupEntity : AuditableEntity<int>
    {
        [Required, MaxLength(FieldLengths.Name)]
        public string Name { get; private set; } = string.Empty;

        private readonly List<AccountEntity> _accounts = [];
        public IReadOnlyCollection<AccountEntity> Accounts => _accounts;
        public void AddAccount(AccountEntity account)
        {
            ArgumentNullException.ThrowIfNull(account);

            if (account.AccountGroupId != Id)
                account.SetGroup(Id);

            if (_accounts.Any(a => a.Id == account.Id))
                return;

            _accounts.Add(account);
        }

        private AccountGroupEntity() { }

        public AccountGroupEntity(string name)
        {
            SetName(name);
        }

        public void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ValidationException($"{nameof(Name)} is required.");
            var trimmed = name.Trim();

            if (Name == trimmed)
                return;

            Name = trimmed;
        }

        public void Update(string name) => SetName(name);
    }
}
