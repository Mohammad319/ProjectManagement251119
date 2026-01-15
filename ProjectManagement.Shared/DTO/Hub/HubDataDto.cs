using System;
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
            if (Data is null)
                throw new InvalidOperationException("HubDataDto.Data is null.");

            var json = Data as string ?? Data.ToString();
            if (string.IsNullOrWhiteSpace(json))
                throw new InvalidOperationException("HubDataDto.Data is empty or not a valid JSON string.");

            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        }

    }
}
