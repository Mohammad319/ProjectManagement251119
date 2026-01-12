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
        // -------------------------
        // UI (Hybrid: Server + WASM)
        // -------------------------
        services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddInteractiveWebAssemblyComponents()
            .AddAuthenticationStateSerialization(options =>
            {
                options.SerializeAllClaims = true;
            });

        services.AddRazorPages();       // مهم لبعض سيناريوهات الهوية/الصفحات
        services.AddHttpClient();

        // API Versioning (يبقى هنا أو في Program — ما يسبب تكرار مثل Controllers)
        services.AddApiVersioning();

        // Client shared services (من مشروع Client)
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
        // Tenant connection strings + DbContext options cache
        // -------------------------
        services.AddScoped<ITenantConnectionStringProvider, TenantConnectionStringProvider>();
        services.AddSingleton<ITenantDbContextOptionsCache, TenantDbContextOptionsCache>();

        // -------------------------
        // Persistence helpers
        // -------------------------
        services.AddScoped<TenantAuditSaveChangesInterceptor>(); // Scoped
        services.AddScoped<IDbContextFactoryTenant, DbContextFactory>();

        // -------------------------
        // App services
        // -------------------------
        services.AddScoped<INotificationHub, SendHubNotification>();
        services.AddScoped<ITenantUserService, TenantUserService>();

        return services;
    }
}
