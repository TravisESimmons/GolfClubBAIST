using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using GolfBAIST.TechnicalServices;
using System.Threading.Tasks;

namespace GolfBAIST.Pages
{
    public class LoginModel : PageModel
    {
        private readonly ILogger<LoginModel> _logger;
        private readonly LoginService _loginService;

        [BindProperty]
        public string Username { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public string ErrorMessage { get; private set; }

        public LoginModel(ILogger<LoginModel> logger, LoginService loginService)
        {
            _logger = logger;
            _loginService = loginService;
        }

        public void OnGet()
        {
            // Just load the login page
        }

        public async Task<IActionResult> OnPostAsync()
        {
            _logger.LogInformation("Login attempt for user: {Username}", Username);

            var role = _loginService.ValidateLogin(Username, Password);

            if (!string.IsNullOrEmpty(role))
            {
                if (role.ToLower() == "applicant")
                {
                    ErrorMessage = "Your membership application is still pending approval.";
                    return Page();
                }

                await _loginService.SignInAsync(HttpContext, Username, role);
                _logger.LogInformation("User {Username} signed in with role {Role}", Username, role);

                var lowerRole = role.ToLower();

                return lowerRole switch
                {
                    "admin" or "shopclerk" or "committee" => RedirectToPage("/EmployeePortal"),
                    "user" => RedirectToPage("/BookingTeeTime"),
                    _ => RedirectToPage("/Index")
                };
            }
            else
            {
                _logger.LogWarning("Invalid login for user: {Username}", Username);
                ErrorMessage = "Invalid login attempt.";
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return Page();
            }
        }
    }
}
