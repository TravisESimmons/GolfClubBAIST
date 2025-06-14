using System;
using System.Collections.Generic;

namespace GolfBAIST.Models
{
    public class TeeTime
    {
        public int TeeTimeID { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; } 
        public TimeSpan EndTime { get; set; } 
        public int MemberID { get; set; }
        public int Players { get; set; }
        public string Phone { get; set; }
        public int? Carts { get; set; }
        public string EmployeeName { get; set; }
        public int? ScoreID { get; set; }
        public List<int> AdditionalMemberIDs { get; set; } = new List<int>();
        public bool CancellationRequested { get; set; }
    }
}