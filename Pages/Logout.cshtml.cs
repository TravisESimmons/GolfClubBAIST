using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using GolfBAIST.TechnicalServices;
using System.Threading.Tasks;

namespace GolfBAIST.Pages
{
    public class LogoutModel : PageModel
    {
        private readonly ILogger<LogoutModel> _logger;
        private readonly LoginService _loginService;

        public LogoutModel(ILogger<LogoutModel> logger, LoginService loginService)
        {
            _logger = logger;
            _loginService = loginService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await _loginService.SignOutAsync(HttpContext);
            HttpContext.Session.Clear(); // ✅ clears session
            TempData["LogoutMessage"] = "✅ You’ve been signed out.";
            _logger.LogInformation("User logged out successfully.");
            return RedirectToPage("/Index"); // ✅ go home after logout
        }
    }
}
