using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.SignalR;
using Persistence.Factory;
using Persistence.Interceptors;
using ProjectManagement.Client.DependencyInjection;
using ProjectManagement.Client.Helper;
using ProjectManagement.BlazorServer;
using ProjectManagement.Services;
using ProjectManagement.SignalR;
using ProjectManagement.Services.UI;

namespace ProjectManagement.Extensions;

public static class ProjectServicesRegistrationExtensions
{
    public static IServiceCollection AddProjectServices(this IServiceCollection services)
    {
        services.AddSingleton<ITenantConnectionStringStore, TenantConnectionStringStore>();
        services.AddHostedService<TenantPreloadHostedService>();

        // Dev/test demo data generator (no-op unless DemoSeed:Enabled and non-Production).
        services.AddScoped<ProjectManagement.Services.DemoSeed.DemoDataSeeder>();
        services.AddHostedService<ProjectManagement.Services.DemoSeed.DemoSeedHostedService>();

        services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddInteractiveWebAssemblyComponents()
            .AddAuthenticationStateSerialization(options =>
            {
                // Interactive WASM components read tenant/user/department claims from AuthenticationStateProvider.
                options.SerializeAllClaims = true;
            });

        services.AddRazorPages();
        services.AddHttpClient();
        services.AddApiVersioning();
        services.AddScoped<IClientLogger, ClientLogger>();

        // Register client-side services for interactive components rendered on the server.
        services.AddProjectRepositories();
        services.AddApplicationServices();

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
        services.AddSingleton<IUserNotificationNotifier, UserNotificationNotifier>();
        services.AddScoped<INotificationPublisher, NotificationPublisher>();
        services.AddScoped<IUserSystemRoleProvider, UserSystemRoleProvider>();
        services.AddScoped<ITenantUserService, TenantUserService>();
        services.AddScoped<IUserManagementAuditService, UserManagementAuditService>();

        return services;
    }
}
