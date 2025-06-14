using GolfBAIST.Models;
using GolfBAIST.TechnicalServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace GolfBAIST.Pages
{
    public class EmployeeViewApplicationModel : PageModel
    {
        private readonly MembershipApplicationService _applicationService;
        private readonly MemberService _memberService;
        private readonly UserService _userService;
        private readonly ILogger<EmployeeViewApplicationModel> _logger;

        public EmployeeViewApplicationModel(
            MembershipApplicationService applicationService,
            MemberService memberService,
            UserService userService,
            ILogger<EmployeeViewApplicationModel> logger)
        {
            _applicationService = applicationService;
            _memberService = memberService;
            _userService = userService;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public int ApplicationID { get; set; }

        [BindProperty]
        public string DenialReason { get; set; }

        public MembershipApplication Application { get; set; }

        public void OnGet()
        {
            Application = _applicationService.GetApplicationById(ApplicationID);
        }

        public IActionResult OnPost()
        {
            var action = Request.Form["action"];

            if (action == "Approve")
            {
                _applicationService.SimpleApprove(ApplicationID, _memberService, _userService);
            }
            else if (action == "Deny")
            {
                if (string.IsNullOrWhiteSpace(DenialReason))
                {
                    TempData["Error"] = "You must provide a denial reason before denying the application.";
                    Application = _applicationService.GetApplicationById(ApplicationID); 
                    return Page(); 
                }

                _applicationService.DenyApplication(ApplicationID, DenialReason);
            }

            return RedirectToPage("/EmployeePortal", new { tab = "members" });
        }


    }

}
