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
            CalculationType = x.CalculationType,
            BidRole = x.BidRole,
            CalculationRole = x.CalculationRole,
            CustomCalculationRoleName = x.CustomCalculationRoleName,
            IsLocked = x.IsLocked,
            LockedAtUtc = x.LockedAtUtc,
            LockedByUserId = x.LockedByUserId,
            ApprovedByUserId = x.ApprovedByUserId,
            ApprovedByName = x.ApprovedByName,
            ApprovedAtUtc = x.ApprovedAtUtc,
            SourceCalculationId = x.SourceCalculationId,
            VersionGroupId = x.VersionGroupId,
            VersionNumber = x.VersionNumber,
            CreatedFromCalculationId = x.CreatedFromCalculationId,
            IsCurrentVersion = x.IsCurrentVersion,
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
            CalculationType = x.Calculation.CalculationType,
            BidRole = x.Calculation.BidRole,
            CalculationRole = x.Calculation.CalculationRole,
            CustomCalculationRoleName = x.Calculation.CustomCalculationRoleName,
            IsLocked = x.Calculation.IsLocked,
            LockedAtUtc = x.Calculation.LockedAtUtc,
            LockedByUserId = x.Calculation.LockedByUserId,
            ApprovedByUserId = x.Calculation.ApprovedByUserId,
            ApprovedByName = x.Calculation.ApprovedByName,
            ApprovedAtUtc = x.Calculation.ApprovedAtUtc,
            SourceCalculationId = x.Calculation.SourceCalculationId,
            VersionGroupId = x.Calculation.VersionGroupId,
            VersionNumber = x.Calculation.VersionNumber,
            CreatedFromCalculationId = x.Calculation.CreatedFromCalculationId,
            IsCurrentVersion = x.Calculation.IsCurrentVersion,
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
