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

namespace Application.Feature.Project.TaskStatus.Commands
{
    public sealed record CreateTaskStatusCommand(PostTaskStatusDTO dto) : IRequest<int>;

        public class CreateTaskStatusCommandHandler(IShardingSingleDbContext postRepository, IMapper mapper) : IRequestHandler<CreateTaskStatusCommand, int>
        {
        public async Task<int> Handle(CreateTaskStatusCommand request, CancellationToken cancellationToken)
            {
                TaskStatusEntity entity = mapper.Map<TaskStatusEntity>(request.dto);
                postRepository.TaskStatus.Add(entity);
                await postRepository.SaveChangesAsync();
                return entity.Id;
            }
        }
    }
