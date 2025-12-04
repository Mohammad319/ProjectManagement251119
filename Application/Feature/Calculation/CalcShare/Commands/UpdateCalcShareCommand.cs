using Application.Interfaces;
using AutoMapper;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Calculation.CalcShare.Commands
{
    public sealed record UpdateCalcShareCommand(UpdateShareCalcDTO dto, int FromUser, int FromDepartment) : IRequest<bool>;

    public class UpdateCalcShareCommandHandler(IShardingSingleDbContext _dataAccess, IMapper _mapper) : IRequestHandler<UpdateCalcShareCommand, bool>
    {
        public async Task<bool> Handle(UpdateCalcShareCommand request, CancellationToken cancellationToken)
        {
            var calculation = await _dataAccess.ShareCalc.FindAsync(request.dto.Id);
            if (calculation == null)
                return false;
            _mapper.Map(request.dto, calculation);
            await _dataAccess.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
