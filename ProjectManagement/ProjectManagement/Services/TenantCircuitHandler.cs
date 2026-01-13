using Microsoft.AspNetCore.Components.Server.Circuits;
using ProjectManagement.Services;

namespace ProjectManagement.BlazorServer;

/// <summary>
/// عند فتح Circuit في Blazor Server، نضمن تعبئة TenantContext عبر resolver
/// داخل نفس Scope الخاص بالـ circuit (بدون Task.Run).
/// </summary>
public sealed class TenantCircuitHandler(ITenantContextResolver resolver) : CircuitHandler
{
    public override async Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        await resolver.EnsureResolvedAsync(cancellationToken).ConfigureAwait(false);
        await base.OnCircuitOpenedAsync(circuit, cancellationToken).ConfigureAwait(false);
    }

    // اختياري لكنه مفيد: عند عودة الاتصال (reconnect)
    public override async Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        await resolver.EnsureResolvedAsync(cancellationToken).ConfigureAwait(false);
        await base.OnConnectionUpAsync(circuit, cancellationToken).ConfigureAwait(false);
    }
}
