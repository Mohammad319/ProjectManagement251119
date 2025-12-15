using Application.Feature.ResourceType;
using Application.Interfaces;
using ProjectManagement.Shared.DTO.General;
using ProjectManagement.Shared.DTO.ResourceType;

namespace Application.Feature.Calculation.ResourceType.Queries
{
    public sealed record GetResourceTypesQuery(bool IsVisible) : IRequest<List<ResourceTypeModel>>;
    public sealed record GetResourceSortQuery(int ResourceTypeId) : IRequest<List<ResourceSortModel>>;
    public sealed record GetVisualResourcesQuery() : IRequest<ResourceFormDTO>;

    public sealed class GetResourceTypesQueryHandler(IResourceTypeService service)
        : IRequestHandler<GetResourceTypesQuery, List<ResourceTypeModel>>
    {
        public Task<List<ResourceTypeModel>> Handle(GetResourceTypesQuery request, CancellationToken ct)
            => service.GetTypesAsync(request.IsVisible, ct);
    }

    public sealed class GetResourceSortQueryHandler(IResourceTypeService service)
        : IRequestHandler<GetResourceSortQuery, List<ResourceSortModel>>
    {
        public Task<List<ResourceSortModel>> Handle(GetResourceSortQuery request, CancellationToken ct)
            => service.GetSortsAsync(request.ResourceTypeId, ct);
    }

    public sealed class GetVisualResourcesQueryHandler(IResourceTypeService service)
        : IRequestHandler<GetVisualResourcesQuery, ResourceFormDTO>
    {
        public Task<ResourceFormDTO> Handle(GetVisualResourcesQuery request, CancellationToken ct)
            => service.GetVisualFormAsync(ct);
    }
}
