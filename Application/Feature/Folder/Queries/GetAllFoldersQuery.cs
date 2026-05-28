using Application.Interfaces;
using ProjectManagement.Shared.DTO.Folder;

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
    public sealed record GetFoldersDepartmentQuery(bool IncludeArchived, int? DepartmentId) : IRequest<List<ListFolderDTO>>;

    public sealed class GetFoldersDepartmentQueryHandler(IFolderService service)
        : IRequestHandler<GetFoldersDepartmentQuery, List<ListFolderDTO>>
    {
        public Task<List<ListFolderDTO>> Handle(GetFoldersDepartmentQuery request, CancellationToken ct)
            => service.GetByDepartmentAsync(request.IncludeArchived, request.DepartmentId, ct);
    }
    public sealed record GetFoldersFromOtherDepartmentQuery(int DepartmentId, bool IncludeArchived) : IRequest<List<ListFolderDTO>>;

    public sealed class GetFoldersFromOtherDepartmentQueryHandler(IFolderService service)
        : IRequestHandler<GetFoldersFromOtherDepartmentQuery, List<ListFolderDTO>>
    {
        public Task<List<ListFolderDTO>> Handle(GetFoldersFromOtherDepartmentQuery request, CancellationToken ct)
            => service.GetFromOtherDepartmentAsync(request.DepartmentId, request.IncludeArchived, ct);
    }

}
