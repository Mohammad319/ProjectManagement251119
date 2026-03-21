using Domain.Entities.Project;
using ProjectManagement.Shared.DTO.Project;
using System;
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
    }
}
