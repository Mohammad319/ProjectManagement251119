using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace ProjectManagement.Shared.Base.AppTenant.Storage
{
    public class SGT_StorageAppBase
    {
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; }
        TaskMetadata data = new();
        public TaskMetadata Data { get { data ??= new TaskMetadata(); return data; } set { data = value; } }
    }
}
