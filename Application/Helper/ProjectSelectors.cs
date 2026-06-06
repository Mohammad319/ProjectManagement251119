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
                Order = x.SortOrder,
                IsArchived = x.IsArchived,
                Status = x.ProjectStatus != null ? x.ProjectStatus.Name : string.Empty,
                StatusId = x.ProjectStatusId,
                StatusSortOrder = x.ProjectStatus != null ? x.ProjectStatus.SortOrder : null,
                Color = x.ProjectStatus != null ? x.ProjectStatus.Color : string.Empty,
                CountsAsSubmittedBid = x.ProjectStatus != null && x.ProjectStatus.CountsAsSubmittedBid,
                CountsAsWonBid = x.ProjectStatus != null && x.ProjectStatus.CountsAsWonBid,
                CountsAsLostBid = x.ProjectStatus != null && x.ProjectStatus.CountsAsLostBid,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
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
                Order = x.SortOrder,
                IsArchived = x.IsArchived,
                Status = x.ProjectStatus != null ? x.ProjectStatus.Name : string.Empty,
                StatusId = x.ProjectStatusId,
                StatusSortOrder = x.ProjectStatus != null ? x.ProjectStatus.SortOrder : null,
                Color = x.ProjectStatus != null ? x.ProjectStatus.Color : string.Empty,
                CountsAsSubmittedBid = x.ProjectStatus != null && x.ProjectStatus.CountsAsSubmittedBid,
                CountsAsWonBid = x.ProjectStatus != null && x.ProjectStatus.CountsAsWonBid,
                CountsAsLostBid = x.ProjectStatus != null && x.ProjectStatus.CountsAsLostBid
            };
    }
}
