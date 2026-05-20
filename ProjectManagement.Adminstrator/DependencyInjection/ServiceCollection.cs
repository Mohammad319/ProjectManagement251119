using ContextMenuMHD;
using ProjectManagement.Adminstrator.Handless;
using ProjectManagement.Adminstrator.Services.MHDBlazor;
using ProjectManagement.Adminstrator.Services.Synonyms;
using ProjectManagement.Adminstrator.Services.Users;
using ProjectManagement.Adminstrator.Services.TenantMl;
using ProjectManagement.Adminstrator.Components.Account;
using Microsoft.AspNetCore.Identity;
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
            services.AddScoped<ITenantMlTrainingService, TenantMlTrainingService>();
            services.AddScoped<ISynonymDictionaryService, SynonymDictionaryService>();
            services.AddHostedService<TenantMlAutoTrainingHostedService>();
            services.AddScoped<ILoggerPM, Logger>();
            services.AddScoped<ITasksUserComputationServiceWasm, TasksUserComputationServiceWasm>();
            services.AddBlazorMhdUI();
            services.AddAuthorizationCore();
            services.AddSingleton<ContextMenuService>();
            services.AddLocalization();
            services.AddHttpContextAccessor();
            services.AddScoped<LocalizedIdentityEmailSender>();
            services.AddScoped<IEmailSender<ApplicationUser>>(sp => sp.GetRequiredService<LocalizedIdentityEmailSender>());
            services.AddScoped<IAccountNotificationEmailSender>(sp => sp.GetRequiredService<LocalizedIdentityEmailSender>());
            return services;
        }
    }
}
