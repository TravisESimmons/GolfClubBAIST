namespace GolfBAIST.Models
{
    public class MembershipType
    {
        public int MembershipTypeID { get; set; }
        public string TypeName { get; set; }
        public decimal Fee { get; set; }
        public bool GolfPrivileges { get; set; }
    }
}


