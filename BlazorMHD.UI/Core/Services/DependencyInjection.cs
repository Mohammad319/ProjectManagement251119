using BlazorMHD.UI.Core.DesignSystem;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlazorMHD.UI.Core.Services
{
    public static class DependencyInjection
    {
        public static IServiceCollection BlazorMHD(this IServiceCollection builder)
        {
            builder.AddScoped<IDesignSystemService, DesignSystemService>();
            builder.AddScoped<DialogService>();
            builder.AddScoped<ToastService>();
            builder.AddScoped<MessageBoxService>();
            builder.AddScoped<LoadingService>();
            return builder;
        }
    }
}
