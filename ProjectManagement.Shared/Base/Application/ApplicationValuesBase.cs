using ProjectManagement.Shared.Constant;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Shared.Base.Application
{
    public class ApplicationValuesData
    {
        public Dictionary<Guid, string> Attributes { get; set; }
    }
    public class ApplicationValuesBase
    {
        public int UserId { get; set; }
        ApplicationValuesData data;
        public ApplicationValuesData Data { get { data ??= new ApplicationValuesData(); return data; } set { data = value; } }

        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Name { get; set; }
        [MaxLength(50, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Responsible { get; set; }
        public DateTime LastUpdate { get; set; } = DateTime.Now;

    }
}
