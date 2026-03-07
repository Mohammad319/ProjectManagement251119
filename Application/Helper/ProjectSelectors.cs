using Domain.Entities.Project;
using ProjectManagement.Shared.DTO.App;
using ProjectManagement.Shared.DTO.Project;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Application.Helper
{
    public static class ProjectSelectors
    {
        public static Expression<Func<ProjectEntity, ListProjectDTO>> List =>
            x => new ListProjectDTO
            {
                Id = x.Id,
                Name = x.Name ?? string.Empty,
                Code = x.Code ?? string.Empty,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                TenderDeadline = x.TenderDeadline,
                TenderQA = x.TenderQA,
                Order = x.SortOrder
            };

        public static Expression<Func<ProjectEntity, SearchProjectDTO>> Search =>
            x => new SearchProjectDTO
            {
                Id = x.Id,
                FolderId = x.FolderId,
                Name = x.Name ?? string.Empty,
                Code = x.Code ?? string.Empty,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                TenderDeadline = x.TenderDeadline,
                TenderQA = x.TenderQA,
                Order = x.SortOrder
            };

        public static Expression<Func<ProjectEntity, PostProjectDTO>> Post =>
            x => new PostProjectDTO
            {
                Name = x.Name ?? string.Empty,
                Code = x.Code ?? string.Empty,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                TenderDeadline = x.TenderDeadline,
                TenderQA = x.TenderQA,
                FolderId = x.FolderId,
                OrganisationId = x.OrganisationId,
                ProcurementMethodsId = x.ProcurementMethodId,
                CompensationId = x.CompensationId,
                ContractId = x.ContractId,
                TypeId = x.ProjectTypeId,
                IsVisible = x.IsVisible,
                Order = x.SortOrder,
                Address = x.Metadata.Address ?? new List<AddressDTO>(),
                Notes = x.Metadata.Notes ?? new List<string>(),
                Responsibles = x.Metadata.Responsibles ?? new List<string>(),
                Contacts = x.Metadata.Contacts ?? new List<ProjectManagement.Shared.Base.Organisation.UnderContactOrganisationBase>(),
                Developer = x.Metadata.Developer ?? string.Empty,
                ClientsManager = x.Metadata.ClientsManager ?? string.Empty,
                ProjectManager = x.Metadata.ProjectManager ?? string.Empty,
                Designer = x.Metadata.Designer ?? string.Empty,
                Procurement = x.Metadata.Procurement,
                Supervisor = x.Metadata.Supervisor ?? string.Empty,
                OverviewInfoProject = x.Metadata.OverviewInfoProject ?? string.Empty,
                ClientsContactPersonTender = x.Metadata.ClientsContactPersonTender ?? string.Empty,
                Inspector = x.Metadata.Inspector ?? string.Empty
            };

        public static Expression<Func<ProjectEntity, ProjectDetailsDTO>> Details =>
            x => new ProjectDetailsDTO
            {
                Name = x.Name ?? string.Empty,
                Code = x.Code ?? string.Empty,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                TenderDeadline = x.TenderDeadline,
                TenderQA = x.TenderQA,
                Order = x.SortOrder,
                IsVisible = x.IsVisible,
                Folder = x.Folder.Name,
                Organisation = x.Organisation != null ? x.Organisation.Name : string.Empty,
                ProcurementMethods = x.ProcurementMethod != null ? x.ProcurementMethod.Name : string.Empty,
                Compensation = x.Compensation != null ? x.Compensation.Name : string.Empty,
                Contract = x.Contract != null ? x.Contract.Name : string.Empty,
                Type = x.ProjectType != null ? x.ProjectType.Name : string.Empty,
                Created = x.CreatedAt,
                LastModified = x.UpdatedAt,
                Address = x.Metadata.Address ?? new List<AddressDTO>(),
                Notes = x.Metadata.Notes ?? new List<string>(),
                Responsibles = x.Metadata.Responsibles ?? new List<string>(),
                Contacts = x.Metadata.Contacts ?? new List<ProjectManagement.Shared.Base.Organisation.UnderContactOrganisationBase>(),
                Developer = x.Metadata.Developer ?? string.Empty,
                ClientsManager = x.Metadata.ClientsManager ?? string.Empty,
                ProjectManager = x.Metadata.ProjectManager ?? string.Empty,
                Designer = x.Metadata.Designer ?? string.Empty,
                Procurement = x.Metadata.Procurement,
                Supervisor = x.Metadata.Supervisor ?? string.Empty,
                OverviewInfoProject = x.Metadata.OverviewInfoProject ?? string.Empty,
                ClientsContactPersonTender = x.Metadata.ClientsContactPersonTender ?? string.Empty,
                Inspector = x.Metadata.Inspector ?? string.Empty
            };
    }
}
