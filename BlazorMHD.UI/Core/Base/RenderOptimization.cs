using Microsoft.AspNetCore.Components;

namespace BlazorMHD.UI.Core.Base;

public static class RenderOptimization
{
    public static RenderFragment Cache(this RenderFragment fragment) => builder =>
    {
        builder.AddContent(0, fragment);
    };
}
