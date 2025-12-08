using Application.Interfaces;
using ProjectManagement.Shared.DTO.Project;

namespace Application.Feature.Project.Contract.Commands
{
    public sealed record UpdateContractCommand(PostContractDTO Dto, int Id) : IRequest<bool>;

    public class UpdateContractCommandHandler(IShardingSingleDbContext postRepository) : IRequestHandler<UpdateContractCommand, bool>
    {
        public async Task<bool> Handle(UpdateContractCommand request, CancellationToken cancellationToken)
        {
            var _ProjectContract = await postRepository.Contract.FindAsync(request.Id, cancellationToken);
            if (_ProjectContract != null)
            {
                _ProjectContract.Name = request.Dto.Name;
                _ProjectContract.IsVisible = request.Dto.IsVisible;
                _ProjectContract.SortOrder = request.Dto.Order;
                await postRepository.SaveChangesAsync(cancellationToken);
                return true;
            }
            return false;
        }
    }
}
