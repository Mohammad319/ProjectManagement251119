using DocumentFormat.OpenXml.Office2021.DocumentTasks;
using ProjectManagement.Client.Shared.Model.Tenant;
using ProjectManagement.Shared.DTO.Calculation;
using System.Collections.Generic;
using System.Linq;

namespace ProjectManagement.Client.Extensions.Storage
{
    public static class StorageAppExtension
    {
        static TaskPostDTO ToTaskPostDTORecursive(List<TaskStoragePostModel> TasksApp,TaskStoragePostModel task)
        {
            TaskPostDTO taskToPost = new()
            {
                Name = task.Name,
                Data = task.Data,
                Tasks = [],
                Resources = [],
            };
            var list = TasksApp.Where(x => x.TaskId == task.Id).ToList();
            if (list != null && list.Count != 0) foreach (var item in list)
                {
                    taskToPost.Tasks.Add(ToTaskPostDTORecursive(TasksApp,item));
                }
            else if (task.Groups.SelectMany(x => x.ResourcesSelected) != null)
            {
                foreach (var item in task.Groups.SelectMany(x => x.ResourcesSelected)) item.Id = 0;
                taskToPost.Resources.AddRange(task.Groups.SelectMany(x => x.ResourcesSelected));
            }
            return taskToPost;
        }

        public static List<TaskPostDTO> Convert(List<TaskStoragePostModel> TasksApp, List<int> IDs)
        {
            List<TaskPostDTO> TasksPP = [];
            foreach (var id in IDs)
            {
                var t = TasksApp.FirstOrDefault(x => x.Id == id);
                if (t != null)
                    TasksPP.Add(ToTaskPostDTORecursive(TasksApp,t));
            }
            return TasksPP;
        }

        public static List<ResourcePostDTO> Convert(List<ResGroupPostModel> Groups, List<int> IDs)
        {
            List<ResourcePostDTO> resources = [];
            foreach (var id in IDs)
            {
                var task = Groups.SelectMany(x => x.Resources).FirstOrDefault(x => x.Id == id);
                if (task != null)
                    resources.Add(task);
            }
            return resources;
        }
    }
}
