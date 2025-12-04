using ProjectManagement.Shared.Base.Calculation;

namespace ProjectManagement.Shared.DTO.Calculation
{
    public class PostTaskStatusDTO : TaskStatusBase
    {
        public bool IsVisible { get; set; } = true;
    }
}
