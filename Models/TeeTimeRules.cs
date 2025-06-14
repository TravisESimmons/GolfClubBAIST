namespace GolfBAIST.Models
{
    public class TeeTimeRules
    {
        public static Dictionary<string, List<(TimeSpan start, TimeSpan end)>> MembershipTimeRestrictions = new Dictionary<string, List<(TimeSpan, TimeSpan)>>()
        {
            {
                "Gold", new List<(TimeSpan, TimeSpan)>
                {
                    (TimeSpan.Parse("00:00"), TimeSpan.Parse("23:59")) 
                }
            },
            {
                "Silver", new List<(TimeSpan, TimeSpan)>
                {
                    (TimeSpan.Parse("00:00"), TimeSpan.Parse("15:00")),
                    (TimeSpan.Parse("17:30"), TimeSpan.Parse("23:59"))
                }
            },
            {
                "Bronze", new List<(TimeSpan, TimeSpan)>
                {
                    (TimeSpan.Parse("00:00"), TimeSpan.Parse("15:00")),
                    (TimeSpan.Parse("18:00"), TimeSpan.Parse("23:59"))
                }
            }
        };
    }
}
