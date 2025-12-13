using System;
using ProjectManagement.Shared.Enums;

namespace ProjectManagement.Shared.DTO.Calculation
{
    public sealed class ShareCalcUpsertDTO
    {
        public int? Id { get; set; }              // null -> Create , value -> Update
        public int CalculationId { get; set; }
        public int DepartmentId { get; set; }

        // الشكل الجديد المفضل
        public ShareTabs Tabs { get; set; } = ShareTabs.None;

        // Optional: دعم الشكل القديم إذا UI مازال يرسل Tap1..Tap6
        public bool? Tap1 { get; set; }
        public bool? Tap2 { get; set; }
        public bool? Tap3 { get; set; }
        public bool? Tap4 { get; set; }
        public bool? Tap5 { get; set; }
        public bool? Tap6 { get; set; }

        public ShareTabs ResolveTabs()
        {
            // إذا Tabs تم إرساله بشكل مباشر -> اعتمده
            var tabs = Tabs;

            // إذا وصل booleans (قديمة) استخدمها لتعديل Tabs
            if (Tap1.HasValue) tabs = Tap1.Value ? tabs | ShareTabs.Tap1 : tabs & ~ShareTabs.Tap1;
            if (Tap2.HasValue) tabs = Tap2.Value ? tabs | ShareTabs.Tap2 : tabs & ~ShareTabs.Tap2;
            if (Tap3.HasValue) tabs = Tap3.Value ? tabs | ShareTabs.Tap3 : tabs & ~ShareTabs.Tap3;
            if (Tap4.HasValue) tabs = Tap4.Value ? tabs | ShareTabs.Tap4 : tabs & ~ShareTabs.Tap4;
            if (Tap5.HasValue) tabs = Tap5.Value ? tabs | ShareTabs.Tap5 : tabs & ~ShareTabs.Tap5;
            if (Tap6.HasValue) tabs = Tap6.Value ? tabs | ShareTabs.Tap6 : tabs & ~ShareTabs.Tap6;

            return tabs;
        }
    }
}

namespace ProjectManagement.Shared.Enums
{
    [Flags]
    public enum ShareTabs
    {
        None = 0,
        Tap1 = 1 << 0,
        Tap2 = 1 << 1,
        Tap3 = 1 << 2,
        Tap4 = 1 << 3,
        Tap5 = 1 << 4,
        Tap6 = 1 << 5,
        All = Tap1 | Tap2 | Tap3 | Tap4 | Tap5 | Tap6
    }
}
