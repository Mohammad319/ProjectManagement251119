
using ProjectManagement.Client.Shared.Model.Project.Calculation;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.General;
using ProjectManagement.Shared.Base.Calculation;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Shared.Repositories.Calculation
{
    public interface ICalculationRepository
    {
        Task<bool> ReOrderAsync(int Id, double newOrder);
        Task<int> CopyAsync(Guid ProjectId, int calcId);
        Task<GetProjectCalcConfigDTO> GetConfig(int? m, int? con, int? com, int? t, int? st);
        Task<List<ListCalculationMVVM>> GetAsync(Guid projectId);
        Task<List<HourlyPriceListGroupDTO>> GetHourlyPriceListAsync(int calcid);
        Task<List<ListCalculationMVVM>> GetShareCalculationsAsync(Guid projectId);
        Task<CalculationDetailsDTO> DetailsAsync(int id);
        Task<CalculationMVVM> GetPageAsync(int id,bool otherdepartment);
        Task<PostCalculationDTO> GetPostAsync(int id);
        Task<int> CreateAsync(Guid ProjectId, PostCalculationDTO model);
        Task<bool> UpdateAsync(int calculationId, List<HourlyPriceListGroupDTO> hourlyPriceList);
        Task<bool> UpdateAsync(PostCalculationDTO model, int id);
        Task<bool> UpdateAsync(List<OHFactors> model, int id);
        Task<bool> UpdateAsync(List<QuanityListDTO> model, int id);

        Task<bool> DeleteAsync(int id);
    }
}
