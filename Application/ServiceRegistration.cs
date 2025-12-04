using Application.Interfaces;
using Application.Services.CalculationItems.Resource;
using Application.Services.CalculationItems.Task;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;   // مهم للـ ILoggerFactory
using System.Reflection;

namespace Application
{
    public static class ServiceRegistration
    {
        public static void AddApplicationLayer(this IServiceCollection services)
        {
            services.AddScoped<IResourceService, ResourceService>();
            services.AddScoped<ITaskService, TaskService>();
            services.AddScoped<ICommandDispatcher, CommandDispatcher>();

            services.AddCommandHandlers(Assembly.GetExecutingAssembly());
            services.AddSingleton<IMapper>(sp =>
            {
                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

                var config = new MapperConfiguration(cfg =>
                {
                    cfg.AddMaps(Assembly.GetExecutingAssembly());
                }, loggerFactory);

                return config.CreateMapper();
            });
        }
    }


public static class CommandHandlerRegistrationExtensions
    {
        public static void AddCommandHandlers(this IServiceCollection services, Assembly assembly)
        {
            var handlerInterfaceType = typeof(IRequestHandler<,>);

            var handlers = assembly
                .GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .SelectMany(t => t.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterfaceType)
                    .Select(i => new { Handler = t, Interface = i }));

            foreach (var h in handlers)
            {
                services.AddScoped(h.Interface, h.Handler);
            }
        }
    }
}