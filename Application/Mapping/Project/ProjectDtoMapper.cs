using Domain.Entities.Project;
using ProjectManagement.Shared.DTO.Project;

namespace Application.Mapping.Project
{
    public static class ProjectDtoMapper
    {
        public static PostProjectDTO ToPostDto(this ProjectEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new PostProjectDTO
            {
                Name = entity.Name,
                Code = entity.Code ?? string.Empty,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                TenderDeadline = entity.TenderDeadline,
                TenderQA = entity.TenderQA,
                FolderId = entity.FolderId,
                OrganisationId = entity.OrganisationId,
                ProcurementMethodsId = entity.ProcurementMethodId,
                CompensationId = entity.CompensationId,
                ContractId = entity.ContractId,
                TypeId = entity.ProjectTypeId,
                IsVisible = entity.IsVisible,
                Order = entity.SortOrder,
                Data = entity.GetMetadataSnapshot(),
                StatusId = entity.ProjectStatusId
            };
        }

        public static ProjectDetailsDTO ToDetailsDto(this ProjectEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new ProjectDetailsDTO
            {
                Name = entity.Name,
                Code = entity.Code ?? string.Empty,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                TenderDeadline = entity.TenderDeadline,
                TenderQA = entity.TenderQA,
                Order = entity.SortOrder,
                IsVisible = entity.IsVisible,
                Data = entity.GetMetadataSnapshot(),
                Folder = entity.Folder?.Name ?? string.Empty,
                Organisation = entity.Organisation?.Name ?? string.Empty,
                ProcurementMethods = entity.ProcurementMethod?.Name ?? string.Empty,
                Compensation = entity.Compensation?.Name ?? string.Empty,
                Contract = entity.Contract?.Name ?? string.Empty,
                Type = entity.ProjectType?.Name ?? string.Empty,
                Status = entity.ProjectStatus?.Name ?? string.Empty,
                StatusId = entity.ProjectStatusId,
                StatusName = entity.ProjectStatus?.Name ?? string.Empty,
                Created = entity.CreatedAt,
                LastModified = entity.UpdatedAt
            };
        }

        public static ListProjectDTO ToListDto(
            this ProjectEntity entity,
            int? statusSortOrder = null,
            int calculationCount = 0)
        {
            ArgumentNullException.ThrowIfNull(entity);

            var metadata = entity.GetMetadataSnapshot();

            return new ListProjectDTO
            {
                Id = entity.Id,
                Name = entity.Name ?? string.Empty,
                Code = entity.Code ?? string.Empty,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                TenderDeadline = entity.TenderDeadline,
                TenderQA = entity.TenderQA,
                Order = entity.SortOrder,
                IsVisible = entity.IsVisible,
                CalculationCount = calculationCount,
                Status = entity.ProjectStatus?.Name ?? metadata.StatusName,
                StatusId = entity.ProjectStatusId ?? metadata.StatusId,
                StatusSortOrder = statusSortOrder,
                Color = entity.ProjectStatus?.Color ?? string.Empty,
                CountsAsSubmittedBid = entity.ProjectStatus?.CountsAsSubmittedBid ?? false,
                CountsAsWonBid = entity.ProjectStatus?.CountsAsWonBid ?? false,
                CountsAsLostBid = entity.ProjectStatus?.CountsAsLostBid ?? false,
                Responsible = metadata.Responsibles.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                Developer = metadata.Developer,
                Organisation = entity.Organisation?.Name ?? string.Empty,
                ProcurementName = metadata.ProcurementName,
                ProcurementNumber = metadata.ProcurementNumber,
                CustomerReference = metadata.CustomerReference,
                Contract = entity.Contract?.Name ?? string.Empty,
                Type = entity.ProjectType?.Name ?? string.Empty,
                Inspector = metadata.Inspector
            };
        }
    }
}
