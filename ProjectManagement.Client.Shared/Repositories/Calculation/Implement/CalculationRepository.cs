using ProjectManagement.Client.Shared.Constants;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Shared.Repositories.Calculation.Implement
{
    public class CalculationRepository(HTTPRepository _httpRepository) : ICalculationRepository
    {
        static string CalcURLBase => PMAPIConst.Calculation;

        public async Task<GetProjectCalcConfigDTO> GetConfig(int? m, int? con, int? com, int? t, int? st)
        {
            if (!m.HasValue) m = 0;
            if (!con.HasValue) con = 0;
            if (!com.HasValue) com = 0;
            if (!t.HasValue) t = 0;
            if (!st.HasValue) st = 0;

            return await _httpRepository.GetAsync<GetProjectCalcConfigDTO>(CalcURLBase + $"config/{m}/{con}/{com}/{t}/{st}");
        }
        public async Task<bool> ReOrderAsync(int Id, double newOrder)
        {
            return await _httpRepository.GetAsync<bool>(CalcURLBase + URLConst.Calculation.ReOrder + $"/{Id}/{newOrder}");
        }
        public async Task<int> CopyAsync(Guid ProjectId, int calcId)
        {
            return await _httpRepository.GetAsync<int>(CalcURLBase + URLConst.Calculation.Copy + $"/{ProjectId}/{calcId}");
        }
        public async Task<List<ListCalculationMVVM>> GetAsync(Guid guid)
            => [.. (await _httpRepository.GetAsync<List<ListCalculationMVVM>>(CalcURLBase + guid)).OrderByDescending(x => x.Order)];

        public async Task<List<HourlyPriceListGroupDTO>> GetHourlyPriceListAsync(int calcid)
        {
            var re = await _httpRepository.GetAsync<List<HourlyPriceListGroupDTO>>(CalcURLBase + URLConst.Calculation.HourlyPriceList + $"/{calcid}");
            if (re == null)
                return [];
            return re;
        }
        public async Task<List<ListCalculationMVVM>> GetShareCalculationsAsync(Guid projectId)
        {
            return (await _httpRepository.GetAsync<List<ListCalculationMVVM>>(CalcURLBase + URLConst.Calculation.Share + "/" + projectId)).OrderByDescending(x => x.Order).ToList();
        }
        public async Task<CalculationDetailsDTO> DetailsAsync(int id)
        {
            return await _httpRepository.GetAsync<CalculationDetailsDTO>(CalcURLBase + URLConst.Details + $"/{id}");
        }
        public async Task<CalculationMVVM> GetPageAsync(int id, bool otherdepartment)
        {
            string page = otherdepartment ? URLConst.Calculation.SharedPage : URLConst.Calculation.Page;
            return await _httpRepository.GetAsync<CalculationMVVM>(CalcURLBase + $"{page}/{id}");
        }
        public async Task<PostCalculationDTO> GetPostAsync(int id) =>
            await _httpRepository.GetAsync<PostCalculationDTO>(CalcURLBase + URLConst.Calculation.GetToPost + $"/{id}");

        public async Task<int> CreateAsync(Guid ProjectId, PostCalculationDTO model)
        {
            return await _httpRepository.PostAsync<int, PostCalculationDTO>(model, CalcURLBase + URLConst.Calculation.Create + "/" + ProjectId);
        }

        public async Task<bool> UpdateAsync(int calculationId, List<HourlyPriceListGroupDTO> hourlyPriceList)
        {
            return await _httpRepository.PostAsync<bool, List<HourlyPriceListGroupDTO>>(hourlyPriceList, CalcURLBase + URLConst.Calculation.HourlyPriceList + "/" + calculationId);
        }
        public async Task<bool> UpdateAsync(PostCalculationDTO model, int id)
        {
            return await _httpRepository.PutAsync(model, CalcURLBase + id);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _httpRepository.DeleteAsync(CalcURLBase + id.ToString());
        }

        public async Task<bool> UpdateAsync(List<OHFactors> model, int id)
        {
            return await _httpRepository.PutAsync(model, CalcURLBase + "factors/" + id);
        }

        public async Task<bool> UpdateAsync(List<QuanityListDTO> model, int id)
        {
            return await _httpRepository.PutAsync(model, CalcURLBase + "QuantityList/" + id);
        }
    }
}
