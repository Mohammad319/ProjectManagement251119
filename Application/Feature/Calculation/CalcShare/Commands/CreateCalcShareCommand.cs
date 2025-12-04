using Application.Interfaces;
using AutoMapper;
using Domain.Entities.Application;
using Domain.Entities.Calculation;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Calculation.CalcShare.Commands
{
    public sealed record CreateCalcShareCommand(PostShareCalcDTO dto, int FromUser,int FromDepartment) : IRequest<int>;

        public class CreateCalcShareCommandHandler(IShardingSingleDbContext _dataAccess, IMapper _mapper) : IRequestHandler<CreateCalcShareCommand, int>
        {
            public async Task<int> Handle(CreateCalcShareCommand request, CancellationToken cancellationToken)
            {
                ShareCalcEntity calculation = _mapper.Map<ShareCalcEntity>(request.dto);

                _dataAccess.ShareCalc.Add(calculation);
                await _dataAccess.SaveChangesAsync(cancellationToken);
                return calculation.Id;
            }
    }
}
