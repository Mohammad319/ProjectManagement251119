using Domain.Entities.Base;
using Domain.Entities.Users;
using ProjectManagement.Shared.DTO.Calculation;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    public class ShareCalcData
    {
        public bool Tap1 { get; set; }
        public bool Tap2 { get; set; }
        public bool Tap3 { get; set; }
        public bool Tap4 { get; set; }
        public bool Tap5 { get; set; }
        public bool Tap6 { get; set; }
    }
    public sealed class ShareCalcEntity : IDataKeyFilterReadOnly
    {
        public int Id { get; set; }
        [JsonIgnore] public int TenantId { get; set; }
        public int DepartmentId { get; set; }
        public DepartmentEntity Department { get; set; } = null!;
        public int? UserId { get; set; }
        public UserEntity? User { get; set; }
        public int CalculationId { get; set; }
        public CalculationEntity Calculation { get; set; } = null!;
        ShareCalcData? _metadata;
        public ShareCalcData Metadata { get { _metadata ??= new ShareCalcData(); return _metadata; } set { _metadata = value; } }

    }
}
