using ProjectManagement.Shared.Enums;
using System.Text.Json.Serialization;

namespace ProjectManagement.Shared.DTO.Calculation
{
    public abstract class ShareCalcTabsDto
    {
        [JsonIgnore]
        public ShareTabs Tabs { get; set; } = ShareTabs.None;

        public bool Tap1
        {
            get => Tabs.HasFlag(ShareTabs.Tap1);
            set => SetTab(ShareTabs.Tap1, value);
        }

        public bool Tap2
        {
            get => Tabs.HasFlag(ShareTabs.Tap2);
            set => SetTab(ShareTabs.Tap2, value);
        }

        public bool Tap3
        {
            get => Tabs.HasFlag(ShareTabs.Tap3);
            set => SetTab(ShareTabs.Tap3, value);
        }

        public bool Tap4
        {
            get => Tabs.HasFlag(ShareTabs.Tap4);
            set => SetTab(ShareTabs.Tap4, value);
        }

        public bool Tap5
        {
            get => Tabs.HasFlag(ShareTabs.Tap5);
            set => SetTab(ShareTabs.Tap5, value);
        }

        public bool Tap6
        {
            get => Tabs.HasFlag(ShareTabs.Tap6);
            set => SetTab(ShareTabs.Tap6, value);
        }

        private void SetTab(ShareTabs tab, bool enabled)
            => Tabs = enabled ? Tabs | tab : Tabs & ~tab;
    }

    public class PostShareCalcDTO : ShareCalcTabsDto
    {
        public int DepartmentId { get; set; }
        public int? UserId { get; set; }
        public int CalculationId { get; set; }
    }

    public class UpdateShareCalcDTO : ShareCalcTabsDto
    {
        public int Id { get; set; }
    }

    public class ListShareCalcDTO : ShareCalcTabsDto
    {
        public int Id { get; set; }
        public string Department { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public int? UserId { get; set; }
    }
}
