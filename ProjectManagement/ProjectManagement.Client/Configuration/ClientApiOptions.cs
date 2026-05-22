using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace ProjectManagement.Client.Configuration;

public sealed class ClientApiOptions
{
    public const string SectionName = "Api";

    public string? BaseAddress { get; set; }

    public int TimeoutSeconds { get; set; } = 100;

    public bool ShowDetailedErrors { get; set; }

    public Uri ResolveBaseAddress(IWebAssemblyHostEnvironment environment)
    {
        if (!string.IsNullOrWhiteSpace(BaseAddress)
            && Uri.TryCreate(BaseAddress, UriKind.Absolute, out var configuredBaseAddress))
        {
            return configuredBaseAddress;
        }

        return new Uri(environment.BaseAddress);
    }

    public TimeSpan ResolveTimeout()
    {
        const int minimumTimeoutSeconds = 5;
        const int maximumTimeoutSeconds = 300;

        var boundedTimeout = Math.Clamp(TimeoutSeconds, minimumTimeoutSeconds, maximumTimeoutSeconds);
        return TimeSpan.FromSeconds(boundedTimeout);
    }
}
