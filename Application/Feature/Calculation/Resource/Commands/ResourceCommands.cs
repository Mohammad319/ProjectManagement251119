using Application.Interfaces;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Project;

namespace Application.Feature.Calculation.Resource.Commands
{
    public sealed record CopyResourceCommand(List<ResourceTaskItemDTO> Items, int parentTaskId, int sourceCalcId) : IRequest<bool>;
    public class CopyResourceCommandHandler(IResourceService resService) : IRequestHandler<CopyResourceCommand, bool>
    {
        public async Task<bool> Handle(CopyResourceCommand request, CancellationToken cancellationToken)
        {
            return await resService.CopyAsync(request.Items, request.parentTaskId, request.sourceCalcId, cancellationToken);
        }
    }

    public sealed record CreateResourceCommand(List<ResourcePostDTO> Items, int parentTaskId) : IRequest<bool>;

    public class CreateResourceCommandHandler(IResourceService resService) : IRequestHandler<CreateResourceCommand, bool>
    {
        public async Task<bool> Handle(CreateResourceCommand request, CancellationToken cancellationToken)
        {
            return await resService.CreateAsync(request.Items, request.parentTaskId, cancellationToken);
        }
    }

    public sealed record CutResourceCommand(List<ResourceTaskItemDTO> Items, int TaskId, int sourceCalcId) : IRequest<bool>;

    public class CutResourceCommandHandler(IResourceService resService) : IRequestHandler<CutResourceCommand, bool>
    {
        public async Task<bool> Handle(CutResourceCommand request, CancellationToken cancellationToken)
        {
            return await resService.CutAsync(request.TaskId, request.sourceCalcId, request.Items, cancellationToken);
        }
    }

    public sealed record DeleteResourceCommand(IEnumerable<int> Items, int CalcID) : IRequest<bool>;

    public class DeleteResourceCommandHandler(IResourceService resService) : IRequestHandler<DeleteResourceCommand, bool>
    {
        public async Task<bool> Handle(DeleteResourceCommand request, CancellationToken cancellationToken)
        {
            return await resService.DeleteAsync(request.Items, request.CalcID, cancellationToken);
        }
    }

    public sealed record NewOrderResourceCommand(int Id, double NewOrder) : IRequest<bool>;

    public class NewOrderResourceCommandHandler(IResourceService resService) : IRequestHandler<NewOrderResourceCommand, bool>
    {
        public async Task<bool> Handle(NewOrderResourceCommand request, CancellationToken cancellationToken)
        {
            return await resService.NewOrderAsync(request.Id, request.NewOrder, cancellationToken);
        }

    }
    public sealed record UpdateResourceCommand(int Id, ResourcePostDTO Dto) : IRequest<bool>;

    public class UpdateResourceCommandHandler(IResourceService resService) : IRequestHandler<UpdateResourceCommand, bool>
    {
        public async Task<bool> Handle(UpdateResourceCommand request, CancellationToken cancellationToken)
        {
            return await resService.UpdateAsync(request.Id, request.Dto, cancellationToken);
        }

    }
}
