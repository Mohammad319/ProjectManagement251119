using Application.Extention;
using Application.Interfaces;
using Application.Services.CalculationItems.Resource;

namespace Application.Feature.Calculation.Resource.Commands
{
    public sealed record NewOrderResourceCommand(int Id, double NewOrder) : IRequest<bool>;

    public class NewOrderResourceCommandHandler(IResourceService resService) : IRequestHandler<NewOrderResourceCommand, bool>
    {
        public async Task<bool> Handle(NewOrderResourceCommand request, CancellationToken cancellationToken)
        {
            return await resService.NewOrderAsync(request.Id, request.NewOrder, cancellationToken);
        }

    }
}
