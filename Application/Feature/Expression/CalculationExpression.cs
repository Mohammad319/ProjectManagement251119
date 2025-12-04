using Application.Extention;
using Application.Mapping.CalcItems;
using Domain.Entities.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Offer;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Application.Feature.Expression
{
    public class CalculationExpression
    {

        public static readonly Expression<Func<CalculationEntity, CalculationPageDTO>> SelectCalculationPageDTO = static (x) => new CalculationPageDTO()
        {
            Tax = x.Tax,
            Name = x.Name,
            OrganisationId = x.OrganisationId,
            Code = x.Code,
            TemplateId = x.TemplateId,
            Factors = x.HourlyPriceFactorData.Factors,
            QuanityList = x.Data.QuanityList,
            Compensation = x.Compensation == null ? string.Empty : x.Compensation.Name,
            Customer = x.Organisation == null ? string.Empty : x.Organisation.Name,
            Contract = x.Contract == null ? string.Empty : x.Contract.Name,
            Tasks = x.Tasks.Select(s => s.MapToTaskListDTO()).ToList(),
        };

        public static readonly Expression<Func<ShareCalcEntity, CalculationPageOtherDepartmentDTO>> SelectCalculationPageOtherDepartmentDTO = (x) => new CalculationPageOtherDepartmentDTO()
        {
            Tax = x.Calculation.Tax,
            //TimeMonth = x.Calculation.TimeMonth,
            Name = x.Calculation.Name,
            Customer = x.Calculation.Organisation.Name,
            OrganisationId = x.Calculation.OrganisationId,
            Code = x.Calculation.Code,
            Compensation = x.Calculation.Compensation.Name,
            Contract = x.Calculation.Contract.Name,
            Tap1 = x.Data.Tap1,
            Tap2 = x.Data.Tap2,
            Tap3 = x.Data.Tap3,
            Tap4 = x.Data.Tap4,
            Tap5 = x.Data.Tap5,
            Tap6 = x.Data.Tap6,
            Tasks = x.Calculation.Tasks.Select(s => s.MapToTaskListDTO()).ToList(),
        };
    }
}
