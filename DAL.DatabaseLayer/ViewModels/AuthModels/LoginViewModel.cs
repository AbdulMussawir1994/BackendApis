namespace DAL.DatabaseLayer.ViewModels.AuthModels;

public class LoginViewModel
{
    //  [Required]
    //  [RegularExpression(@"^\d+$", ErrorMessage = "CNIC Number must be numeric.")]
    public required string CNIC { get; set; }

    //  [Required]
    //  [RegularExpression(@"^\d{6}$", ErrorMessage = "Password must be a 6 digit number.")]
    public required string Password { get; set; }
}
