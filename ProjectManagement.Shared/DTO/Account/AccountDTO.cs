using ProjectManagement.Shared.Base.Account;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Helper;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProjectManagement.Shared.DTO.Account
{
    public class AccountData
    {
        public List<string> Comments { get; set; } = [];

        public AccountData Clone()
        {
            return new AccountData
            {
                Comments = MetadataCloneHelper.CloneStrings(Comments)
            };
        }
    }

    public class PostAccountDTO
    {
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [MaxLength(20, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Account { get; set; } = string.Empty;

        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Name { get; set; } = string.Empty;

        public bool IsVisible { get; set; } = true;
        public int AccountGroupId { get; set; }

        private AccountData? data = new();

        public AccountData Data
        {
            get
            {
                data ??= new AccountData();
                return data;
            }
            set => data = value?.Clone() ?? new AccountData();
        }
    }

    public class ListAccountDTO
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Account { get; init; } = string.Empty;
    }

    public class AccountManageDTO
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Code { get; init; } = string.Empty;
        public bool IsVisible { get; init; } = true;

        private AccountData? metadata;

        public AccountData Metadata
        {
            get
            {
                metadata ??= new AccountData();
                return metadata;
            }
            set => metadata = value?.Clone() ?? new AccountData();
        }

        [JsonIgnore]
        public AccountData Data
        {
            get => Metadata;
            set => Metadata = value;
        }
    }
}
