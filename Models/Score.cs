namespace GolfBAIST.Models
{
    public class Score
    {
        public int ScoreID { get; set; }
        public int TeeTimeID { get; set; } 
        public int MemberID { get; set; }
        public DateTime Date { get; set; }
        public string CourseName { get; set; }
        public decimal CourseRating { get; set; }
        public int SlopeRating { get; set; }
        public string HoleScores { get; set; }  
        public int TotalScore { get; set; }
        public double Differential { get; set; }
        public Member Member { get; set; } 
    }
}
