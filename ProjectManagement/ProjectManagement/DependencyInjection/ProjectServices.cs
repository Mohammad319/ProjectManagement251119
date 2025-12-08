using Microsoft.AspNetCore.Components;
using Persistence.Factory;
using ProjectManagement.Client.DependencyInjection;
using ProjectManagement.Server.HubsPM;
using ProjectManagement.Services;

namespace ProjectManagement.DependencyInjection
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddProjectServices(this IServiceCollection services)
        {
            services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddInteractiveWebAssemblyComponents()
            .AddAuthenticationStateSerialization();
            services.AddHttpContextAccessor();
            services.AddHttpClient();
            services.AddScoped(sp =>
            {
                var navigationManager = sp.GetRequiredService<NavigationManager>();
                return new HttpClient { BaseAddress = new Uri(navigationManager.BaseUri) };
            });
            services.AddApiVersioning();
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddControllers();

            services.AddScoped<IDbContextFactory, DbContextFactory>();
            services.AddScoped<INotificationHub, SendHubNotification>();
            services.AddScoped<ITenantUserService, TenantUserService>();
            services.AddScoped<ICurrentTenantService, CurrentTenantService>();

            services.AddClientServices();

            return services;
        }
    }

}
