using AuthPermissions.Services.Implement;
using Domain.Repository.AuthPermissions;
using Microsoft.Extensions.DependencyInjection;

namespace AuthPermissions
{
    public static class ServiceAuthRegistration
    {
        public static void AddAuthPermissionsLayer(this IServiceCollection services)
        {
            services.AddScoped<IAuthRepository, AuthRepository>();
        }
    }
}
