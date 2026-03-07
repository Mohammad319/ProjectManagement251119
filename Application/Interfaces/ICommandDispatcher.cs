using Microsoft.Extensions.DependencyInjection;

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

    public sealed class CommandDispatcher(IServiceProvider serviceProvider) : ICommandDispatcher
    {
        public Task<TResult> Send<TResult>(IRequest<TResult> command, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(command);

            var requestType = command.GetType();
            var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResult));
            var handler = serviceProvider.GetService(handlerType);

            if (handler is null)
            {
                throw new InvalidOperationException(
                    $"No handler registered for request '{requestType.FullName}' with result '{typeof(TResult).FullName}'.");
            }

            return ((dynamic)handler).Handle((dynamic)command, cancellationToken);
        }
    }
}
