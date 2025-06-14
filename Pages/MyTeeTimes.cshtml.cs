using GolfBAIST.Models;
using GolfBAIST.TechnicalServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace GolfBAIST.Pages
{
    public class MyTeeTimesModel : PageModel
    {
        private readonly ILogger<MyTeeTimesModel> _logger;
        private readonly TeeTimesService _teeTimesService;
        private readonly StandingService _standingService;

        [BindProperty]
        public TeeTime TeeTime { get; set; }
        public List<TeeTime> TeeTimes { get; set; } = new();
        public List<StandingTeeTimeRequest> StandingTeeTimes { get; set; } = new();
        public Dictionary<int, string> MemberNames { get; set; } = new();

        public string StatusMessage { get; set; }

        [BindProperty(SupportsGet = true, Name = "p")]
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 5;

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Tab { get; set; } = "regular";

        public int? LoggedInMemberID { get; set; }
        public string Role { get; set; }

        public MyTeeTimesModel(ILogger<MyTeeTimesModel> logger, TeeTimesService teeTimesService, StandingService standingService)
        {
            _logger = logger;
            _teeTimesService = teeTimesService;
            _standingService = standingService;
        }

        public void OnGet()
        {
            StatusMessage = TempData["StatusMessage"] as string;

            int? memberId = HttpContext.Session.GetInt32("MemberID");
            string role = HttpContext.Session.GetString("Role") ?? "";

            LoggedInMemberID = memberId;
            Role = role;

            if (!memberId.HasValue || role == "Employee")
            {
                TeeTimes = new();
                StandingTeeTimes = new();
                MemberNames = new();
                return;
            }

            var allTeeTimes = _teeTimesService.GetTeeTimesByMemberID(memberId.Value);
            allTeeTimes = allTeeTimes.OrderByDescending(tt => tt.TeeTimeID).ToList();

            var allStandingTeeTimes = _standingService.GetStandingTeeTimesByMemberID(memberId.Value);

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                string term = SearchTerm.ToLower();
                var matchedMemberNames = _teeTimesService.GetMemberNames(
                    allTeeTimes.SelectMany(tt => tt.AdditionalMemberIDs).Distinct().ToList()
                );

                // Apply search to regular tee times
                allTeeTimes = allTeeTimes.Where(tt =>
                    (tt.Phone?.ToLower().Contains(term) ?? false) ||
                    tt.Date.ToString("yyyy-MM-dd").Contains(term) ||
                    (tt.EmployeeName?.ToLower().Contains(term) ?? false) ||
                    matchedMemberNames.TryGetValue(tt.MemberID, out var primaryName) && primaryName.ToLower().Contains(term) ||
                    tt.AdditionalMemberIDs.Any(id => matchedMemberNames.ContainsKey(id) && matchedMemberNames[id].ToLower().Contains(term))
                ).ToList();

                // Apply search to standing tee times
                allStandingTeeTimes = allStandingTeeTimes.Where(stt =>
                    stt.RequestedDayOfWeek.ToLower().Contains(term) ||
                    stt.StartDate.ToString("yyyy-MM-dd").Contains(term) ||
                    stt.EndDate.ToString("yyyy-MM-dd").Contains(term) ||
                    matchedMemberNames.TryGetValue(stt.MemberID, out var standingPrimaryName) && standingPrimaryName.ToLower().Contains(term) ||
                    (stt.AdditionalPlayer1ID.HasValue && matchedMemberNames.ContainsKey(stt.AdditionalPlayer1ID.Value) && matchedMemberNames[stt.AdditionalPlayer1ID.Value].ToLower().Contains(term)) ||
                    (stt.AdditionalPlayer2ID.HasValue && matchedMemberNames.ContainsKey(stt.AdditionalPlayer2ID.Value) && matchedMemberNames[stt.AdditionalPlayer2ID.Value].ToLower().Contains(term)) ||
                    (stt.AdditionalPlayer3ID.HasValue && matchedMemberNames.ContainsKey(stt.AdditionalPlayer3ID.Value) && matchedMemberNames[stt.AdditionalPlayer3ID.Value].ToLower().Contains(term))
                ).ToList();
            }

            TotalPages = (int)Math.Ceiling(allTeeTimes.Count / (double)PageSize);
            if (CurrentPage < 1) CurrentPage = 1;
            if (CurrentPage > TotalPages) CurrentPage = TotalPages;

            TeeTimes = allTeeTimes
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            StandingTeeTimes = allStandingTeeTimes;

            var memberIds = allTeeTimes.Select(tt => tt.MemberID)
                .Concat(allTeeTimes.SelectMany(tt => tt.AdditionalMemberIDs))
                .Concat(StandingTeeTimes.SelectMany(stt => new[] {
                    stt.AdditionalPlayer1ID ?? 0,
                    stt.AdditionalPlayer2ID ?? 0,
                    stt.AdditionalPlayer3ID ?? 0
                }))
                .Where(id => id != 0)
                .Distinct()
                .ToList();

            MemberNames = _teeTimesService.GetMemberNames(memberIds);
        }

        public IActionResult OnGetEdit(int teeTimeId)
        {
            var allTeeTimes = _teeTimesService.GetTeeTimes();
            TeeTime = allTeeTimes.FirstOrDefault(t => t.TeeTimeID == teeTimeId);

            if (TeeTime == null)
            {
                _logger.LogWarning("TeeTime not found for ID: {ID}", teeTimeId);
                return RedirectToPage("/MyTeeTimes");
            }

            int? currentMemberId = HttpContext.Session.GetInt32("MemberID");
            string role = HttpContext.Session.GetString("Role") ?? "";

            if (role != "Employee" && TeeTime.MemberID != currentMemberId)
            {
                _logger.LogWarning("Unauthorized access attempt to edit TeeTimeID: {ID}", teeTimeId);
                return RedirectToPage("/MyTeeTimes");
            }

            return Page();
        }

        public IActionResult OnPostLeaveTeeTime(int id)
        {
            int? memberId = HttpContext.Session.GetInt32("MemberID");
            if (!memberId.HasValue)
            {
                TempData["StatusMessage"] = "You must be logged in.";
                return RedirectToPage();
            }

            bool result = _teeTimesService.RemoveAdditionalMember(id, memberId.Value);

            TempData["StatusMessage"] = result
                ? "✅ You left the tee time successfully."
                : "⚠️ You were not part of this tee time.";

            return RedirectToPage();
        }

        [IgnoreAntiforgeryToken]
        public IActionResult OnPostRequestCancellation([FromBody] CancelRequest data)
        {
            _logger.LogInformation("AJAX Regular Cancel: Received ID = {ID}", data.Id);

            try
            {
                int? memberId = HttpContext.Session.GetInt32("MemberID");
                string role = HttpContext.Session.GetString("Role") ?? "User";

                if (!memberId.HasValue)
                {
                    return new JsonResult(new { success = false, error = "Member ID not found in session" });
                }

                _teeTimesService.RequestCancellation(data.Id, memberId.Value, role);
                _logger.LogInformation("Cancellation request submitted for TeeTimeID {TeeTimeID} by MemberID {MemberID}", data.Id, memberId.Value);

                return new JsonResult(new { success = true, message = "Cancellation request submitted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting cancellation for TeeTimeID {TeeTimeID}", data.Id);
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }



        [IgnoreAntiforgeryToken]
        public IActionResult OnPostRequestStandingCancellation([FromBody] CancelRequest data)
        {
            _logger.LogInformation("AJAX Standing Cancel: Received ID = {ID}", data.Id);
            _standingService.RequestCancellation(data.Id);

            return new JsonResult(new { success = true });
        }


        public class CancelRequest
        {
            public int Id { get; set; }
        }
    }
}
