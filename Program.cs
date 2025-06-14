using GolfBAIST.TechnicalServices;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace GolfBAIST
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddRazorPages();
            builder.Services.AddHttpContextAccessor(); // needed for @inject HttpContextAccessor
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // Services
            builder.Services.AddScoped<TeeTimesService>();
            builder.Services.AddScoped<StandingService>();
            builder.Services.AddScoped<RegisterService>();
            builder.Services.AddScoped<MemberService>();
            builder.Services.AddScoped<MembershipApplicationService>();
            builder.Services.AddScoped<UserService>();
            builder.Services.AddScoped<ScoresService>();
            builder.Services.AddScoped<LoginService>();

            builder.Services.AddControllers();

            // Authentication
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Login";
                    options.LogoutPath = "/Logout";
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
                    options.SlidingExpiration = true;
                    options.Cookie.HttpOnly = true;
                    options.Cookie.IsEssential = true;
                    options.Cookie.Expiration = null;
                });

            var app = builder.Build();

            // Removed PathBase for local development
            // app.UsePathBase("/Students/tsimmons11");

            app.UseStaticFiles();
            app.UseRouting();

            app.UseSession();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.MapRazorPages();

            app.Run();
        }
    }
}
