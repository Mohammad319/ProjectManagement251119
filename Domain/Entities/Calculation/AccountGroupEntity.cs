using Domain.Entities.Base;
using ProjectManagement.Shared.Constant;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities.Calculation
{
    public sealed class AccountGroupEntity : AuditableEntity<int>
    {
        [Required, MaxLength(FieldLengths.Name)]
        public string Name { get; private set; } = string.Empty;

        public ICollection<AccountEntity> Accounts { get; private set; } = [];

        private AccountGroupEntity() { }

        public AccountGroupEntity(string name)
        {
            SetName(name);
        }

        public void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ValidationException("Account group name is required.");
            Name = name.Trim();
        }

        public void Update(string name) => SetName(name);
    }
}
