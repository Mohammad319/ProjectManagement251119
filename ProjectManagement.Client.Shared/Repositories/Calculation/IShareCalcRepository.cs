using ProjectManagement.Shared.DTO.Calculation;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Shared.Repositories.Calculation
{
    public interface IShareCalcRepository
    {
        public Task<List<ListShareCalcDTO>> GetAsync(int calcId);
        Task<int> CreateAsync(PostShareCalcDTO calcId);
        Task<bool> UpdateAsync(UpdateShareCalcDTO share);
        Task<bool> RemoveAsync(int calcId);
    }
}
