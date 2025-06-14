using GolfBAIST.Models;
using GolfBAIST.TechnicalServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GolfBAIST.Pages
{
    public class ModifyTeeTimeModel : PageModel
    {
        private readonly TeeTimesService _teeTimesService;
        private readonly ILogger<ModifyTeeTimeModel> _logger;

        public ModifyTeeTimeModel(TeeTimesService teeTimesService, ILogger<ModifyTeeTimeModel> logger)
        {
            _teeTimesService = teeTimesService;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public int TeeTimeID { get; set; }

        [BindProperty(SupportsGet = true)]
        public string RoleQueryParam { get; set; } // Allows role override via query

        [BindProperty]
        public TeeTime TeeTime { get; set; }

        public string MemberName { get; set; }

        [BindProperty]
        public string TimeSlot { get; set; }

        public string Message { get; set; }

        public string BookedByName { get; set; }

        public Dictionary<int, string> MemberNames { get; set; }

        public string Role { get; set; }

        public IActionResult OnGet()
        {
            TeeTime = _teeTimesService.GetTeeTimes().FirstOrDefault(t => t.TeeTimeID == TeeTimeID);
            if (TeeTime == null) return RedirectToPage("/MyTeeTimes");

            TeeTime.Players = 1 + (TeeTime.AdditionalMemberIDs?.Count(id => id != 0) ?? 0);

            int memberId = HttpContext.Session.GetInt32("MemberID") ?? 0;
            string role = RoleQueryParam ?? HttpContext.Session.GetString("Role") ?? "";
            Role = role;

            MemberNames = _teeTimesService.GetNamesForExistingPlayers(TeeTimeID, memberId, role);
            BookedByName = MemberNames.ContainsKey(TeeTime.MemberID)
                ? MemberNames[TeeTime.MemberID]
                : $"Member #{TeeTime.MemberID}";

            if (role.Equals("admin", StringComparison.OrdinalIgnoreCase) || role.Equals("shopclerk", StringComparison.OrdinalIgnoreCase))
            {
                var scoresService = new ScoresService();
                var score = scoresService.GetScoreByMemberAndTeeTime(TeeTime.MemberID, TeeTimeID);
                if (score != null)
                {
                    TeeTime.ScoreID = score.ScoreID;
                }
            }

            return Page();
        }

        public IActionResult OnPost()
        {
            try
            {
                int currentMemberId = HttpContext.Session.GetInt32("MemberID") ?? 0;
                string role = HttpContext.Session.GetString("Role") ?? "";

                if (!(role.Equals("admin", StringComparison.OrdinalIgnoreCase) || role.Equals("shopclerk", StringComparison.OrdinalIgnoreCase)) && TeeTime.MemberID != currentMemberId)
                {
                    _logger.LogWarning("Unauthorized POST update attempt by MemberID {MemberID} on TeeTimeID {TeeTimeID}", currentMemberId, TeeTime.TeeTimeID);
                    return RedirectToPage("/MyTeeTimes");
                }

                if (!string.IsNullOrEmpty(TimeSlot))
                {
                    var split = TimeSlot.Split("|");
                    TeeTime.StartTime = TimeSpan.Parse(split[0]);
                    TeeTime.EndTime = TimeSpan.Parse(split[1]);
                }

                TeeTime.Players = 1 + (TeeTime.AdditionalMemberIDs?.Count(id => id != 0) ?? 0);

                _teeTimesService.ValidateTeeTimeParameters(TeeTime);

                if (!_teeTimesService.IsTimeSlotAvailable(TeeTime.Date, TeeTime.StartTime, TeeTime.EndTime, TeeTime.TeeTimeID))
                {
                    Message = "Selected time is not available.";
                    return Page();
                }

                bool success = _teeTimesService.UpdateTeeTime(TeeTime);
                Message = success ? "Tee time updated successfully!" : "Failed to update tee time.";

                if (role.Equals("admin", StringComparison.OrdinalIgnoreCase) || role.Equals("shopclerk", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToPage("/EmployeePortal", new { tab = "manage" });
                }


                return RedirectToPage("/MyTeeTimes");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tee time");
                Message = "An error occurred.";
                return Page();
            }
        }
    }
}
