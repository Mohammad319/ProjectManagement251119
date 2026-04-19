using Application.Mapping.CalcItems;
using Domain.Entities.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
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
            TemplateColumnId = x.TemplateColumnId,
            Sort = x.Sort,
            Factors = x.Factors,
            QuanityList = x.Metadata.QuanityList,
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
            Customer = x.Calculation.Organisation == null ? string.Empty : x.Calculation.Organisation.Name,
            OrganisationId = x.Calculation.OrganisationId,
            Code = x.Calculation.Code,
            TemplateId = x.Calculation.TemplateId,
            TemplateColumnId = x.Calculation.TemplateColumnId,
            Sort = x.Calculation.Sort,
            Compensation = x.Calculation.Compensation == null ? string.Empty : x.Calculation.Compensation.Name,
            Contract = x.Calculation.Contract == null ? string.Empty : x.Calculation.Contract.Name,
            Tap1 = x.Metadata.Tap1,
            Tap2 = x.Metadata.Tap2,
            Tap3 = x.Metadata.Tap3,
            Tap4 = x.Metadata.Tap4,
            Tap5 = x.Metadata.Tap5,
            Tap6 = x.Metadata.Tap6,
            Tasks = x.Calculation.Tasks.Select(s => s.MapToTaskListDTO()).ToList(),
        };
    }
}
