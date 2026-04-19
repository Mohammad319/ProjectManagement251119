using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Shared.MVVM.Calculation;

namespace ProjectManagement.Client.Extensions.CalcultationItemsOperation
{
    public static class ResourceExtensions
    {
        public static string Style(this ResourceListMVVM resource, string? color, string inactiveTextColor, bool isTaskActive, bool isSelected)
        {
            color ??= "transparent";
            if (isSelected)
                return CSS.SelectedItem;

            var rowTextColor = $"var(--net-inactive-text-color, {inactiveTextColor})";
            return isTaskActive && resource.Active
                ? $"background-color:{color}"
                : $"background-color:{color};--calc-row-text-color:{rowTextColor};color:{rowTextColor};";
        }
    }
}
