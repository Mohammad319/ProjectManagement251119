using ProjectManagement.Shared.DTO.Calculation;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Shared.Repositories.Calculation
{
    public interface ITenderRepository
    {
        Task<TenderDetailsDTO> DetailsAsync(int id, int calculationId);
        Task<TenderAttributeValuesListDTO> GetAllAsync(int calculationId);
        Task<int> CreateAsync(int calculationId, int companyId, TenderPostDTO post);
        Task<int> CreateAsync(int calculationId, TenderAttributeListPostDTO post);
        Task<bool> UpdateBindAsync(int tenderID, int attrID, double val);
        Task<bool> UpdateAsync(int id, int calculationId, TenderPostDTO post);
        Task<bool> UpdateAsync(int id, int calculationId, TenderAttributeListPostDTO post);
        Task<bool> DeleteAsync(int id, int calculationId);
        Task<bool> DeleteAttributeAsync(int id, int calculationId);
    }
}
