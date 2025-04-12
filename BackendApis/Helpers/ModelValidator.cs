using DAL.DatabaseLayer.ViewModels.AuthModels;
using DAL.RepositoryLayer.IDataAccess;
using FluentValidation;

namespace BackendApis.Helpers;

// ✅ RegisterViewModelValidator
public class ModelValidator : AbstractValidator<RegisterViewModel>
{
    public ModelValidator(IDataBaseAccess userDataAccess)
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.")
            .WithMessage("Email is already registered.");

        RuleFor(x => x.CNIC)
            .NotEmpty().WithMessage("CNIC is required.")
            .Matches(@"^\d{13,}$").WithMessage("CNIC must be numeric with at least 13 digits.");

        RuleFor(x => x.MobileNo)
            .NotEmpty().WithMessage("Mobile number is required.")
            .Matches(@"^\+92\d{10,}$").WithMessage("Mobile number must start with +92 and be at least 13 digits.");
    }
}

// ✅ LoginViewModelValidator
public class LoginViewModelValidator : AbstractValidator<LoginViewModel>
{
    public LoginViewModelValidator()
    {
        RuleFor(x => x.CNIC)
            .NotEmpty().WithMessage("CNIC Number is required.")
            .Matches(@"^\d+$").WithMessage("CNIC Number must be numeric.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .Matches(@"^\d{6}$").WithMessage("Password must be exactly 6 digits.");
    }
}

//PasswordViewModelValidator
public class PasswordViewModelValidator : AbstractValidator<PasswordViewModel>
{
    public PasswordViewModelValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("User Id is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .Matches(@"^\d{6}$").WithMessage("Password must be a 6-digit number.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Confirm Password is required.")
            .Matches(@"^\d{6}$").WithMessage("Confirm Password must be a 6-digit number.")
            .Equal(x => x.Password).WithMessage("Password and Confirm Password do not match.");
    }
}
