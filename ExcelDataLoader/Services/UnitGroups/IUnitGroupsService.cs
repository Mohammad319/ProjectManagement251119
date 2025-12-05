using TaskResourceBlueprints.Entities;

namespace TaskResourceBlueprints.Services.UnitGroups
{
    public interface IUnitGroupsService
    {
        Task<List<TaskUnitGroup>> GetAllAsync();

        Task<TaskUnitGroup> GetByIdAsync(int id);
        Task<bool> UpdateAsync(TaskUnitGroup obj);
        Task<int> AddAsync(TaskUnitGroup obj);
        Task<bool> DeleteAsync(int id);
    }
}
