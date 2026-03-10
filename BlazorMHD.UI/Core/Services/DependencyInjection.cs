using BlazorMHD.UI.Core.DesignSystem;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorMHD.UI.Core.Services;

public static class DependencyInjection
{
    /// <summary>
    /// Preferred registration entry point.
    /// </summary>
    public static IServiceCollection AddBlazorMhdUI(this IServiceCollection services)
    {
        services.AddScoped<IDesignSystemService, DesignSystemService>();
        services.AddScoped<DialogService>();
        services.AddScoped<ToastService>();
        services.AddScoped<MessageBoxService>();
        services.AddScoped<LoadingService>();
        return services;
    }

    /// <summary>
    /// Backward-compatible alias kept for existing projects.
    /// </summary>
    [Obsolete("Use AddBlazorMhdUI(...) instead. This alias remains for backward compatibility.")]
    public static IServiceCollection BlazorMHD(this IServiceCollection services)
        => services.AddBlazorMhdUI();
}
