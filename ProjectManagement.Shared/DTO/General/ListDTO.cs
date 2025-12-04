using System.Collections.Generic;

namespace ProjectManagement.Shared.DTO.General
{
    public class ListDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    public class ListDTO<T>
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public T Data { get; set; }
    }
    public class ListOrderDTO
    {
        public int Id { get; set; }
        public int Order { get; set; }
        public string Name { get; set; }
        public string Color { get; set; } = "#00ff00";
    }
    public class GetProjectCalcConfigDTO
    {
        public List<ListDTO> Methods { get; set; }
        public List<ListDTO> Contracts { get; set; }
        public List<ListDTO> Compensations { get; set; }
        public List<ListDTO> Types { get; set; }
        public List<ListDTO> Statuses { get; set; }
        public List<ListDTO> Organisation { get; set; }
    }
}
