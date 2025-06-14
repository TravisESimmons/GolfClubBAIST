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
    public class BookingTeeTimeModel : PageModel
    {
        private readonly ILogger<BookingTeeTimeModel> _logger;
        private readonly TeeTimesService _teeTimesService;

        public BookingTeeTimeModel(ILogger<BookingTeeTimeModel> logger, TeeTimesService teeTimesService)
        {
            _logger = logger;
            _teeTimesService = teeTimesService;
        }

        [BindProperty]
        public TeeTime TeeTime { get; set; }

        public List<TeeTime> AvailableJoinableTeeTimes { get; set; } = new List<TeeTime>();

        public string Message { get; set; }

        public void OnGet()
        {
            int? memberId = HttpContext.Session.GetInt32("MemberID");

            TeeTime = new TeeTime();

            if (memberId.HasValue)
            {
                TeeTime.MemberID = memberId.Value;

                var member = _teeTimesService.GetMemberById(memberId.Value);
                if (member != null)
                {
                    TeeTime.Phone = FormatPhoneNumber(member.Phone); 
                }

                AvailableJoinableTeeTimes = _teeTimesService.GetJoinableTeeTimes();
            }
            else
            {
                Message = "You must log in to book a tee time.";
                AvailableJoinableTeeTimes = new List<TeeTime>();
            }
        }

        public IActionResult OnPost()
        {
            try
            {
                if (TimeSpan.TryParse(Request.Form["TeeTime.StartTime"], out var parsedStart))
                    TeeTime.StartTime = parsedStart;

                if (TimeSpan.TryParse(Request.Form["TeeTime.EndTime"], out var parsedEnd))
                    TeeTime.EndTime = parsedEnd;

                _teeTimesService.ValidateTeeTimeParameters(TeeTime);

                if (!_teeTimesService.IsTimeSlotAvailable(TeeTime.Date, TeeTime.StartTime, TeeTime.EndTime))
                {
                    Message = "Selected time is not available.";
                    return Page();
                }

                bool success = _teeTimesService.AddTeeTime(TeeTime, out int teeTimeId, out int scoreId);
                Message = success ? "Tee time booked successfully!" : "Booking failed.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Booking failed");
                Message = "An error occurred.";
            }

            AvailableJoinableTeeTimes = _teeTimesService.GetJoinableTeeTimes();
            return Page();
        }

        public IActionResult OnPostJoin(int teeTimeId)
        {
            try
            {
                int? memberId = HttpContext.Session.GetInt32("MemberID");
                if (!memberId.HasValue) return RedirectToPage("/Login");

                bool joined = _teeTimesService.JoinTeeTime(teeTimeId, memberId.Value);
                Message = joined ? "Successfully joined tee time!" : "Unable to join.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Join failed");
                Message = "An error occurred while joining.";
            }

            AvailableJoinableTeeTimes = _teeTimesService.GetJoinableTeeTimes();
            return Page();
        }
        private string FormatPhoneNumber(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            var digits = new string(raw.Where(char.IsDigit).ToArray());
            return digits.Length == 10
                ? $"({digits[..3]}) {digits[3..6]}-{digits[6..]}"
                : raw;
        }
    }
}
