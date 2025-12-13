using Domain.Entities.Base;
using Domain.Entities.Users;
using ProjectManagement.Shared.Enums;

namespace Domain.Entities.Calculation
{
    public sealed class ShareCalcData
    {
        public ShareTabs Tabs { get; set; } = ShareTabs.None;
        public bool Tap1 { get => Tabs.HasFlag(ShareTabs.Tap1); set => Tabs = value ? Tabs | ShareTabs.Tap1 : Tabs & ~ShareTabs.Tap1; }
        public bool Tap2 { get => Tabs.HasFlag(ShareTabs.Tap2); set => Tabs = value ? Tabs | ShareTabs.Tap2 : Tabs & ~ShareTabs.Tap2; }
        public bool Tap3 { get => Tabs.HasFlag(ShareTabs.Tap3); set => Tabs = value ? Tabs | ShareTabs.Tap3 : Tabs & ~ShareTabs.Tap3; }
        public bool Tap4 { get => Tabs.HasFlag(ShareTabs.Tap4); set => Tabs = value ? Tabs | ShareTabs.Tap4 : Tabs & ~ShareTabs.Tap4; }
        public bool Tap5 { get => Tabs.HasFlag(ShareTabs.Tap5); set => Tabs = value ? Tabs | ShareTabs.Tap5 : Tabs & ~ShareTabs.Tap5; }
        public bool Tap6 { get => Tabs.HasFlag(ShareTabs.Tap6); set => Tabs = value ? Tabs | ShareTabs.Tap6 : Tabs & ~ShareTabs.Tap6; }
    }
    public sealed class ShareCalcEntity : AuditableEntity<int>
    {
        public int DepartmentId { get; private set; }
        public DepartmentEntity Department { get; private set; } = null!;

        public int CalculationId { get; private set; }
        public CalculationEntity Calculation { get; private set; } = null!;

        public UserEntity? CreatedAtUser { get; private set; }

        private ShareCalcData? _metadata;
        public ShareCalcData Metadata
        {
            get => _metadata ??= new ShareCalcData();
            private set => _metadata = value;
        }

        private ShareCalcEntity() { }

        public ShareCalcEntity(int calculationId, int departmentId, int createdBy, ShareCalcData metadata)
        {
            CalculationId = calculationId;
            DepartmentId = departmentId;
            CreatedBy = createdBy;
            Metadata = metadata ?? new ShareCalcData();
        }

        public void Update(int departmentId, ShareCalcData data)
        {
            DepartmentId = departmentId;
            Metadata = data ?? new ShareCalcData();
        }
    }

    //public sealed class ShareCalcEntity : AuditableEntity<int>
    //{
    //    public int DepartmentId { get; private set; }
    //    public DepartmentEntity Department { get; private set; } = null!;

    //    public int CalculationId { get; private set; }
    //    public CalculationEntity Calculation { get; private set; } = null!;

    //    // المستخدم المنشئ (CreatedBy موجود في AuditableEntity)
    //    public UserEntity? CreatedAtUser { get; private set; }

    //    private ShareCalcData? _metadata;
    //    public ShareCalcData Metadata
    //    {
    //        get => _metadata ??= new ShareCalcData();
    //        private set => _metadata = value;
    //    }

    //    private ShareCalcEntity() { } // EF

    //    public ShareCalcEntity(int calculationId, int departmentId, int createdBy, ShareCalcData metadata)
    //    {
    //        CalculationId = calculationId;
    //        DepartmentId = departmentId;
    //        CreatedBy = createdBy;
    //        Metadata = metadata ?? new ShareCalcData();
    //    }

    //    public void UpdateTabs(ShareCalcData data)
    //    {
    //        Metadata = data ?? new ShareCalcData();
    //    }

    //    public void UpdateDepartment(int departmentId)
    //    {
    //        DepartmentId = departmentId;
    //    }
    //}


}
