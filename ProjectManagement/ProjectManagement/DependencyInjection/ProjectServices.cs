using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.SignalR;
using Persistence.Factory;
using Persistence.Interceptors;
using ProjectManagement.BlazorServer;
using ProjectManagement.Client.DependencyInjection;
using ProjectManagement.Factories;
using ProjectManagement.Services;
using ProjectManagement.SignalR;

namespace ProjectManagement.DependencyInjection;

public static class ServiceRegistration
{
    public static IServiceCollection AddProjectServices(this IServiceCollection services)
    {
        services.AddRazorComponents().AddInteractiveServerComponents().AddInteractiveWebAssemblyComponents()
            .AddAuthenticationStateSerialization(options =>
            {
                options.SerializeAllClaims = true;
            });

        services.AddHttpClient();
        //services.AddScoped(sp =>
        //{
        //    var navigationManager = sp.GetRequiredService<NavigationManager>();
        //    return new HttpClient { BaseAddress = new Uri(navigationManager.BaseUri) };
        //});

        services.AddApiVersioning();
        services.AddRazorPages();
        services.AddServerSideBlazor();
        services.AddControllers();

        services.AddClientServices();

        // -------------------------
        // Tenant Context (Hybrid)
        // -------------------------
        // TenantContext يُعبّى في 3 أماكن:
        // 1) HTTP Requests عبر TenantContextMiddleware
        // 2) Hub invocations عبر TenantContextHubFilter
        // 3) Blazor Server Circuits عبر TenantContextResolver + TenantCircuitHandler
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());

        services.AddScoped<ITenantContextResolver, TenantContextResolver>();

        // CircuitHandler: يضمن تعبئة TenantContext في Blazor Server circuits
        services.AddSingleton<CircuitHandler, TenantCircuitHandler>();

        // HubFilter: يضمن تعبئة TenantContext قبل كل Hub method invocation
        services.AddSingleton<IHubFilter, TenantContextHubFilter>();

        // -------------------------
        // Cache
        // -------------------------
        services.AddMemoryCache();

        // -------------------------
        // Tenant Connection String Provider (Catalog DB)
        // -------------------------
        services.AddScoped<ITenantConnectionStringProvider, TenantConnectionStringProvider>();

        // DbContextOptions cache per tenant
        services.AddSingleton<ITenantDbContextOptionsCache, TenantDbContextOptionsCache>();

        // Interceptor must be Scoped
        services.AddScoped<TenantAuditSaveChangesInterceptor>();

        // Factory + tenant DbContext
        services.AddScoped<IDbContextFactoryTenant, DbContextFactory>();
        //services.AddScoped(sp => sp.GetRequiredService<IDbContextFactoryTenant>().CreateDbContext());

        // App services
        services.AddScoped<INotificationHub, SendHubNotification>();
        services.AddScoped<ITenantUserService, TenantUserService>();

        return services;
    }
}
