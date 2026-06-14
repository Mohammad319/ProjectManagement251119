using Blazored.LocalStorage;
using BlazorMHD.UI.Core.Services;
using pax.BlazorChartJs;

namespace ProjectManagement.Client.DependencyInjection
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddClientServices(this IServiceCollection services)
        {
            services.AddProjectRepositories();
            services.AddApplicationServices();
            services.AddBlazoredLocalStorage();
            services.AddLocalization();
            // ContextMenuService is registered by AddBlazorMhdUI() (merged from ContextMenuMHD)
            services.AddChartJs(options =>
            {
                options.ChartJsLocation = "https://cdn.jsdelivr.net/npm/chart.js";
                options.ChartJsPluginDatalabelsLocation = "https://cdn.jsdelivr.net/npm/chartjs-plugin-datalabels@2";
            });

            services.AddBlazorMhdUI();
            return services;
        }
    }
}
