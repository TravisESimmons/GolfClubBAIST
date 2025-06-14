using GolfBAIST.Models;
using GolfBAIST.TechnicalServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

namespace GolfBAIST.Pages
{
    public class ViewScoreSheetModel : PageModel
    {
        private readonly ILogger<ViewScoreSheetModel> _logger;
        private readonly ScoresService _scoresService;

        [BindProperty]
        public Score Score { get; set; }

        [BindProperty]
        public int[] StrokesInput { get; set; } = new int[18];

        [BindProperty]
        public string CourseNameInput { get; set; }

        [BindProperty]
        public decimal CourseRating { get; set; }

        [BindProperty]
        public int SlopeRating { get; set; }



        public Dictionary<int, int> Strokes { get; set; } = new();

        public string CourseName => Score?.CourseName ?? "Default Course";
        public DateTime DatePlayed => Score?.Date ?? DateTime.Today;

        public ViewScoreSheetModel(ILogger<ViewScoreSheetModel> logger, ScoresService scoresService)
        {
            _logger = logger;
            _scoresService = scoresService;
        }

        public IActionResult OnGet(int? scoreID = null, int? memberID = null, int? teeTimeID = null)
        {
            int? sessionMemberId = HttpContext.Session.GetInt32("MemberID");

            // Handle the case where memberID and teeTimeID are provided (from employee portal)
            if (memberID.HasValue && teeTimeID.HasValue)
            {
                var foundScore = _scoresService.GetScoreByMemberAndTeeTime(memberID.Value, teeTimeID.Value);

                if (foundScore == null)
                {
                    _logger.LogInformation("No existing score found for MemberID: {MemberID} and TeeTimeID: {TeeTimeID}, creating new one", memberID, teeTimeID);

                    // Create a new score entry for this member/tee time
                    Score = new Score
                    {
                        TeeTimeID = teeTimeID.Value,
                        MemberID = memberID.Value,
                        Date = DateTime.Today,
                        CourseName = "Default Course",
                        CourseRating = 72.0m,
                        SlopeRating = 113,
                        HoleScores = "",
                        TotalScore = 0,
                        Differential = 0.0
                    };
                }
                else
                {
                    Score = foundScore;
                }
            }
            // Handle the case where scoreID is provided (normal flow)
            else if (scoreID.HasValue)
            {
                var original = _scoresService.GetScores().FirstOrDefault(s => s.ScoreID == scoreID);

                if (original == null)
                {
                    _logger.LogWarning("Score not found for ScoreID: {ScoreID}", scoreID);
                    TempData["Error"] = $"Score not found for Score ID: {scoreID}";
                    return Page();
                }

                Score = _scoresService.GetScoreByMemberAndTeeTime(sessionMemberId ?? original.MemberID, original.TeeTimeID)
                         ?? original;
            }
            else
            {
                _logger.LogWarning("No scoreID, memberID, or teeTimeID provided");
                TempData["Error"] = "Invalid parameters provided";
                return Page();
            }

            if (Score == null)
            {
                _logger.LogWarning("Unable to load score details");
                TempData["Error"] = "Unable to load score details";
                return Page();
            }

            if (!string.IsNullOrEmpty(Score.HoleScores))
            {
                try
                {
                    var deserialized = JsonSerializer.Deserialize<Dictionary<int, int>>(Score.HoleScores);
                    if (deserialized != null)
                        Strokes = deserialized;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to deserialize HoleScores.");
                }
            }

            return Page();
        }



        public IActionResult OnPostSubmitScore(int ScoreID, int[] Par, string[] TeeBox)
        {
            _logger.LogInformation("Submitting score for ScoreID: {ScoreID}", ScoreID);

            int? memberId = HttpContext.Session.GetInt32("MemberID");
            if (!memberId.HasValue)
            {
                _logger.LogWarning("MemberID not found in session.");
                return Unauthorized();
            }

            var existingScore = _scoresService.GetScores().FirstOrDefault(s => s.ScoreID == ScoreID);
            if (existingScore == null)
            {
                _logger.LogWarning("ScoreID not found: {ScoreID}", ScoreID);
                return NotFound();
            }

            int teeTimeID = existingScore.TeeTimeID;

            Score = _scoresService.GetScoreByMemberAndTeeTime(memberId.Value, teeTimeID) ?? new Score();

            bool isNew = false;

            if (Score == null)
            {
                _logger.LogInformation("No existing score found for MemberID: {MemberID}, creating new one", memberId.Value);
                Score = new Score
                {
                    TeeTimeID = teeTimeID,
                    MemberID = memberId.Value,
                    Date = existingScore.Date,
                    CourseName = CourseNameInput ?? "Default Course",
                    CourseRating = CourseRating,
                    SlopeRating = SlopeRating
                };
                isNew = true;
            }

            // Update score properties
            Score.CourseName = CourseNameInput ?? "Default Course";
            Score.CourseRating = CourseRating;
            Score.SlopeRating = SlopeRating;

            // Validate stroke inputs
            if (StrokesInput == null || StrokesInput.Length != 18)
            {
                ModelState.AddModelError("", "Invalid stroke data. Please ensure all 18 holes have scores.");
                return Page();
            }

            // Build hole scores dictionary and calculate total efficiently
            var holeScoresDict = new Dictionary<int, int>();
            int totalScore = 0;
            bool hasInvalidScores = false;

            for (int i = 0; i < StrokesInput.Length; i++)
            {
                int holeNumber = i + 1;
                int strokes = StrokesInput[i];

                // Validate stroke count
                if (strokes < 1 || strokes > 20)
                {
                    hasInvalidScores = true;
                    ModelState.AddModelError($"StrokesInput[{i}]", $"Invalid score for hole {holeNumber}. Must be between 1 and 20.");
                    continue;
                }

                holeScoresDict[holeNumber] = strokes;
                totalScore += strokes;
            }

            if (hasInvalidScores)
            {
                return Page();
            }

            // Serialize hole scores
            string holeScoresJson;
            try
            {
                holeScoresJson = JsonSerializer.Serialize(holeScoresDict);
                _logger.LogInformation("Serialized HoleScores: {HoleScores}", holeScoresJson);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to serialize hole scores");
                ModelState.AddModelError("", "Failed to process score data.");
                return Page();
            }

            Score.HoleScores = holeScoresJson;
            Score.TotalScore = totalScore;

            // Calculate differential with proper validation
            if (Score.SlopeRating <= 0)
            {
                ModelState.AddModelError("", "Invalid slope rating.");
                return Page();
            }

            double differential = ((totalScore - (double)Score.CourseRating) * 113.0) / Score.SlopeRating;
            Score.Differential = Math.Round(differential, 1);

            // Save score with better error handling
            bool success;
            try
            {
                if (isNew)
                {
                    int newScoreId = _scoresService.AddScore(Score);
                    Score.ScoreID = newScoreId;
                    success = newScoreId > 0;
                }
                else
                {
                    success = _scoresService.UpdateScore(Score);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save score for ScoreID: {ScoreID}", ScoreID);
                ModelState.AddModelError("", "An error occurred while saving the score. Please try again.");
                return Page();
            }

            if (success)
            {
                TempData["Message"] = $"Score saved successfully! Total: {totalScore}, Differential: {Score.Differential}";
                return RedirectToPage("/ScoreSuccess");
            }

            ModelState.AddModelError("", "Failed to save the score. Please try again.");
            return Page();
        }


        public int GetPar(int hole)
        {
            // Default pars for Club BAIST National
            var defaultPars = new Dictionary<int, int> {
                { 1, 4 }, { 2, 5 }, { 3, 3 }, { 4, 4 }, { 5, 4 },
                { 6, 3 }, { 7, 5 }, { 8, 4 }, { 9, 4 }, { 10, 4 },
                { 11, 4 }, { 12, 3 }, { 13, 5 }, { 14, 4 }, { 15, 4 },
                { 16, 3 }, { 17, 4 }, { 18, 4 }
            };

            // Course-specific pars (could be moved to database in the future)
            var coursePars = new Dictionary<string, Dictionary<int, int>>
            {
                ["Club BAIST National"] = defaultPars,
                ["Spruce Meadows"] = new Dictionary<int, int> {
                    { 1, 4 }, { 2, 4 }, { 3, 3 }, { 4, 5 }, { 5, 4 },
                    { 6, 3 }, { 7, 4 }, { 8, 5 }, { 9, 4 }, { 10, 4 },
                    { 11, 4 }, { 12, 3 }, { 13, 4 }, { 14, 5 }, { 15, 4 },
                    { 16, 3 }, { 17, 4 }, { 18, 4 }
                },
                ["Rocky Ridge"] = new Dictionary<int, int> {
                    { 1, 4 }, { 2, 5 }, { 3, 4 }, { 4, 3 }, { 5, 4 },
                    { 6, 4 }, { 7, 5 }, { 8, 3 }, { 9, 4 }, { 10, 4 },
                    { 11, 5 }, { 12, 3 }, { 13, 4 }, { 14, 4 }, { 15, 4 },
                    { 16, 3 }, { 17, 5 }, { 18, 4 }
                }
            };

            string courseName = Score?.CourseName ?? CourseNameInput ?? "Club BAIST National";

            if (coursePars.ContainsKey(courseName) && coursePars[courseName].ContainsKey(hole))
            {
                return coursePars[courseName][hole];
            }

            return defaultPars.ContainsKey(hole) ? defaultPars[hole] : 4;
        }

        /// <summary>
        /// Gets the total par for the selected course
        /// </summary>
        public int GetCoursePar()
        {
            return Enumerable.Range(1, 18).Sum(hole => GetPar(hole));
        }
    }
}
