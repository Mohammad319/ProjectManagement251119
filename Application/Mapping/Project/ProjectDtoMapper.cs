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
                ProcurementProcedureId = entity.ProcurementProcedureId,
                CompensationId = entity.CompensationId,
                ContractId = entity.ContractId,
                TypeId = entity.ProjectTypeId,
                IsArchived = entity.IsArchived,
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
                IsArchived = entity.IsArchived,
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
            int calculationCount = 0,
            bool isShared = false)
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
                IsArchived = entity.IsArchived,
                IsShared = isShared,
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
                Inspector = metadata.Inspector,
                Designer = metadata.Designer,
                Supervisor = metadata.Supervisor,
                AddressText = FormatAddress(metadata.Address.FirstOrDefault()),
                ProcurementMethods = entity.ProcurementMethod?.Name ?? string.Empty,
                Compensation = entity.Compensation?.Name ?? string.Empty,
                ProcurementProcedure = entity.ProcurementProcedure?.Name ?? string.Empty,
                ClientsManager = metadata.ClientsManager,
                PublicationDate = metadata.PublicationDate,
                DecisionDate = metadata.DecisionDate,
                ImportInfo = metadata.ImportInfo
            };
        }

        private static string FormatAddress(ProjectManagement.Shared.DTO.App.AddressDTO? address)
        {
            if (address is null)
                return string.Empty;

            var parts = new[]
            {
                address.Street,
                address.Nr,
                address.ZIPCode,
                address.City,
                address.Region,
                address.Country
            };

            return string.Join(", ", parts.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
        }
    }
}
