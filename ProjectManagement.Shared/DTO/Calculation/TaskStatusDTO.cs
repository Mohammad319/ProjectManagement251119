using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.General;

namespace ProjectManagement.Shared.DTO.Calculation
{
    public class PostTaskStatusDTO :  TaskStatusBase
    {
        public bool IsVisible { get; set; } = true;
    }
}
