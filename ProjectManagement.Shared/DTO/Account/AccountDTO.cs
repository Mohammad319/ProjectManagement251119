using ProjectManagement.Shared.Base.Account;
using ProjectManagement.Shared.Constant;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Shared.DTO.Account
{
    public class AccountData
    {
        public List<string> Comments { get; set; } = [];
    }
    public class PostAccountDTO
    {
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [MaxLength(20, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Account { get; set; }
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Name { get; set; }
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
    public class AccountManageDTO
    {
        public int Id { get; init; }
        public string Name { get; init; }
        public string Code { get; init; }
        public bool IsVisible { get; init; } = true;
        private AccountData? _metadata;
        public AccountData Metadata
        {
            get => _metadata ??= new();
            set => _metadata = value;
        }
    }
}
