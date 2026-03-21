using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Shared.MVVM.Calculation;

namespace ProjectManagement.Client.Extensions.CalcultationItemsOperation
{
    public static class TaskExtensions
    {
        public static string Style(this TaskListMVVM task, string color, bool parentActive, bool isSelected)
        {
            if (isSelected)
                return CSS.SelectedItem;

            return task.Metadata.IsActive && parentActive
                ? $"background-color:{color};"
                : $"background-color:{color};color:rgba(180, 180, 180, 0.5);";
        }
    }
}
