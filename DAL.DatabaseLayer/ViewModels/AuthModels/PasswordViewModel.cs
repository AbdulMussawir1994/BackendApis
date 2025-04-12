using System.ComponentModel.DataAnnotations;

namespace DAL.DatabaseLayer.ViewModels.AuthModels;

public class PasswordViewModel
{
    public string Id { get; set; }

    [DataType(DataType.Password)]
    public string Password { get; set; }

    [Display(Name = "Confirm Password")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; }
}