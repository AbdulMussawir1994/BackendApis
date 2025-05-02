using Cryptography.Utilities;
using DAL.DatabaseLayer.DataContext;
using DAL.DatabaseLayer.Models;
using DAL.DatabaseLayer.ViewModels.AuthModels;
using DAL.RepositoryLayer.IDataAccess;
using DAL.RepositoryLayer.Repositories;
using DAL.ServiceLayer.Utilities;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace TestProject.RepositoriesTest;

public class AuthRepositoryTests : IDisposable
{
    private readonly WebContextDb _dbContext;
    private readonly AuthRepository _authRepository;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<AuthRepository>> _mockLogger;
    private readonly Mock<IDataBaseAccess> _mockDatabaseAccess;
    private readonly Mock<IValidator<RegisterViewModel>> _mockValidator;
    private readonly UserManager<AppUser> _userManager;
    private readonly ConfigHandler _configHandler;
    private readonly AesGcmEncryption _aesGcmEncryption;

    public AuthRepositoryTests()
    {
        // ✅ In-Memory SQLite Database
        var options = new DbContextOptionsBuilder<WebContextDb>()
            .UseSqlite("Filename=:memory:")
            .Options;

        _dbContext = new WebContextDb(options);
        _dbContext.Database.OpenConnection();
        _dbContext.Database.EnsureCreated();

        // ✅ Mock dependencies
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<AuthRepository>>();
        _mockDatabaseAccess = new Mock<IDataBaseAccess>();
        _mockValidator = new Mock<IValidator<RegisterViewModel>>();

        // ✅ Set up UserManager
        var userStore = new UserStore<AppUser>(_dbContext);
        _userManager = new UserManager<AppUser>(
            userStore,
            null,
            new PasswordHasher<AppUser>(),
            null,
            null,
            null,
            null,
            null,
            new Mock<ILogger<UserManager<AppUser>>>().Object
        );

        // ✅ Set up basic config values for JWT (important to avoid null exception)
        _mockConfiguration.Setup(c => c["JWTKey:Secret"]).Returns("SuperSecretKey123456");
        _mockConfiguration.Setup(c => c["JWTKey:ValidIssuer"]).Returns("TestIssuer");
        _mockConfiguration.Setup(c => c["JWTKey:ValidAudience"]).Returns("TestAudience");
        _mockConfiguration.Setup(c => c["JWTKey:TokenExpiryTimeInMinutes"]).Returns("60");

        // ✅ Setup AesGcmEncryption (real class, not mock)
        _aesGcmEncryption = new AesGcmEncryption(_mockConfiguration.Object);

        // ✅ Setup ConfigHandler (basic mock config)
        var dummyHttpClient = new HttpClient();
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _configHandler = new ConfigHandler(_mockConfiguration.Object, dummyHttpClient, httpClientFactoryMock.Object);

        // ✅ Now inject all dependencies properly
        _authRepository = new AuthRepository(
            _mockLogger.Object,
            _userManager,
            _aesGcmEncryption,
            _mockConfiguration.Object,
            _mockDatabaseAccess.Object,
            _mockValidator.Object,
            _configHandler
        );
    }

    [Fact]
    public void AuthRepository_ShouldBeCreated()
    {
        _authRepository.Should().NotBeNull();
    }

    public void Dispose()
    {
        _dbContext?.Database?.CloseConnection();
        _dbContext?.Dispose();
    }

    #region **LoginUser Tests**

    [Fact]
    public async Task LoginUser_Should_Return_Success_When_Valid_Credentials()
    {
        // Arrange
        var user = new AppUser
        {
            Id = "5c3f7894-6c66-4d9f-ac63-ab6dbcd59db0",
            CNIC = "4210148778829",
            EmailConfirmed = true,
            PhoneNumberConfirmed = true
        };
        user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, "123456");

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        var loginModel = new LoginViewModel { CNIC = "4210148778829", Password = "123456" };
        var cancellationToken = new CancellationToken();

        // Act
        var response = await _authRepository.LoginUser(loginModel, cancellationToken);

        // Assert
        response.Status.IsSuccess.Should().BeTrue();
        response.Status.StatusMessage.Should().Be("Login Successful");
        response.Content.Id.Should().Be("5c3f7894-6c66-4d9f-ac63-ab6dbcd59db0");
        response.Content.AccessToken.Should().Be("jwtToken");
    }

    [Fact]
    public async Task LoginUser_Should_Return_Error_When_User_Is_Not_Verified()
    {
        // Arrange
        var user = new AppUser
        {
            Id = "5c3f7894-6c66-4d9f-ac63-ab6dbcd59db0",
            CNIC = "4210148778829",
            EmailConfirmed = true,
            PhoneNumberConfirmed = true
        };

        user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, "123456");

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        var loginModel = new LoginViewModel { CNIC = "4210148778829", Password = "123456" };
        var cancellationToken = new CancellationToken();

        // Act
        var response = await _authRepository.LoginUser(loginModel, cancellationToken);

        // Assert
        response.Status.IsSuccess.Should().BeFalse();
        response.Status.StatusMessage.Should().Be("Verify your password. and");
    }

    [Fact]
    public async Task RegisterUser_Should_Return_Error_When_CNIC_Already_Exists()
    {
        // Arrange
        var registerModel = new RegisterViewModel
        {
            CNIC = "123456789", // CNIC to simulate as already existing
            Email = "new@example.com",
            MobileNo = "1234567890",
            Username = "newuser"
        };

        var cancellationToken = new CancellationToken();

        // 🧠 Mock CNIC check to simulate "already exists"
        _mockDatabaseAccess.Setup(x => x.FindCNICAsync(registerModel.CNIC, cancellationToken))
                           .ReturnsAsync(true);

        // ✅ You should also mock the other methods to avoid hitting them
        _mockDatabaseAccess.Setup(x => x.FindEmailAsync(It.IsAny<string>(), cancellationToken))
                           .ReturnsAsync(false);

        _mockDatabaseAccess.Setup(x => x.FindMobileAsync(It.IsAny<string>(), cancellationToken))
                           .ReturnsAsync(false);

        // Act
        var response = await _authRepository.RegisterUser(registerModel, cancellationToken);

        // Assert
        response.Status.IsSuccess.Should().BeFalse();
        response.Status.StatusMessage.Should().Be("CNIC number is already registered.");
    }

    //[Fact]
    //public async Task UpdatePrivacyPolicy_Should_Return_Success_When_User_Agrees()
    //{
    //    // Arrange
    //    var user = new AppUser
    //    {
    //        Id = "user_id",
    //        CNIC = "4210148778829",
    //    };

    //    await _dbContext.Users.AddAsync(user);
    //    await _dbContext.SaveChangesAsync();

    //    // Act
    //    var response = await _authRepository.UpdatePrivacyPolicyAsync("user_id");

    //    // Assert
    //    response.Status.IsSuccess.Should().BeTrue();
    //    response.Status.StatusMessage.Should().Be("Privacy policy agreed successfully.");
    //}

    [Fact]
    public async Task SavePassword_Should_Return_Error_When_Invalid_Password_Format()
    {
        // Arrange
        var user = new AppUser
        {
            Id = "user_id",
            CNIC = "4210148778829"
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        var passwordModel = new PasswordViewModel { Id = "user_id", Password = "abc" };

        // Act
        var response = await _authRepository.SavePasswordAsync(passwordModel);

        // Assert
        response.Status.IsSuccess.Should().BeFalse();
        response.Status.StatusMessage.Should().Be("Password must be exactly 6 digits. Password must contain only digits.");
    }


    //[Fact]
    //public async Task UpdatePrivacyPolicy_Should_Return_Error_When_User_Not_Found()
    //{
    //    // Act
    //    var response = await _authRepository.UpdatePrivacyPolicyAsync("invalid_user");

    //    // Assert
    //    response.Status.IsSuccess.Should().BeFalse();
    //    response.Status.StatusMessage.Should().Be("User not found.");
    //}



    [Fact]
    public async Task LoginUser_Should_Return_Error_When_CNIC_Invalid()
    {
        // Arrange
        var loginModel = new LoginViewModel { CNIC = "invalid_cnic", Password = "123456" };
        var cancellationToken = new CancellationToken();

        // Act
        var response = await _authRepository.LoginUser(loginModel, cancellationToken);

        // Assert
        response.Status.IsSuccess.Should().BeFalse();
        response.Status.StatusMessage.Should().Be("CNIC  is Invalid.");
    }

    [Fact]
    public async Task LoginUser_Should_Return_Error_When_Password_Incorrect()
    {
        // Arrange
        var user = new AppUser
        {
            Id = "5c3f7894-6c66-4d9f-ac63-ab6dbcd59db0",
            CNIC = "4210148778829",
        };
        user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, "123456");

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        var loginModel = new LoginViewModel { CNIC = "4210148778829", Password = "wrong_password" };
        var cancellationToken = new CancellationToken();

        // Act
        var response = await _authRepository.LoginUser(loginModel, cancellationToken);

        // Assert
        response.Status.IsSuccess.Should().BeFalse();
        response.Status.StatusMessage.Should().Be("Password is Invalid.");
    }

    #endregion

    #region **RegisterUser Tests**

    [Fact]
    public async Task RegisterUser_Should_Return_Success_When_Valid()
    {
        // Arrange
        var registerModel = new RegisterViewModel
        {
            CNIC = "123456789",
            Email = "user@example.com",
            MobileNo = "1234567890",
            Username = "user"
        };
        var cancellationToken = new CancellationToken();

        // Act
        var response = await _authRepository.RegisterUser(registerModel, cancellationToken);

        // Assert
        response.Status.IsSuccess.Should().BeTrue();
        response.Status.StatusMessage.Should().Be("Registration successful. Please verify your Email & Mobile number.");
    }

    #endregion

    #region **VerifyOtp Tests**

    //[Fact]
    //public async Task VerifyOtp_Should_Return_Error_When_Invalid_Otp()
    //{
    //    // Arrange
    //    var otpModel = new OtpViewModel { UserId = "user_id", OtpCode = "0000", OtpType = "Email" };

    //    // ✅ Fix: Provide a valid CNIC value
    //    var user = new AppUser
    //    {
    //        Id = "user_id",
    //        CNIC = "4210148778829" // ✅ Ensures CNIC is not NULL to avoid SQLite constraint error
    //    };

    //    await _dbContext.Users.AddAsync(user);
    //    await _dbContext.SaveChangesAsync();

    //    _mockOtpRepository.Setup(x => x.GetOtpAsync(
    //            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
    //        .ReturnsAsync(new OtpResponseModel { Status = OtpStatus.NotFound });

    //    // Act
    //    var response = await _authRepository.VerifyOtpAsync(otpModel);

    //    // Assert
    //    response.Status.IsSuccess.Should().BeFalse();
    //    response.Status.StatusMessage.Should().Be("Otp not found.");
    //}

    #endregion

    #region **ChangePassword Tests**

    //[Fact]
    //public async Task ChangePassword_Should_Return_Error_When_User_Not_Found()
    //{
    //    // Arrange
    //    var model = new ChangePasswordViewModel
    //    {
    //        CNIC = "123456",
    //        MobileNo = "9876543210",
    //        Email = "test@example.com",
    //        Password = "newPass123",
    //        VerifyPassword = "newPass123"
    //    };

    //    // Act
    //    var response = await _authRepository.ChangePasswordAsync(model);

    //    // Assert
    //    response.Status.IsSuccess.Should().BeFalse();
    //    response.Status.StatusMessage.Should().Be("Please provide correct IC Number, Mobile No, and Email.");
    //}

    //[Fact]
    //public async Task ChangePassword_Should_Return_Success_When_Valid()
    //{
    //    // Arrange
    //    var user = new AppUser
    //    {
    //        Id = "valid_user",
    //        CNIC = "123456",
    //        PhoneNumber = "9876543210",
    //        Email = "test@example.com"
    //    };

    //    await _dbContext.Users.AddAsync(user);
    //    await _dbContext.SaveChangesAsync();

    //    var model = new ChangePasswordViewModel
    //    {
    //        CNIC = "123456",
    //        MobileNo = "9876543210",
    //        Email = "test@example.com",
    //        Password = "newPass123",
    //        VerifyPassword = "newPass123"
    //    };

    //    // Act
    //    var response = await _authRepository.ChangePasswordAsync(model);

    //    // Assert
    //    response.Status.IsSuccess.Should().BeTrue();
    //    response.Status.StatusMessage.Should().Be("Password changed successfully. Please verify your Email & Mobile number.");
    //}


    //[Fact]
    //public async Task VerifyPassword_Should_Return_Error_When_Password_Is_Wrong()
    //{
    //    // Arrange
    //    var user = new AppUser
    //    {
    //        Id = "user_id",
    //        CNIC = "4210148778829"
    //    };

    //    user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, "correct_password");

    //    await _dbContext.Users.AddAsync(user);
    //    await _dbContext.SaveChangesAsync();

    //    var passwordModel = new PasswordViewModel { Id = "user_id", Password = "wrong_password" };

    //    // Act
    //    var response = await _authRepository.VerifyPasswordAsync(passwordModel);

    //    // Assert
    //    response.Status.IsSuccess.Should().BeFalse();
    //    response.Status.StatusMessage.Should().Be("Unmatched Pin.");
    //}



    #endregion
}
