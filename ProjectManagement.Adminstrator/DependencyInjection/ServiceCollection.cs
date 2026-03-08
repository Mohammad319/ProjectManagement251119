using ContextMenuMHD;
using ProjectManagement.Adminstrator.Handless;
using ProjectManagement.Adminstrator.Services.MHDBlazor;
using ProjectManagement.Adminstrator.Services.Users;
using ProjectManagement.Shared.DTO.ProjectAppStorage.Service;

namespace ProjectManagement.Adminstrator.DependencyInjection
{
    public static class ServiceCollection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<MhdServices>();
            services.AddScoped<IExceptionHandlers, ExceptionHandlers>();
            services.AddScoped<IUsersService, UsersService>();
            services.AddScoped<ILoggerPM, Logger>();
            services.AddScoped<ITasksUserComputationServiceWasm, TasksUserComputationServiceWasm>();
            services.BlazorMHD();
            services.AddAuthorizationCore();
            services.AddSingleton<ContextMenuService>();
            services.AddLocalization();
            return services;
        }
    }
}
