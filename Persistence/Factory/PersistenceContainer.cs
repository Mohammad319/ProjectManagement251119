using Application.Interfaces.Email;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Service;

namespace Persistence.Factory
{
    public static class PersistenceContainer
    {
        public static IServiceCollection AddPersistenceServices(this IServiceCollection services)
        {
            services.AddScoped<IEmailService, EmailService>();

            services.AddScoped(provider =>
            {
                var factory = provider.GetRequiredService<IDbContextFactory>();
                return factory.CreateDbContext();
            });

            return services;
        }
    }

}