using Domain.Entities.Calculation;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Mapping.Calculation
{
    public static class CalculationDtoMapper
    {
        public static CalculationDetailsDTO ToDetailsDto(this CalculationEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new CalculationDetailsDTO
            {
                TenderQA = entity.TenderQA,
                TenderDeadline = entity.TenderDeadline,
                Procurement = entity.Procurement,
                Name = entity.Name,
                Tax = entity.Tax,
                Compensation = entity.Compensation?.Name ?? string.Empty,
                Contract = entity.Contract?.Name ?? string.Empty,
                ProcurementMethods = entity.ProcurementMethods?.Name ?? string.Empty,
                Type = entity.Type?.Name ?? string.Empty,
                Order = entity.SortOrder,
                Sort = entity.Sort,
                Code = entity.Code,
                DecisionDate = entity.DecisionDate,
                EndDate = entity.EndDate,
                StartDate = entity.StartDate,
                PublicationDate = entity.PublicationDate,
                Data = entity.GetMetadataSnapshot(),
                PriceData = new CalculationHourlyPriceFactorData
                {
                    HourlyPrice = entity.HourlyPrice,
                    Factors = entity.Factors
                }
            };
        }

        public static CalculationPostDTO ToPostDto(this CalculationEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new CalculationPostDTO
            {
                TenderQA = entity.TenderQA,
                TenderDeadline = entity.TenderDeadline,
                CompensationId = entity.CompensationId,
                ContractId = entity.ContractId,
                Procurement = entity.Procurement,
                ProcurementMethodsId = entity.ProcurementMethodsId,
                Name = entity.Name,
                Tax = entity.Tax,
                TypeId = entity.TypeId,
                IsPrivate = entity.IsPrivate,
                Code = entity.Code,
                OrganisationId = entity.OrganisationId,
                EndDate = entity.EndDate,
                StatusId = entity.StatusId,
                IsVisible = entity.IsVisible,
                StartDate = entity.StartDate,
                Order = entity.SortOrder,
                Sort = entity.Sort,
                DecisionDate = entity.DecisionDate,
                PublicationDate = entity.PublicationDate,
                TemplateId = entity.TemplateId,
                TemplateColumnId = entity.TemplateColumnId,
                Metadata = entity.GetMetadataSnapshot(),
                PriceData = new CalculationHourlyPriceFactorData
                {
                    HourlyPrice = entity.HourlyPrice,
                    Factors = entity.Factors
                }
            };
        }
    }
}
