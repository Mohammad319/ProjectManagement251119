using System.Text.Json;

namespace ProjectManagement.Shared.DTO.Hub
{
    public class HubDataDto
    {
        public int ParentId { get; set; }
        public string Parent { get; set; }
        public object Data { get; set; }
        public T GetData<T>()
        {
            return JsonSerializer.Deserialize<T>(Data.ToString(),
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
    }
}
