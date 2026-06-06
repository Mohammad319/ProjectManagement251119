using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Shared.DTO.Project
{
    public class ProjectFilter
    {
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Name { get; set; } = string.Empty;
        public string NameOperator { get; set; } = string.Empty;
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Code { get; set; } = string.Empty;
        public DateTime? StartDate1 { get; set; }
        public DateTime? StartDate2 { get; set; }
        public DateTime? EndDate1 { get; set; }
        public DateTime? EndDate2 { get; set; }
        public bool? IsArchived { get; set; } = false;
        public Guid? StatusId { get; set; }
        public Guid? FolderId { get; set; }
        public int? CustomerId { get; set; }
        public int Skip { get; set; } = 0;
        public Sort? SortValue { get; set; }
        public enum Sort
        {
            Code,
            StPro,
            StProDes,
            EnPro,
            EnProDes,
        }
    }
}
