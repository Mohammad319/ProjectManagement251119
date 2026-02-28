using ProjectManagement.Shared.Constant;
using System;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Shared.Base.Application
{
    public class ApplicationDataBase
    {
        public string Description { get; set; } = string.Empty;
    }
    public class ApplicationBase
    {
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Name { get; set; } = string.Empty;
        public bool IsVisible { get; set; } = true;
        public int UserId { get; set; }
        public DateTime LastUpdate { get; set; } = DateTime.Now;
    }
}
