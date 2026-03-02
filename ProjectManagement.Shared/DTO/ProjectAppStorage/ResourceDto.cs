using ProjectManagement.Shared.Base.AppTenant;
using ProjectManagement.Shared.DTO.App.Dataloader;
using ProjectManagement.Shared.Enums;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using static System.Runtime.InteropServices.JavaScript.JSType;
namespace ProjectManagement.Shared.DTO.ProjectAppStorage;
public  enum ResourceSource
{
    Base, FromCodition
}
public class ResourceDto: ResourceDLBase
{
    public CalcResCost CalcResCost { get; set; } = new();


    public int Id { get; set; }
    public int? FolderId { get; set; }
    public int? MenuId { get; set; }

    public bool Active { get; set; } = true;
    public List<string> Formulas { get; set; } = [];
    public List<ResourcePropertyBindDto> Properties { get; set; } = [];
    public List<RoleDTO> CostRole { get; set; } = [];
    public List<RoleDTO> CapRole { get; set; } = [];
    public decimal? CostStorageValue { get; set; }
    public double? CostUserValue { get; set; }
    public string NameUserValue { get; set; } = string.Empty;
    public int? StatusId { get; set; }
    public int? ResourceTypeId { get; set; }
    public int? ResourceSortId { get; set; }
    public int? AccountId { get; set; }
    // Costs are stored as decimal in ResourceMetadata; for UI summaries we expose a double.
    [JsonIgnore] public double TotalCost => ((double)Data.Cost * Data.Quantity.GetValueOrDefault()) + (double)Data.BaseCost.GetValueOrDefault();

    [JsonIgnore] public ResourceSource ResourceSource = ResourceSource.Base;
    [JsonIgnore] public bool IsAdded { get; set; }
    [JsonIgnore] public int? GroupId { get; set; } = null;

    [JsonIgnore] public bool HasCap => ResType is ResourceTypesEnum.MachinesAndEquipments or ResourceTypesEnum.Worker;
    [JsonIgnore] public bool HasWast => ResType is ResourceTypesEnum.Materials;
}
