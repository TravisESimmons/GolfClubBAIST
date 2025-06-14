using System;
using System.ComponentModel.DataAnnotations;

namespace GolfBAIST.Models
{
    public class Register
    {
        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;

        [Required]
        public string PostalCode { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string Phone { get; set; } = string.Empty;

        public string? AlternatePhone { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Range(typeof(DateTime), "01/01/1900", "12/31/2100", ErrorMessage = "Please enter a valid date of birth.")]
        public DateTime DateOfBirth { get; set; }


        [Required]
        public string Occupation { get; set; } = string.Empty;

        [Required]
        public string CompanyName { get; set; } = string.Empty;

        [Required]
        public bool ConsentToEmail { get; set; }

        [Required]
        public bool AgreementAcknowledged { get; set; } 

        [Required]
        public int MembershipTypeID { get; set; }

        [Required]
        [Display(Name = "Sponsor 1 (Member ID)")]
        public int? Sponsor1 { get; set; } = null;

        [Required]
        [Display(Name = "Sponsor 2 (Member ID)")]
        public int? Sponsor2 { get; set; } = null;

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
