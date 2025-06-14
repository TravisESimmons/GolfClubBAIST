using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using GolfBAIST.Models;
using Microsoft.Data.SqlClient;

namespace GolfBAIST.TechnicalServices
{
    public class LoginService
    {
        private readonly ILogger<LoginService> _logger;
        private readonly UserService _userService;

        public LoginService(ILogger<LoginService> logger, UserService userService)
        {
            _logger = logger;
            _userService = userService;
        }

        public string ValidateLogin(string username, string password)
        {
            _logger.LogInformation("Validating login for Username={Username}", username);
            return _userService.ValidateLogin(username, password);
        }

        public async Task SignInAsync(HttpContext httpContext, string username, string role)
        {
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, username),
        new Claim(ClaimTypes.Role, role)
    };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true
            };

            // Get user data
            var user = _userService.GetUserByUsername(username); 
            if (user.MemberID.HasValue)
            {
                httpContext.Session.SetInt32("MemberID", user.MemberID.Value);
            }

            httpContext.Session.SetInt32("IsEmployee", user.IsEmployee ? 1 : 0);
            httpContext.Session.SetString("Role", role); 


            await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
        }

        public async Task SignOutAsync(HttpContext httpContext)
        {
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            httpContext.Session.Clear();
        }




    }
}