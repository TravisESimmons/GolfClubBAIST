namespace GolfBAIST.Models
{
    public class Member
    {
        public int MemberID { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string Address { get; set; }
        public string PostalCode { get; set; }
        public string Phone { get; set; }
        public string AlternatePhone { get; set; }
        public string Email { get; set; }
        public DateTime DateOfBirth { get; set; }

        public string Occupation { get; set; }
        public string CompanyName { get; set; }
        public bool ConsentToEmail { get; set; }
        public bool AgreementAcknowledged { get; set; }

        public int MembershipTypeID { get; set; }
        public int? Sponsor1 { get; set; }
        public int? Sponsor2 { get; set; }
        public string MembershipStatus { get; set; }

        public decimal AnnualDues { get; set; }
        public decimal FoodAndBeverageCharge { get; set; }
        public decimal OutstandingBalance { get; set; }
        public string FullName => $"{FirstName} {LastName}";
    }
}
