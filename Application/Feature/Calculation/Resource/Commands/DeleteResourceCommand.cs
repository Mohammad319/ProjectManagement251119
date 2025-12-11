using Application.Interfaces;

namespace Application.Feature.Calculation.Resource.Commands
{
    public sealed record DeleteResourceCommand(IEnumerable<int> Items, int CalcID) : IRequest<bool>;

    public class DeleteResourceCommandHandler(IResourceService resService) : IRequestHandler<DeleteResourceCommand, bool>
    {
        public async Task<bool> Handle(DeleteResourceCommand request, CancellationToken cancellationToken)
        {
            return await resService.DeleteAsync(request.Items, request.CalcID, cancellationToken);
        }
    }
}
