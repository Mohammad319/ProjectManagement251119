using ProjectManagement.Shared.Base.Calculation;

namespace ProjectManagement.Client.Shared.Model.Project
{
    public class TaskStatusModel : TaskStatusBase
    {
        public int Id { get; set; }
        public bool IsVisible { get; set; } = true;
    }
}
