using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace ProjectManagement.Shared.Base.AppTenant.Storage
{
    public class Resource_StorageAppBase
    {
        [MaxLength(25, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Unit { get; set; }
        [Required] public double Cost { get; set; }
        public double? FixedQ { get; set; }

        public double ChangeFactor1 { get; set; } = 1;
        public double ChangeFactor2 { get; set; } = 1;
        public double CapWaste { get; set; }
        public double? CO2 { get; set; }
        [AllowNull, MaxLength(500)] public string Note { get; set; }
        public string UpperNote { get; set; }
    }
}
