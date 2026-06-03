using System.Collections.Generic;

namespace ProjectManagement.Shared.DTO.General
{
    public class ListDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class StatusListDTO : ListDTO
    {
        public bool IsApprovalStatus { get; set; }
        public bool LocksCalculation { get; set; }
        public bool AllowsProductionCalculation { get; set; }
        public bool CountsAsSubmittedBid { get; set; }
        public bool CountsAsWonBid { get; set; }
        public bool CountsAsLostBid { get; set; }
    }

    public class ListDTO<T>
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public T Data { get; set; } = default!;
    }
    public interface IListOrderDTO
    {
        public int Id { get;  }
        public int SortOrder { get;  }
        public string Name { get; }
        public string Color { get; }
        public bool IsVisible { get;  }

        void Update(string name, string color, int sortOrder, bool isVisible);
    }
    public class ListOrderDTO
    {
        public int Id { get; set; }
        public int SortOrder { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = "#00ff00";
        public string Code { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        public bool IsSystemDefault { get; set; }
    }
    public class GetProjectCalcConfigDTO
    {
        public List<ListDTO> Methods { get; set; } = [];
        public List<ListDTO> Contracts { get; set; } = [];
        public List<ListDTO> Compensations { get; set; } = [];
        public List<ListDTO> Types { get; set; } = [];
        public List<StatusListDTO> Statuses { get; set; } = [];
        public List<StatusListDTO> ProjectStatuses { get; set; } = [];
        public List<ListDTO> Organisation { get; set; } = [];
    }
}
