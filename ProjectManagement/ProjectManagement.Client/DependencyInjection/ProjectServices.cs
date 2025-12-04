using Blazored.LocalStorage;

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
            //services.ServicesMHD();
            services.AddSingleton<ContextMenuService>();


            return services;
        }
    }

}
