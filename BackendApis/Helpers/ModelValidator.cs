using DAL.DatabaseLayer.ViewModels.AuthModels;
using DAL.DatabaseLayer.ViewModels.EmployeeModels;
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

// ✅ PasswordViewModelValidator
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

// ✅ CreateEmployeeValidator
public class CreateEmployeeValidator : AbstractValidator<CreateEmployeeViewModel>
{
    private static readonly string[] AllowedFileExtensions = { ".jpg", ".jpeg", ".png", ".pdf" };
    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png" };
    private const long MaxFileSize = 5 * 1024 * 1024;

    public CreateEmployeeValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MinimumLength(6).WithMessage("Name must be at least 6 characters.")
            .MaximumLength(50).WithMessage("Name must not exceed 50 characters.");

        RuleFor(x => x.Age)
            .InclusiveBetween(18, 65).WithMessage("Age must be between 18 and 65.");

        RuleFor(x => x.Salary)
            .InclusiveBetween(0, 1_000_000).WithMessage("Salary must be between 0 and 1 million.");

        RuleFor(x => x.CV)
            .Must(HaveValidExtension).When(x => x.CV != null)
            .WithMessage("CV must be .jpg, .jpeg, .png, or .pdf.")
            .Must(HaveValidFileSize).When(x => x.CV != null)
            .WithMessage("CV must not exceed 5MB.");

        RuleFor(x => x.Image)
            .Must(HaveValidImageExtension).When(x => x.Image != null)
            .WithMessage("Image must be .jpg, .jpeg, or .png.")
            .Must(HaveValidFileSize).When(x => x.Image != null)
            .WithMessage("Image must not exceed 5MB.");

        RuleFor(x => x.ApplicationUserId)
            .NotEmpty().WithMessage("Application user ID is required.");
    }

    private static bool HaveValidExtension(IFormFile file) =>
        file != null && AllowedFileExtensions.Contains(Path.GetExtension(file.FileName).ToLowerInvariant());

    private static bool HaveValidImageExtension(IFormFile file) =>
        file != null && AllowedImageExtensions.Contains(Path.GetExtension(file.FileName).ToLowerInvariant());

    private static bool HaveValidFileSize(IFormFile file) =>
        file.Length <= MaxFileSize;
}

// ✅ UpdateEmployeeValidator
public class UpdateEmployeeValidator : AbstractValidator<UpdateEmployeeViewModel>
{
    private static readonly string[] AllowedFileExtensions = { ".jpg", ".jpeg", ".png", ".pdf" };
    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png" };
    private const long MaxFileSize = 5 * 1024 * 1024;

    public UpdateEmployeeValidator()
    {
        RuleFor(x => x.Id)
         .NotEmpty().WithMessage("Employee Id is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

        RuleFor(x => x.Age)
            .InclusiveBetween(18, 65).WithMessage("Age must be between 18 and 65.");

        RuleFor(x => x.Salary)
            .InclusiveBetween(0, 100_000_000).WithMessage("Salary must be between 0 and 100 million.");

        RuleFor(x => x.CV)
            .Must(HaveValidExtension).When(x => x.CV != null)
            .WithMessage("CV must be .jpg, .jpeg, .png, or .pdf.")
            .Must(HaveValidFileSize).When(x => x.CV != null)
            .WithMessage("CV must not exceed 5MB.");

        RuleFor(x => x.Image)
            .Must(HaveValidImageExtension).When(x => x.Image != null)
            .WithMessage("Image must be .jpg, .jpeg, or .png.")
            .Must(HaveValidFileSize).When(x => x.Image != null)
            .WithMessage("Image must not exceed 5MB.");

        RuleFor(x => x.ApplicationUserId)
            .NotEmpty().WithMessage("Application user ID is required.");
    }

    private static bool HaveValidExtension(IFormFile file) =>
        file != null && AllowedFileExtensions.Contains(Path.GetExtension(file.FileName).ToLowerInvariant());

    private static bool HaveValidImageExtension(IFormFile file) =>
        file != null && AllowedImageExtensions.Contains(Path.GetExtension(file.FileName).ToLowerInvariant());

    private static bool HaveValidFileSize(IFormFile file) =>
        file.Length <= MaxFileSize;
}