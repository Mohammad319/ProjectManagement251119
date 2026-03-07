using Application.Interfaces;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Application
{
    public static class ServiceRegistration
    {
        public static void AddApplicationLayer(this IServiceCollection services)
        {
            services.AddScoped<ICommandDispatcher, CommandDispatcher>();
            services.AddCommandHandlers(Assembly.GetExecutingAssembly());

            services.AddSingleton<IMapper>(sp =>
            {
                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                var config = new MapperConfiguration(cfg => cfg.AddMaps(Assembly.GetExecutingAssembly()), loggerFactory);
                return config.CreateMapper();
            });
        }
    }

    public static class CommandHandlerRegistrationExtensions
    {
        public static void AddCommandHandlers(this IServiceCollection services, Assembly assembly)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(assembly);

            var handlerInterfaceType = typeof(IRequestHandler<,>);

            var handlers = GetLoadableTypes(assembly)
                .Where(t => t is { IsAbstract: false, IsInterface: false })
                .SelectMany(t => t.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterfaceType)
                    .Select(i => new { Handler = t, Interface = i }))
                .DistinctBy(x => new { x.Handler, x.Interface });

            foreach (var handler in handlers)
            {
                services.AddScoped(handler.Interface, handler.Handler);
            }
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t is not null)!;
            }
        }
    }
}
