
using ProjectManagement.Client.Shared.Model.Project.Calculation;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.General;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Calculation.Template;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Shared.Repositories.Calculation
{
    public interface ICalculationRepository
    {
        Task<bool> ReOrderAsync(int Id, int newOrder);
        Task<int> CopyAsync(Guid ProjectId, int calcId);
        Task<int> CreateProductionCopyAsync(int calcId);
        Task<int> CreateVersionAsync(int calcId);
        Task<bool> MoveAsync(Guid ProjectId, int calcId);
        Task<GetProjectCalcConfigDTO> GetConfig(int? m, int? con, int? com, int? t, int? st);
        Task<List<ListCalculationMVVM>> GetAsync(Guid projectId, bool isVisible = true);
        Task<List<HourlyPriceListGroupDTO>> GetHourlyPriceListAsync(int calcid);
        Task<List<ListCalculationMVVM>> GetShareCalculationsAsync(Guid projectId);
        Task<CalculationDetailsDTO> DetailsAsync(int id);
        Task<CalculationMVVM> GetPageAsync(int id,bool otherdepartment);
        Task<CalculationPostDTO> GetPostAsync(int id);
        Task<int> CreateAsync(Guid ProjectId, CalculationPostDTO model);
        Task<bool> UpdateAsync(int calculationId, List<HourlyPriceListGroupDTO> hourlyPriceList);
        Task<bool> UpdateAsync(CalculationPostDTO model, int id);
        Task<bool> UpdateAsync(List<OHFactors> model, int id);
        Task<bool> UpdateAsync(List<QuanityListDTO> model, int id);
        Task<bool> UpdateSortAsync(int id, SortConfig sort);
        Task<bool> UpdateDisplayPresetsAsync(int id, DisplayOptionsPresetStore store);

        Task<bool> DeleteAsync(int id);
    }
}
