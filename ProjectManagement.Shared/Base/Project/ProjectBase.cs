using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace ProjectManagement.Shared.Base.Project
{
    public class ProjectBase
    {
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Name { get; set; } = string.Empty;
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Code { get; set; } = string.Empty;
        // Nullable: a new project no longer auto-fills (potentially passed) dates, and an empty
        // date stays empty instead of defaulting to "today". Read-side list DTOs keep their own
        // non-nullable fields (mappers coalesce), so only the form/detail path sees null.
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? TenderDeadline { get; set; }
        public DateTime? TenderQA { get; set; }
        public int Order { get; set; }
    }
}
