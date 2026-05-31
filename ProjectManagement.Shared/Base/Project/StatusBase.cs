using ProjectManagement.Shared.Constant;
using System;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Shared.Base.Project
{
    public class StatusBase
    {
        public int Order { get; set; }
        public bool IsVisible { get; set; } = true;
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Name { get; set; } = string.Empty;
        [StringLength(7, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize),MinimumLength = 7)]
        public string Color { get; set; } = "#00ff00";
        public bool IsApprovalStatus { get; set; }
        public bool LocksCalculation { get; set; }
        public bool AllowsProductionCalculation { get; set; }
        public bool CountsAsSubmittedBid { get; set; }
        public bool CountsAsWonBid { get; set; }
        public bool CountsAsLostBid { get; set; }
    }
}
