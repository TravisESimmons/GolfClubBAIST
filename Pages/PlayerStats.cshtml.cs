using GolfBAIST.Models;
using GolfBAIST.TechnicalServices;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GolfBAIST.Pages
{
    public class PlayerStatsModel : PageModel
    {
        private readonly ScoresService _scoresService;

        public PlayerStatsModel(ScoresService scoresService)
        {
            _scoresService = scoresService;
        }

        public List<Score> Scores { get; set; } = new();
        public List<double> ScoreDifferentials { get; set; } = new();
        public double HandicapIndex { get; set; }
        public List<(Score Score, double Differential)> ScoresWithDifferentials { get; set; } = new();


        public void OnGet()
        {
            int? memberId = HttpContext.Session.GetInt32("MemberID");
            if (!memberId.HasValue) return;

            Scores = _scoresService.GetScores()
                .Where(s => s.MemberID == memberId.Value && s.TotalScore > 0 && s.SlopeRating > 0)
                .OrderByDescending(s => s.Date)
                .Take(20)
                .ToList();

            foreach (var score in Scores)
            {
                double differential = (score.TotalScore - (double)score.CourseRating) * 113 / score.SlopeRating;
                ScoreDifferentials.Add(differential);
                ScoresWithDifferentials.Add((score, differential));
            }

            var best8 = ScoreDifferentials.OrderBy(d => d).Take(8).ToList();
            HandicapIndex = best8.Any() ? best8.Average() * 0.96 : 0;
            HandicapIndex = Math.Round(HandicapIndex, 1);
        }
    }
}
