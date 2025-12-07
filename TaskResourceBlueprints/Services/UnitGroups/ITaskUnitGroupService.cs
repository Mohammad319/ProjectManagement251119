using TaskResourceBlueprints.Entities;

namespace TaskResourceBlueprints.Services.UnitGroups
{
    public interface ITaskUnitGroupService
    {
        Task<List<TaskUnitGroup>> GetAllAsync(CancellationToken ct = default);
        Task<TaskUnitGroup?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<bool> UpdateAsync(TaskUnitGroup unitGroup, CancellationToken ct = default);
        Task<int> AddAsync(TaskUnitGroup unitGroup, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    }
}
