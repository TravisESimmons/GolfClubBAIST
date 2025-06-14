using GolfBAIST.Models;
using GolfBAIST.TechnicalServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace GolfBAIST.Pages
{
    public class SignUpModel : PageModel
    {
        private readonly ILogger<SignUpModel> _logger;
        private readonly RegisterService _registerService;
        private readonly MemberService _members;

        [BindProperty]
        public Register Register { get; set; }

        public string Message { get; set; }

        public SignUpModel(ILogger<SignUpModel> logger, RegisterService registerService, MemberService members)
        {
            _logger = logger;
            _registerService = registerService;
            _members = members;
        }


        public void OnGet()
        {
            if (Register == null)
            {
                Register = new Register
                {
                    DateOfBirth = DateTime.Today.AddYears(-18) // reasonable default
                };
            }
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                Message = "Invalid form submission. Please correct the highlighted errors.";
                return Page();
            }

            if (!IsValidSponsor(Register.Sponsor1) || !IsValidSponsor(Register.Sponsor2))
            {
                Message = "Sponsor IDs must be valid members with permission to sponsor.";
                return Page();
            }

            try
            {
                bool success = _registerService.AddNewMember(Register);

                if (success)
                {
                    TempData["Success"] = "Sign up successful!";
                    _logger.LogInformation("New member registered: {Email}", Register.Email);
                    return RedirectToPage("/Login");

                }
                else
                {
                    Message = "An error occurred during sign up. Please try again.";
                    _logger.LogWarning("Registration failed for {Email}", Register.Email);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during sign-up for {Email}", Register.Email);
                Message = $"Unexpected error occurred: {ex.Message}";
            }

            return Page();
        }

        private bool IsValidSponsor(int? sponsorId)
        {
            if (sponsorId == null || sponsorId <= 0) return false;

            var sponsor = _members.GetMemberById(sponsorId.Value);
            if (sponsor == null || string.IsNullOrWhiteSpace(sponsor.MembershipStatus)) return false;

            // You can enforce a rule here, like:
            // return sponsor.MembershipTypeID == 1 || sponsor.MembershipTypeID == 2;
            // Assuming only certain types can sponsor

            return true;
        }
    }
}
