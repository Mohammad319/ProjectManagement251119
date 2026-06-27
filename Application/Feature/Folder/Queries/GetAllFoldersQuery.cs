using Application.Interfaces;
using ProjectManagement.Shared.DTO.Folder;
using ProjectManagement.Shared.DTO.General;

namespace Application.Feature.Project.Folder.Queries
{
    public sealed record GetAllFoldersQuery() : IRequest<List<ListFolderDTO>>;

    public sealed class GetAllFoldersQueryHandler(IFolderService service)
        : IRequestHandler<GetAllFoldersQuery, List<ListFolderDTO>>
    {
        public Task<List<ListFolderDTO>> Handle(GetAllFoldersQuery request, CancellationToken ct)
            => service.GetAllVisibleAsync(ct);
    }
    public sealed record GetDetailsFoldersQuery(Guid Id, int? DepartmentId) : IRequest<DetailsFolderDTO?>;

    public sealed class GetDetailsFoldersQueryHandler(IFolderService service)
        : IRequestHandler<GetDetailsFoldersQuery, DetailsFolderDTO?>
    {
        public Task<DetailsFolderDTO?> Handle(GetDetailsFoldersQuery request, CancellationToken ct)
            => service.GetDetailsAsync(request.Id, request.DepartmentId, ct);
    }
    public sealed record GetFoldersDepartmentQuery(bool IncludeArchived, int? DepartmentId, int UserId = 0, bool IsViewer = false) : IRequest<List<ListFolderDTO>>;

    public sealed class GetFoldersDepartmentQueryHandler(IFolderService service)
        : IRequestHandler<GetFoldersDepartmentQuery, List<ListFolderDTO>>
    {
        public Task<List<ListFolderDTO>> Handle(GetFoldersDepartmentQuery request, CancellationToken ct)
            => service.GetByDepartmentAsync(request.IncludeArchived, request.DepartmentId, ct, request.UserId, request.IsViewer);
    }
    public sealed record GetFoldersFromOtherDepartmentQuery(int DepartmentId, bool IncludeArchived) : IRequest<List<ListFolderDTO>>;

    public sealed class GetFoldersFromOtherDepartmentQueryHandler(IFolderService service)
        : IRequestHandler<GetFoldersFromOtherDepartmentQuery, List<ListFolderDTO>>
    {
        public Task<List<ListFolderDTO>> Handle(GetFoldersFromOtherDepartmentQuery request, CancellationToken ct)
            => service.GetFromOtherDepartmentAsync(request.DepartmentId, request.IncludeArchived, ct);
    }

    // ── Shared / "Alla tillgängliga" discovery ──────────────────────────────────

    public sealed record GetAccessibleDepartmentsQuery(int UserId, int? DepartmentId) : IRequest<List<DepartmentAccessDTO>>;

    public sealed class GetAccessibleDepartmentsQueryHandler(IFolderService service)
        : IRequestHandler<GetAccessibleDepartmentsQuery, List<DepartmentAccessDTO>>
    {
        public Task<List<DepartmentAccessDTO>> Handle(GetAccessibleDepartmentsQuery request, CancellationToken ct)
            => service.GetAccessibleDepartmentsAsync(request.UserId, request.DepartmentId, ct);
    }

    public sealed record HasSharedProjectsInDepartmentQuery(int TargetDepartmentId, int UserId, int? CallerDepartmentId) : IRequest<bool>;

    public sealed class HasSharedProjectsInDepartmentQueryHandler(IFolderService service)
        : IRequestHandler<HasSharedProjectsInDepartmentQuery, bool>
    {
        public Task<bool> Handle(HasSharedProjectsInDepartmentQuery request, CancellationToken ct)
            => service.HasSharedProjectsInDepartmentAsync(request.TargetDepartmentId, request.UserId, request.CallerDepartmentId, ct);
    }

    public sealed record GetSharedDepartmentFoldersQuery(int TargetDepartmentId, int UserId, int? CallerDepartmentId, bool IncludeArchived) : IRequest<List<ListFolderDTO>>;

    public sealed class GetSharedDepartmentFoldersQueryHandler(IFolderService service)
        : IRequestHandler<GetSharedDepartmentFoldersQuery, List<ListFolderDTO>>
    {
        public Task<List<ListFolderDTO>> Handle(GetSharedDepartmentFoldersQuery request, CancellationToken ct)
            => service.GetSharedDepartmentFoldersAsync(request.TargetDepartmentId, request.UserId, request.CallerDepartmentId, request.IncludeArchived, ct);
    }

    public sealed record GetAccessibleFoldersQuery(int UserId, int? CallerDepartmentId, bool IsAdmin, bool IncludeArchived) : IRequest<List<ListFolderDTO>>;

    public sealed class GetAccessibleFoldersQueryHandler(IFolderService service)
        : IRequestHandler<GetAccessibleFoldersQuery, List<ListFolderDTO>>
    {
        public Task<List<ListFolderDTO>> Handle(GetAccessibleFoldersQuery request, CancellationToken ct)
            => service.GetAccessibleFoldersAsync(request.UserId, request.CallerDepartmentId, request.IsAdmin, request.IncludeArchived, ct);
    }

}
