using ProjectManagement.Client.Shared.Model.Tenant;
using ProjectManagement.Shared.DTO.Calculation;

namespace ProjectManagement.Client.Extensions.Storage
{
    public static class StorageAppExtension
    {
        static TaskPostDTO ToTaskPostDTORecursive(List<TaskStoragePostModel> tasksApp, TaskStoragePostModel task)
        {
            var taskToPost = new TaskPostDTO
            {
                Name = task.Name,
                Metadata = task.Data,
                Tasks = [],
                Resources = [],
            };

            var children = tasksApp.Where(x => x.TaskId == task.Id).ToList();
            if (children.Count != 0)
            {
                foreach (var item in children)
                    taskToPost.Tasks.Add(ToTaskPostDTORecursive(tasksApp, item));
            }
            else
            {
                var resources = task.Groups.SelectMany(x => x.ResourcesSelected).ToList();
                foreach (var item in resources)
                    item.Id = 0;
                taskToPost.Resources.AddRange(resources);
            }

            return taskToPost;
        }

        public static List<TaskPostDTO> Convert(List<TaskStoragePostModel> tasksApp, List<int> ids)
        {
            var taskById = tasksApp.ToDictionary(x => x.Id);
            var result = new List<TaskPostDTO>(ids.Count);
            foreach (var id in ids)
            {
                if (taskById.TryGetValue(id, out var task))
                    result.Add(ToTaskPostDTORecursive(tasksApp, task));
            }
            return result;
        }

        public static List<ResourcePostDTO> Convert(List<ResGroupPostModel> groups, List<int> ids)
        {
            var resourceById = groups.SelectMany(x => x.Resources).ToDictionary(x => x.Id);
            var result = new List<ResourcePostDTO>(ids.Count);
            foreach (var id in ids)
            {
                if (resourceById.TryGetValue(id, out var resource))
                    result.Add(resource);
            }
            return result;
        }
    }
}
