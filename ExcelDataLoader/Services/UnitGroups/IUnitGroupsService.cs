using ProjectImportHub.Entities;

namespace ProjectImportHub.Services.UnitGroups
{
    public interface IUnitGroupsService
    {
        Task<List<UnitGroupEntity>> GetAllAsync();

        Task<UnitGroupEntity> GetByIdAsync(int id);
        Task<bool> UpdateAsync(UnitGroupEntity obj);
        Task<int> AddAsync(UnitGroupEntity obj);
        Task<bool> DeleteAsync(int id);
    }
}
