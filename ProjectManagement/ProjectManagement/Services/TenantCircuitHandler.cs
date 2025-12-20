using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;
using ProjectManagement.Services;

namespace ProjectManagement.BlazorServer;

/// <summary>
/// عند فتح Circuit في Blazor Server، نضمن تعبئة TenantContext عبر resolver.
/// </summary>
public sealed class TenantCircuitHandler(IServiceScopeFactory scopeFactory) : CircuitHandler
{
    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        // لا نحجب فتح الـ circuit، نملأ tenant في الخلفية داخل scope.
        _ = Task.Run(async () =>
        {
            using var scope = scopeFactory.CreateScope();
            var resolver = scope.ServiceProvider.GetRequiredService<ITenantContextResolver>();
            await resolver.EnsureResolvedAsync(cancellationToken);
        }, cancellationToken);

        return Task.CompletedTask;
    }
}
