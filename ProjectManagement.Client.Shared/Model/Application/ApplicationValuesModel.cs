using ProjectManagement.Shared.Base.Application;
using System.Text.Json.Serialization;

namespace ProjectManagement.Client.Shared.Model.Application
{
    public class ApplicationValuesModel : ApplicationValuesBase
    {
        public int Id { get; set; }
        public int CalculationId { get; set; }
        public int ApplicationId { get; set; }

        [JsonIgnore]
        public bool Accordion { get; set; }

        public ApplicationModel Application { get; set; }
    }
}
