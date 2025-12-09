using Domain.Entities.Base;
using ProjectManagement.Shared.Constant;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities.Calculation
{
    public sealed class AccountGroupEntity : AuditableEntity<int>
    {
        [Required, MaxLength(FieldLengths.Name)]
        public required string Name { get; set; }

        /// <summary>
        /// All accounts belonging to this group.
        /// </summary>
        public ICollection<AccountEntity> Accounts { get; set; } = [];
    }
}
