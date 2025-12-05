using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using ProjectImportHub.Entities;
using static ProjectManagement.Shared.Constant.URLConst;

namespace ProjectManagement.Adminstrator.Helper
{
    public static class FolderTreeHelper
    {
        /// <summary>
        /// يرسم كامل شجرة المجلدات تلقائيًا لجميع المستويات.
        /// </summary>
        public static RenderFragment RenderFolderTreeRecursive(
            object receiver,
            List<ResourceCategory> folders,
            HashSet<int> expanded,
            HashSet<int> selectedFolderIds,
            EventCallback<ResourceCategory> onClick,
            EventCallback<ResourceCategory> onContextMenu = default
        ) => builder =>
        {
            foreach (var parent in folders.Where(f => f.ParentCategoryId == null).OrderBy(f => f.SortOrder))
            {
                builder.AddContent(0, RenderFolderTreeItem(receiver, folders, parent, 0, expanded, selectedFolderIds, onClick, onContextMenu));
            }
        };

        private static RenderFragment RenderFolderTreeItem(
            object receiver,
            List<ResourceCategory> folders,
            ResourceCategory folder,
            int level,
            HashSet<int> expanded,
            HashSet<int> selectedFolderIds,
            EventCallback<ResourceCategory> onClick,
            EventCallback<ResourceCategory> onContextMenu
        ) => builder =>
        {
            bool hasChildren = folders.Any(c => c.ParentCategoryId == folder.Id);

            // div الخارجي لكل مجلد
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "style", $"margin-left:{level * 16}px");

            // أيقونة المجلد أو الملف
            builder.OpenElement(2, "span");
            builder.AddAttribute(3, "class", "mr-1 cursor-pointer");
            if (hasChildren)
            {
                builder.AddAttribute(4, "onclick", EventCallback.Factory.Create(receiver, () => Toggle(expanded, folder.Id)));
                builder.AddContent(5, expanded.Contains(folder.Id) ? "📂" : "📁");
            }
            else
            {
                builder.AddContent(5, "📄");
            }
            builder.CloseElement(); // span الأيقونة

            // اسم المجلد
            string classList = $"cursor-pointer px-1 py-0.5 rounded {GetClass(selectedFolderIds.Contains(folder.Id))} hover:bg-gray-200 dark:hover:bg-gray-700";
            builder.OpenElement(6, "span");
            builder.AddAttribute(7, "class", classList);

            if (onClick.HasDelegate)
                builder.AddAttribute(8, "onclick", EventCallback.Factory.Create(receiver, () => onClick.InvokeAsync(folder)));

            if (onContextMenu.HasDelegate)
            {
                builder.AddAttribute(9, "oncontextmenu", EventCallback.Factory.Create<MouseEventArgs>(receiver, () => onContextMenu.InvokeAsync(folder)));
                builder.AddEventPreventDefaultAttribute(10, "oncontextmenu", true);
            }

            builder.AddContent(11, folder.DisplayName + folder.Id);
            builder.CloseElement(); // span اسم المجلد

            builder.CloseElement(); // div الخارجي

            // رسم الأبناء بشكل تكراري إذا المجلد موسع
            if (hasChildren && expanded.Contains(folder.Id))
            {
                foreach (var child in folders.Where(f => f.ParentCategoryId == folder.Id).OrderBy(f => f.SortOrder))
                {
                    builder.AddContent(12, RenderFolderTreeItem(receiver, folders, child, level + 1, expanded, selectedFolderIds, onClick, onContextMenu));
                }
            }
        };

        private static void Toggle(HashSet<int> expanded, int folderId)
        {
            if (expanded.Contains(folderId))
                expanded.Remove(folderId);
            else
                expanded.Add(folderId);
        }

        private static string GetClass(bool isSelected)
        {
            return isSelected ? "font-bold text-blue-600 dark:text-blue-400" : "";
        }
    }


}