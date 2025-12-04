using Application.Interfaces;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Calculation.Tender.Commands
{
    public sealed record UpdateTenderAttributeCommand(TenderAttributePostDTO dto, int Id, int CalculationId) : IRequest<bool>;

    public class UpdateTenderAttributeCommandHandler(IShardingSingleDbContext dataAccess) : IRequestHandler<UpdateTenderAttributeCommand, bool>
    {
        public async Task<bool> Handle(UpdateTenderAttributeCommand request, CancellationToken cancellationToken)
        {
            var tender = await dataAccess.AttributeNameTender.FirstOrDefaultAsync(x => x.Id == request.Id &&
            x.CalculationId == request.CalculationId);
            if (tender == null)
                return false;
            tender.Note = request.dto.Note;
            tender.Name = request.dto.Name;

            dataAccess.AttributeNameTender.Update(tender);
            await dataAccess.SaveChangesAsync();
            return true;
        }
    }
}
