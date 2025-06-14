using System.ComponentModel.DataAnnotations;

public class User
{
    public int UserID { get; set; }

    [Required]
    public string Username { get; set; }

    [Required]
    public string Password { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required] 
    public string FirstName { get; set; }

    [Required]  
    public string LastName { get; set; }

    [Required]  
    public string Role { get; set; }

    public int? MemberID { get; set; } 


    public bool IsEmployee { get; set; }
}
