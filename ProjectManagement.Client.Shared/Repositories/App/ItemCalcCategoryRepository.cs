using ProjectManagement.Client.Shared.Constants;
using ProjectManagement.Client.Shared.Repositories;
using ProjectManagement.Client.Shared.Repositories.App;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.General;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Shared.Repositories.App
{
    public class ItemCalcCategoryRepository(HTTPRepository _httpRepository) : IItemCalcCategoryRepository
    {
        private static string URLBase => PMAPIConst.ItemCalcCategory;
        public async Task<List<ListDTO<List<string>>>> GetListAsync() =>
            await _httpRepository.GetAsync<List<ListDTO<List<string>>>>(URLBase);
    }
}
