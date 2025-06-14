using GolfBAIST.Models;
using GolfBAIST.TechnicalServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace GolfBAIST.Pages.Employees
{
    public class CreateEmployeeModel : PageModel
    {
        private readonly ILogger<CreateEmployeeModel> _logger;
        private readonly UserService _userService;

        public CreateEmployeeModel(ILogger<CreateEmployeeModel> logger, UserService userService)
        {
            _logger = logger;
            _userService = userService;
        }

        [BindProperty]
        public User NewUser { get; set; }

        public void OnGet() { }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid) return Page();

            if (string.IsNullOrWhiteSpace(NewUser.Role))
            {
                NewUser.Role = "ShopClerk"; 
            }

            NewUser.IsEmployee = true;
            _userService.AddEmployee(NewUser);
            TempData["SuccessMessage"] = "Employee account created.";
            return RedirectToPage("/EmployeePortal");
        }

    }
}
