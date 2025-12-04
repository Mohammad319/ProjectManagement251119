using AuthPermissions.Services;
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


            services.AddMemoryCache(); // لإضافة IMemoryCache
            services.AddLogging();     // لإضافة ILogger<T>
        }
    }
}