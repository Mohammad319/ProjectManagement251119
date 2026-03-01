using Domain.Entities.Project;
using ProjectManagement.Shared.DTO.Project;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Application.Helper
{
    public static class ProjectSelectors
    {
        public static Expression<Func<ProjectEntity, ListProjectDTO>> List =>
            x => new ListProjectDTO
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code ?? string.Empty,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                Order = x.SortOrder
            };

        public static Expression<Func<ProjectEntity, PostProjectDTO>> Post =>
            x => new PostProjectDTO
            {
                Name = x.Name,
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
                Order = x.SortOrder
            };
    }
}
