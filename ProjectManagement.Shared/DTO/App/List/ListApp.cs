using ProjectManagement.Shared.DTO.General;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ProjectManagement.Shared.DTO.App.List
{
    public record TabItem(int Id, string Label);

    public class ListApp
    {
        [JsonIgnore]public bool IsLoaded;
        public List<ListDTO> CustomerGroups {  get; set; } = [];
        public List<ListDTO> Customers { get; set; } = [];
        public List<ListDTO> Methods { get; set; } = [];
        public List<ListDTO> Contracts { get; set; } = [];
        public List<ListDTO> Compensations { get; set; } = [];
        public List<ListDTO> ProjectTypes { get; set; } = [];
        public List<ListDTO> Status { get; set; } = [];
        public List<ListDTO> ResourceStatus { get; set; } = [];

    }
}
