using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.SignalR;
using Persistence.Factory;
using Persistence.Interceptors;
using ProjectManagement.Client.DependencyInjection;
using ProjectManagement.BlazorServer;
using ProjectManagement.Services;
using ProjectManagement.SignalR;

namespace ProjectManagement.Extensions;

public static class ProjectServicesRegistrationExtensions
{
    public static IServiceCollection AddProjectServices(this IServiceCollection services)
    {
        services.AddSingleton<ITenantConnectionStringStore, TenantConnectionStringStore>();
        services.AddHostedService<TenantPreloadHostedService>();

        services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddInteractiveWebAssemblyComponents()
            .AddAuthenticationStateSerialization(options =>
            {
                options.SerializeAllClaims = false;
            });

        services.AddRazorPages();
        services.AddHttpClient();
        services.AddApiVersioning();

        // Register client repositories for interactive components rendered on the server.
        services.AddProjectRepositories();

        services.AddProjectWebUiServices();

        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());

        services.AddScoped<ITenantContextResolver, TenantContextResolver>();
        services.AddScoped<CircuitHandler, TenantCircuitHandler>();
        services.AddSingleton<IHubFilter, TenantContextHubFilter>();

        services.AddMemoryCache();

        services.AddScoped<ITenantConnectionStringProvider, TenantConnectionStringProvider>();
        services.AddSingleton<ITenantDbContextFactoryCache, TenantDbContextFactoryCache>();

        // Interceptor is stateless (reads TenantId/UserId from DbContext), so it is safe as Singleton.
        services.AddSingleton<TenantAuditSaveChangesInterceptor>();
        services.AddScoped<IDbContextFactoryTenant, DbContextFactory>();

        services.AddScoped<INotificationHub, SendHubNotification>();
        services.AddScoped<ITenantUserService, TenantUserService>();

        return services;
    }
}
