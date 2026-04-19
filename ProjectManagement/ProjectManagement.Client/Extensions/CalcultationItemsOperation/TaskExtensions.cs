using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Shared.MVVM.Calculation;

namespace ProjectManagement.Client.Extensions.CalcultationItemsOperation
{
    public static class TaskExtensions
    {
        public static string Style(this TaskListMVVM task, string color, string inactiveTextColor, bool parentActive, bool isSelected)
        {
            if (isSelected)
                return CSS.SelectedItem;

            var rowTextColor = $"var(--net-inactive-text-color, {inactiveTextColor})";
            return task.Active && parentActive
                ? $"background-color:{color};"
                : $"background-color:{color};--calc-row-text-color:{rowTextColor};color:{rowTextColor};";
        }
    }
}
