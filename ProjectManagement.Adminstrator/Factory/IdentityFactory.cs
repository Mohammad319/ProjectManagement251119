using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ProjectManagement.Adminstrator.Factory
{
    public static class IdentityFactory
    {
        public static IServiceCollection AddCustomAuthentication(this IServiceCollection services, string connectionString)
        {

            //services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
            services.AddIdentity<ApplicationUser, IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.Configure<IdentityOptions>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Lockout.AllowedForNewUsers = false;

                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5); // قفل الحساب لـ 5 دقائق
                options.Lockout.MaxFailedAccessAttempts = 5; // قفل الحساب بعد 5 محاولات فاشلة
            });
            services.Configure<IdentityOptions>(options =>
            {
                options.Password.RequiredLength = 6;
                options.Password.RequireDigit = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
            });
            using var scope = services.BuildServiceProvider().CreateScope();


            services.AddAuthorization();

            return services;
        }
    }
}
