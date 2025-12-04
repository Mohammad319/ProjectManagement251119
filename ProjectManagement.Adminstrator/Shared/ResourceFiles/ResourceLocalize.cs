using ProjectManagement.Adminstrator.Shared.ResourceFiles;
using ProjectManagement.Shared.Enums;

namespace ProjectManagement.Client.Adminstrator.Shared.ResourceFiles
{
    public static class ResourceLocalize
    {
        public static string GetResourceType(ResourceTypesEnum resourceType)
        {
            return resourceType switch
            {
                ResourceTypesEnum.Materials => ResourceLoc.Materials,
                ResourceTypesEnum.MachinesAndEquipments => ResourceLoc.MachinesAndEquipments,
                ResourceTypesEnum.Worker => ResourceLoc.Worker,
                ResourceTypesEnum.Managers => ResourceLoc.Managers,
                ResourceTypesEnum.Design => ResourceLoc.Design, 
                ResourceTypesEnum.Subcontractors => ResourceLoc.Subcontractors, 
                ResourceTypesEnum.ProjectOverheadCosts => ResourceLoc.ProjectOverheadCosts,
                ResourceTypesEnum.overheadCosts => ResourceLoc.overheadCosts,
                ResourceTypesEnum.Risk => ResourceLoc.Risk,
                ResourceTypesEnum.Adjustment => ResourceLoc.Adjustment,
                ResourceTypesEnum.Information => ResourceLoc.Information,
                _ => ResourceLoc.Materials,
            };
    }
        public static string GetStorageSort(StorageSort resourceType)
        {
            return resourceType switch
            {
                StorageSort.Construction => ResourceLoc.Construction,
                StorageSort.Infrastructure => ResourceLoc.Infrastructure,
                StorageSort.Electricity => ResourceLoc.Electricity,
                StorageSort.VVS => ResourceLoc.VVS,
                StorageSort.ElectricalPower => ResourceLoc.ElectricalPower,
                _ => ResourceLoc.Construction,
            };
        }
        }
}