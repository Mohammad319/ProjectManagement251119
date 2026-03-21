using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Shared.MVVM.Calculation;

namespace ProjectManagement.Client.Extensions.CalcultationItemsOperation
{
    public static class ResourceExtensions
    {
        public static string Style(this ResourceListMVVM resource, string? color, bool isTaskActive, bool isSelected)
        {
            color ??= "transparent";
            if (isSelected)
                return CSS.SelectedItem;

            return isTaskActive && resource.Active
                ? $"background-color:{color}"
                : $"background-color:{color};color:rgb(49 48 45 / 55%)";
        }
    }
}
