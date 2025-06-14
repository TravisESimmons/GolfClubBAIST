using GolfBAIST.Models;
using GolfBAIST.TechnicalServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GolfBAIST.Pages
{
    public class EmployeePortalModel : PageModel
    {
        private readonly UserService _userService;
        private readonly TeeTimesService _teeTimesService;
        private readonly StandingService _standingService;
        private readonly MemberService _memberService;
        private readonly MembershipApplicationService _applicationService;
        private readonly ILogger<EmployeePortalModel> _logger;

        public string FullName { get; set; }
        public string Role { get; set; }

        public List<TeeTime> CancellationRequests { get; set; }
        public List<StandingTeeTimeRequest> StandingCancellationRequests { get; set; }
        public List<StandingTeeTimeRequest> StandingTeeTimeRequests { get; set; }
        public List<MembershipApplication> PendingApplications { get; set; }
        public List<TeeTime> AllTeeTimes { get; set; }
        public List<TeeTime> FilteredTeeTimes { get; set; }
        public List<Member> SelectedTeeTimePlayers { get; set; }
        public List<Score> Scorecards { get; set; }
        public Dictionary<int, string> MemberNames { get; set; } = new();


        [BindProperty(SupportsGet = true)]
        public string ActiveTab { get; set; }

        [BindProperty]
        public int TeeTimeID { get; set; }

        [BindProperty]
        public int MemberID { get; set; }

        [BindProperty]
        public int RequestID { get; set; }

        public bool ScrolledToPlayers { get; set; }

        [BindProperty]
        public string TeeTimeSearchTerm { get; set; }

        private readonly ScoresService _scoresService;

        public EmployeePortalModel(
            UserService userService,
            TeeTimesService teeTimesService,
            StandingService standingService,
            MemberService memberService,
            MembershipApplicationService applicationService,
            ScoresService scoresService, // << add this
            ILogger<EmployeePortalModel> logger)
        {
            _userService = userService;
            _teeTimesService = teeTimesService;
            _standingService = standingService;
            _memberService = memberService;
            _applicationService = applicationService;
            _scoresService = scoresService; // << assign here too
            _logger = logger;
        }


        public void OnGet()
        {
            LoadUserInfo();

            var tabQuery = Request.Query["tab"].ToString();
            ActiveTab = !string.IsNullOrEmpty(tabQuery)
                ? tabQuery
                : (Role?.ToLower() == "committee" ? "members" : "manage");

            LoadAllData();

            FilteredTeeTimes = AllTeeTimes;

            if (TempData["ScrollToPlayers"] != null)
            {
                var id = Convert.ToInt32(TempData["ScrollToPlayers"]);
                SelectedTeeTimePlayers = _teeTimesService.GetPlayersByTeeTimeID(id);
                TeeTimeID = id;
                ScrolledToPlayers = true;
            }
        }

        public int GetPlayerCount(int teeTimeID)
        {
            var players = _teeTimesService.GetPlayersByTeeTimeID(teeTimeID);
            return players?.Count ?? 0;
        }

        public List<Member> GetPlayersByTeeTimeID(int teeTimeID)
        {
            return _teeTimesService.GetPlayersByTeeTimeID(teeTimeID) ?? new List<Member>();
        }



        public IActionResult OnPostSearchTeeTime()
        {
            LoadUserInfo();
            ActiveTab = "scores";
            LoadAllData();

            if (int.TryParse(TeeTimeSearchTerm, out int searchId))
            {
                FilteredTeeTimes = AllTeeTimes
                    .Where(t => t.TeeTimeID == searchId)
                    .ToList();
            }
            else
            {
                FilteredTeeTimes = new List<TeeTime>();
            }

            return Page();
        }

        public IActionResult OnPostSearchTeeTimeManage()
        {
            LoadUserInfo();
            ActiveTab = "manage";
            LoadAllData();

            if (!string.IsNullOrEmpty(TeeTimeSearchTerm))
            {
                FilteredTeeTimes = AllTeeTimes
                    .Where(t =>
                        t.TeeTimeID.ToString().Contains(TeeTimeSearchTerm) ||
                        t.Date.ToString("yyyy-MM-dd").Contains(TeeTimeSearchTerm))
                    .ToList();
            }
            else
            {
                FilteredTeeTimes = AllTeeTimes;
            }

            return Page();
        }

        public JsonResult OnPostDeleteAjax([FromBody] TeeTimeDeleteModel model)
        {
            try
            {
                _teeTimesService.DeleteTeeTime(model.TeeTimeId);
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to delete TeeTimeID {TeeTimeId}: {Error}", model.TeeTimeId, ex.Message);
                return new JsonResult(new { success = false, error = "Deletion failed." });
            }
        }

        public class TeeTimeDeleteModel
        {
            public int TeeTimeId { get; set; }
        }

        public async Task<IActionResult> OnPostPlayersAjaxAsync([FromBody] TeeTimeRequestModel request)
        {
            if (request?.TeeTimeId == null)
                return BadRequest();

            var players = _teeTimesService.GetPlayersByTeeTimeID(request.TeeTimeId.Value);

            var basePath = HttpContext.Request.PathBase.Value ?? "";

            var html = new StringBuilder();
            html.Append("<h5>Players for Selected Tee Time</h5><ul class='list-group'>");

            foreach (var player in players)
            {
                var score = _scoresService.GetScoreByMemberAndTeeTime(player.MemberID, request.TeeTimeId.Value);
                int scoreId = score?.ScoreID ?? 0;

                html.Append($@"
<li class='list-group-item d-flex justify-content-between align-items-center'>
    <span>{player.FirstName} {player.LastName} (ID: {player.MemberID})</span>
    <a class='btn btn-sm btn-secondary' href='{basePath}/EmployeeViewScoreSheet?scoreID={scoreId}'>View Scorecard</a>
</li>");
            }

            html.Append("</ul>");
            return Content(html.ToString(), "text/html");
        }



        public class TeeTimeRequestModel
        {
            public int? TeeTimeId { get; set; }
        }



        public IActionResult OnPostDelete()
        {
            LoadUserInfo();
            _teeTimesService.DeleteTeeTime(TeeTimeID);
            return RedirectToPage(new { tab = "manage" });
        }

        public IActionResult OnPostViewPlayers()
        {
            LoadUserInfo();
            ActiveTab = "scores";
            ScrolledToPlayers = true;

            LoadAllData();
            SelectedTeeTimePlayers = _teeTimesService.GetPlayersByTeeTimeID(TeeTimeID);
            TempData["ScrollToPlayers"] = TeeTimeID;

            return RedirectToPage(new { tab = "scores" });
        }

        public IActionResult OnPostApproveMember()
        {
            LoadUserInfo();
            _logger.LogInformation("Approving MemberID: {MemberID}", MemberID);
            _memberService.ApproveMember(MemberID);
            return RedirectToPage(new { tab = "members" });
        }

        public IActionResult OnPostCancelTeeTime()
        {
            LoadUserInfo();
            _logger.LogInformation("Cancelling TeeTimeID: {TeeTimeID}", TeeTimeID);
            _teeTimesService.ApproveCancellation(TeeTimeID);
            return RedirectToPage(new { tab = "cancel" });
        }

        public IActionResult OnPostDenyCancellation()
        {
            LoadUserInfo();
            _logger.LogInformation("Denying Cancellation for TeeTimeID: {TeeTimeID}", TeeTimeID);
            _teeTimesService.DenyCancellation(TeeTimeID);
            return RedirectToPage(new { tab = "cancel" });
        }

        public IActionResult OnPostApproveStandingCancellation()
        {
            LoadUserInfo();
            _logger.LogInformation("Approving Standing Tee Cancellation RequestID: {RequestID}", RequestID);
            _standingService.DeleteStandingRequest(RequestID);

            TempData["StatusMessage"] = "✅ Standing tee cancellation approved.";
            return RedirectToPage(new { tab = "standing" });
        }


        public IActionResult OnPostDenyStandingCancellation()
        {
            LoadUserInfo();
            _logger.LogInformation("Denying Standing Tee Cancellation RequestID: {RequestID}", RequestID);
            _standingService.DenyCancellation(RequestID);
            TempData["StatusMessage"] = "✅ Standing tee cancellation approved.";
            return RedirectToPage(new { tab = "standing" });
        }

        public IActionResult OnPostApproveStandingRequest()
        {
            LoadUserInfo();
            string approvedBy = HttpContext.Session.GetString("Username") ?? "Employee";
            _logger.LogInformation("Approving Standing Tee RequestID: {RequestID} by {ApprovedBy}", RequestID, approvedBy);
            _standingService.ApproveStandingTeeTimeRequest(RequestID, approvedBy);
            return RedirectToPage(new { tab = "standing" });
        }

        public IActionResult OnPostDenyStandingRequest()
        {
            LoadUserInfo();
            _logger.LogInformation("Denying Standing Tee RequestID: {RequestID}", RequestID);
            _standingService.DenyStandingTeeTimeRequest(RequestID);
            return RedirectToPage(new { tab = "standing" });
        }

        private void LoadUserInfo()
        {
            var username = User.Identity?.Name;
            var user = _userService.GetUserByUsername(username);
            FullName = $"{user.FirstName} {user.LastName}";
            Role = user.Role;
        }

        private void LoadAllData()
        {
            CancellationRequests = _teeTimesService.GetCancellationRequests();
            StandingCancellationRequests = _standingService.GetStandingCancellationRequests();
            StandingTeeTimeRequests = _standingService.GetPendingStandingTeeTimeRequests();
            PendingApplications = _applicationService.GetPendingApplications();
            AllTeeTimes = _teeTimesService.GetAllTeeTimes();

            // Load member names for better display - include all member IDs from various sources
            var memberIds = new List<int>();

            // Add member IDs from AllTeeTimes
            if (AllTeeTimes != null)
            {
                memberIds.AddRange(AllTeeTimes.SelectMany(tt => new[] { tt.MemberID }.Concat(tt.AdditionalMemberIDs)));
            }

            // Add member IDs from CancellationRequests
            if (CancellationRequests != null)
            {
                memberIds.AddRange(CancellationRequests.Select(cr => cr.MemberID));
            }

            // Add member IDs from StandingTeeTimeRequests
            if (StandingTeeTimeRequests != null)
            {
                memberIds.AddRange(StandingTeeTimeRequests.Select(str => str.MemberID));
            }

            // Add member IDs from StandingCancellationRequests
            if (StandingCancellationRequests != null)
            {
                memberIds.AddRange(StandingCancellationRequests.Select(scr => scr.MemberID));
            }

            // Get distinct member IDs and load names
            var distinctMemberIds = memberIds.Distinct().ToList();
            MemberNames = _teeTimesService.GetMemberNames(distinctMemberIds);
        }

    }
}
