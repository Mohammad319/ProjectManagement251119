using ProjectManagement.Shared.Base.Calculation;

namespace ProjectManagement.Client.Shared.Model.Project.Calculation
{
    public class StatusResourceModel : StatusResourceBase
    {
        public int Id { get; set; }
        public bool IsVisible { get; set; } = true;
    }
}
