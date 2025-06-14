using GolfBAIST.TechnicalServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using GolfBAIST.Models;

namespace GolfBAIST.Pages
{
    public class StandingTeeTimeModel : PageModel
    {
        private readonly StandingService _standingService;
        private readonly ILogger<StandingTeeTimeModel> _logger;
        private readonly string _connectionString = @"Server=localhost\SQLEXPRESS;Database=GolfBAIST_Local;Trusted_Connection=True;TrustServerCertificate=True;";

        public StandingTeeTimeModel(StandingService standingService, ILogger<StandingTeeTimeModel> logger)
        {
            _standingService = standingService;
            _logger = logger;
        }

        [BindProperty] public int MemberID { get; set; }
        [BindProperty] public string ShareholderName { get; set; }
        [BindProperty] public string ShareholderID { get; set; }
        [BindProperty] public string DayOfWeek { get; set; }
        [BindProperty] public TimeSpan StartTime { get; set; }
        [BindProperty] public TimeSpan EndTime { get; set; }
        [BindProperty] public DateTime StartDate { get; set; }
        [BindProperty] public DateTime EndDate { get; set; }
        [BindProperty] public int? AdditionalPlayer1ID { get; set; }
        [BindProperty] public int? AdditionalPlayer2ID { get; set; }
        [BindProperty] public int? AdditionalPlayer3ID { get; set; }

        [TempData]
        public string StatusMessage { get; set; }
        public string Message { get; private set; }
        public bool IsShareholder { get; private set; }

        public void OnGet()
        {
            if (TempData.ContainsKey("SuccessMessage"))
            {
                Message = TempData["SuccessMessage"]?.ToString();
                return;
            }

            int? memberId = HttpContext.Session.GetInt32("MemberID");
            if (memberId.HasValue)
            {
                MemberID = memberId.Value;
                ShareholderID = memberId.Value.ToString();
                ShareholderName = GetMemberName(MemberID);

                int? membershipTypeID = _standingService.GetMembershipTypeID(MemberID);
                IsShareholder = membershipTypeID == 1;

                if (!IsShareholder)
                {
                    Message = "In order to make a standing tee time, a Gold Shareholder membership is required.";
                }
            }
            else
            {
                Message = "Please log in to book a standing tee time.";
            }
        }

        public IActionResult OnPost()
        {
            int totalPlayers = 1;
            if (AdditionalPlayer1ID > 0) totalPlayers++;
            if (AdditionalPlayer2ID > 0) totalPlayers++;
            if (AdditionalPlayer3ID > 0) totalPlayers++;

            if (totalPlayers != 4)
            {
                Message = "Standing tee time requests must include exactly 4 players.";
                return Page();
            }

            int? memberId = HttpContext.Session.GetInt32("MemberID");
            if (!memberId.HasValue)
            {
                Message = "Please log in to book a standing tee time.";
                return Page();
            }

            MemberID = memberId.Value;

            if (StartDate > EndDate)
            {
                Message = "Start date cannot be after end date.";
                return Page();
            }

            try
            {
                string calculatedDayOfWeek = StartDate.DayOfWeek.ToString();

                var request = new StandingTeeTimeRequest
                {
                    MemberID = MemberID,
                    RequestedDayOfWeek = calculatedDayOfWeek,
                    RequestedStartTime = StartTime,
                    RequestedEndTime = EndTime,
                    StartDate = StartDate,
                    EndDate = EndDate,
                    AdditionalPlayer1ID = AdditionalPlayer1ID,
                    AdditionalPlayer2ID = AdditionalPlayer2ID,
                    AdditionalPlayer3ID = AdditionalPlayer3ID
                };

                _standingService.ValidateStandingTeeTimeRequest(request);

                var success = _standingService.CreateStandingTeeTimeRequest(MemberID, calculatedDayOfWeek, StartTime, EndTime, StartDate, EndDate, new List<int>
                {
                    MemberID,
                    AdditionalPlayer1ID ?? 0,
                    AdditionalPlayer2ID ?? 0,
                    AdditionalPlayer3ID ?? 0
                });

                if (success)
                {
                    TempData["SuccessMessage"] = "Standing Tee Time request successfully created.";
                    return RedirectToPage();
                }

                Message = "Failed to create Standing Tee Time request.";
            }
            catch (ArgumentException ex)
            {
                Message = ex.Message;
            }
            catch (Exception)
            {
                Message = "An unexpected error occurred while processing your request.";
            }

            return Page();
        }

        private string GetMemberName(int memberId)
        {
            using SqlConnection connection = new SqlConnection(_connectionString);
            connection.Open();
            SqlCommand command = new SqlCommand("SELECT FirstName, LastName FROM Members WHERE MemberID = @MemberID", connection);
            command.Parameters.AddWithValue("@MemberID", memberId);

            using SqlDataReader reader = command.ExecuteReader();
            return reader.Read() ? $"{reader["FirstName"]} {reader["LastName"]}" : "Unknown Member";
        }
    }
}
