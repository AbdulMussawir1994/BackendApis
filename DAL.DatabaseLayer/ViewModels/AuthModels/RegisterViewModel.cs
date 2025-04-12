namespace DAL.DatabaseLayer.ViewModels.AuthModels;

public class RegisterViewModel
{
    //[Required(ErrorMessage = "Username is required.")]
    //[StringLength(50, MinimumLength = 6, ErrorMessage = "Username must be at least 6 characters long.")]
    public string Username { get; set; }

    //    [Required(ErrorMessage = "Email is required.")]
    //    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; }

    // [Required(ErrorMessage = "CNIC Number is required.")]
    //  [RegularExpression(@"^\d{13,}$", ErrorMessage = "IC Number must be a numeric value with at least 13 digits.")]
    public string CNIC { get; set; }

    // [Required(ErrorMessage = "Mobile number is required.")]
    //[RegularExpression(@"^\+92\d{10,}$", ErrorMessage = "Mobile number must start with +60 and contain at least 10 additional digits.")]
    public string MobileNo { get; set; }
}
