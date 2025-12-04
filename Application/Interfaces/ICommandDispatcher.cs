using Microsoft.Extensions.DependencyInjection;
using System;

namespace Application.Interfaces
{
    public interface IRequest<TResult> { }

    public interface IRequestHandler<TRequest, TResult> where TRequest : IRequest<TResult>
    {
        Task<TResult> Handle(TRequest command, CancellationToken cancellationToken);
    }

    public interface ICommandDispatcher
    {
        Task<TResult> Send<TResult>(IRequest<TResult> command, CancellationToken cancellationToken = default);
    }
    public class CommandDispatcher(IServiceProvider _serviceProvider) : ICommandDispatcher
    {
        public Task<TResult> Send<TResult>(IRequest<TResult> command, CancellationToken cancellationToken = default)
        {
            var handlerType = typeof(IRequestHandler<,>).MakeGenericType(command.GetType(), typeof(TResult));
            dynamic handler = _serviceProvider.GetRequiredService(handlerType);
            return handler.Handle((dynamic)command, cancellationToken);
        }
    }
}
