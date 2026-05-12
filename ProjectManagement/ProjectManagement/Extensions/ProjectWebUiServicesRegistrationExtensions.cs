using BlazorMHD.UI.Core.Services;
using ProjectManagement.Client.Services.MHDBlazor;
using ProjectManagement.Services.UI;

namespace ProjectManagement.Extensions;

public static class ProjectWebUiServicesRegistrationExtensions
{
    public static IServiceCollection AddProjectWebUiServices(this IServiceCollection services)
    {
        services.AddLocalization();

        // خدمات UI مطلوبة داخل مشروع السيرفر نفسه
        services.AddContextMenuMHD();
        services.AddScoped<MhdServices>();

        // Facades تمنع الـ Components من استهلاك repos الخاصة بالـ Client أو DbContext مباشرًا
        services.AddScoped<IApplicationValuesViewService, ApplicationValuesViewService>();
        services.AddScoped<IStorageViewService, StorageViewService>();
        services.AddScoped<IDepartmentUsersViewService, DepartmentUsersViewService>();

        return services;
    }
}
