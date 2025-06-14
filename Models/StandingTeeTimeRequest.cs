namespace GolfBAIST.Models
{
    public class StandingTeeTimeRequest
    {
        public int RequestID { get; set; }
        public int MemberID { get; set; }
        public string RequestedDayOfWeek { get; set; }
        public TimeSpan RequestedStartTime { get; set; }
        public TimeSpan RequestedEndTime { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int? AdditionalPlayer1ID { get; set; }
        public int? AdditionalPlayer2ID { get; set; }
        public int? AdditionalPlayer3ID { get; set; }
        public TimeSpan? ApprovedTeeTime { get; set; }
        public string ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public int PriorityNumber { get; set; }
        public bool CancellationRequested { get; set; }
    }
}
