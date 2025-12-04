using Application.Interfaces;
using Application.Interfaces.Context;
using Domain.Entities.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Calculation.Template;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Feature.Calculation.Tender.Commands
{
    public sealed record CreateTenderAttributeCommand(TenderAttributeListPostDTO dto, int CalculationId) : IRequest<int>;
        public class CreateTenderAttributeCommandHandler : IRequestHandler<CreateTenderAttributeCommand, int>
        {
            private readonly IShardingSingleDbContext _dataAccess;

            public CreateTenderAttributeCommandHandler(IShardingSingleDbContext dataAccess)
            {
                _dataAccess = dataAccess;
            }
            public async Task<int> Handle(CreateTenderAttributeCommand request, CancellationToken cancellationToken)
            {
                AttributeNameTenderEntity attr = new()
                {
                    CalculationId = request.CalculationId,
                    Note = request.dto.Note,
                    Name = request.dto.Name,
                };

                _dataAccess.AttributeNameTender.Add(attr);
                await _dataAccess.SaveChangesAsync();

                List<TenderAttributeBindEntity> TendersAttributes = [];
                foreach (var tender in request.dto.TendersValues)
                    TendersAttributes.Add(new TenderAttributeBindEntity()
                    {
                        TenderAttributeId = attr.Id,
                        TenderId = tender.Key,
                        Value = tender.Value
                    });

                _dataAccess.TenderAttributeBind.AddRange(TendersAttributes);
                await _dataAccess.SaveChangesAsync();

                return attr.Id;
            }
        }
    }
