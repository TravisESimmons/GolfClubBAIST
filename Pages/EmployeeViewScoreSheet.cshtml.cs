using GolfBAIST.Models;
using GolfBAIST.TechnicalServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace GolfBAIST.Pages.Employees
{
    public class EmployeeViewScoreSheetModel : PageModel
    {
        private readonly ScoresService _scoresService;

        public EmployeeViewScoreSheetModel(ScoresService scoresService)
        {
            _scoresService = scoresService;
        }

        [BindProperty] public Score Score { get; set; }
        [BindProperty] public Dictionary<int, int> Strokes { get; set; } = new();

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty(SupportsGet = true)]
        public int TeeTimeID { get; set; }

        public IActionResult OnGet(int scoreId)
        {
            Score = _scoresService.GetScoreByID(scoreId);
            if (Score == null) return NotFound();

            TeeTimeID = Score.TeeTimeID;

            if (!string.IsNullOrEmpty(Score.HoleScores))
            {
                try
                {
                    var deserialized = JsonSerializer.Deserialize<Dictionary<string, int>>(Score.HoleScores);
                    if (deserialized != null)
                    {
                        foreach (var kv in deserialized)
                        {
                            int hole = int.Parse(kv.Key.Replace("Hole", ""));
                            Strokes[hole] = kv.Value;
                        }
                    }
                }
                catch
                {
                    // fallback to default strokes if JSON malformed
                }
            }

            return Page();
        }

        public IActionResult OnPost()
        {
            ModelState.Remove("Score.Member");

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var holeScores = new Dictionary<string, int>();
            int total = 0;

            foreach (var stroke in Strokes)
            {
                holeScores.Add($"Hole{stroke.Key}", stroke.Value);
                total += stroke.Value;
            }

            Score.HoleScores = JsonSerializer.Serialize(holeScores);
            Score.TotalScore = total;

            if (Score.Date < new DateTime(1753, 1, 1))
            {
                ModelState.AddModelError("Score.Date", "Invalid date.");
                return Page();
            }

            bool success = _scoresService.UpdateScore(Score);

            if (success)
            {
                StatusMessage = "✅ Score successfully updated.";
                return RedirectToPage("/EmployeeViewScoreSheet", new
                {
                    scoreId = Score.ScoreID,
                    TeeTimeID = Score.TeeTimeID
                });
            }

            ModelState.AddModelError("", "Error saving score.");
            return Page();
        }

        public int GetPar(int hole)
        {
            var pars = new Dictionary<int, int>
            {
                {1, 4},{2, 5},{3, 3},{4, 4},{5, 4},
                {6, 3},{7, 5},{8, 4},{9, 4},{10, 4},
                {11, 4},{12, 3},{13, 5},{14, 4},{15, 4},
                {16, 3},{17, 4},{18, 4}
            };
            return pars[hole];
        }
    }
}
