using Application.Interfaces;
using ProjectManagement.Shared.DTO.Project;

namespace Application.Feature.Project.ProcurementMethods.Commands
{
    public sealed record UpdateProcurementMethodsCommand(PostProcurementMethodsDTO dto, int Id) : IRequest<bool>;

    public class UpdateProcurementMethodsCommandHandler(IShardingSingleDbContext postRepository) : IRequestHandler<UpdateProcurementMethodsCommand, bool>
    {
        public async Task<bool> Handle(UpdateProcurementMethodsCommand request, CancellationToken cancellationToken)
        {
            var _ProcurementMethods = await postRepository.ProcurementMethod.FindAsync(request.Id, cancellationToken);
            if (_ProcurementMethods != null)
            {
                _ProcurementMethods.Name = request.dto.Name;
                _ProcurementMethods.IsVisible = request.dto.IsVisible;
                await postRepository.SaveChangesAsync(cancellationToken);
                return true;
            }
            return false;
        }
    }
}
