using Domain.Entities.Base;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Resource;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities.Calculation
{
    public sealed class AccountGroupEntity : AuditableEntity<int>
    {
        [Required(
            ErrorMessageResourceName = ErrorsMessages.FieldIsRequred,
            ErrorMessageResourceType = typeof(ResLocalize))]
        [MaxLength(
            80,
            ErrorMessageResourceName = ErrorsMessages.MaxLength,
            ErrorMessageResourceType = typeof(ResLocalize))]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// All accounts belonging to this group.
        /// </summary>
        public ICollection<AccountEntity> Accounts { get; set; } = [];
    }
}
