using Application.Interfaces;
using Application.Interfaces.Context;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Calculation.Template;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Feature.Calculation.Tender.Commands
{
    public sealed record UpdateTenderCommand(TenderPostDTO dto, int Id, int CalculationId) : IRequest<bool>;
        public class UpdateTenderCommandHandler : IRequestHandler<UpdateTenderCommand, bool>
        {
            private readonly IShardingSingleDbContext _dataAccess;

            public UpdateTenderCommandHandler(IShardingSingleDbContext dataAccess)
            {
                _dataAccess = dataAccess;
            }
            public async Task<bool> Handle(UpdateTenderCommand request, CancellationToken cancellationToken)
            {
                var tender = await _dataAccess.Tenders.FirstOrDefaultAsync(x => x.Id == request.Id &&
                x.CalculationId == request.CalculationId);
                if (tender == null)
                    return false;
                tender.Note = request.dto.Note;

                _dataAccess.Tenders.Update(tender);
                await _dataAccess.SaveChangesAsync();
                return true;
            }

        }
    }
