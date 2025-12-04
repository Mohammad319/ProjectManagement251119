using ProjectManagement.Shared.DTO.General;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Shared.Repositories.App
{
    public interface IItemCalcCategoryRepository
    {
        Task<List<ListDTO<List<string>>>> GetListAsync();
    }
}
