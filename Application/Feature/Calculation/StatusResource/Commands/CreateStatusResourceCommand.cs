using Application.Interfaces;
using Application.Interfaces.Context;
using AutoMapper;
using Domain.Entities.Calculation;
using Domain.Entities.Project;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Project;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Feature.Project.StatusResource.Commands
{
    public sealed record CreateStatusResourceCommand(PostResourceStatusDTO dto) : IRequest<int>;

        public class CreateStatusResourceCommandHandler(IShardingSingleDbContext _dataAccess, IMapper _mapper) : IRequestHandler<CreateStatusResourceCommand, int>
        {
            public async Task<int> Handle(CreateStatusResourceCommand request, CancellationToken cancellationToken)
            {
                StatusResourcesEntity entity = _mapper.Map<StatusResourcesEntity>(request.dto);
                _dataAccess.ResourceStatus.Add(entity);
                await _dataAccess.SaveChangesAsync(cancellationToken);
                return entity.Id;
            }
        }
    }
