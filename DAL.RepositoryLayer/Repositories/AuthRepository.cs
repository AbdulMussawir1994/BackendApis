using Cryptography.Utilities;
using DAL.DatabaseLayer.DTOs.AuthDto;
using DAL.DatabaseLayer.Models;
using DAL.DatabaseLayer.ViewModels.AuthModels;
using DAL.RepositoryLayer.IDataAccess;
using DAL.RepositoryLayer.IRepositories;
using DAL.ServiceLayer.Models;
using DAL.ServiceLayer.Utilities;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace DAL.RepositoryLayer.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly ConfigHandler _configHandler;
        private readonly ILogger<AuthRepository> _logger;
        private readonly UserManager<AppUser> _userManager;
        private readonly AesGcmEncryption _aesGcmEncryption;
        private readonly IConfiguration _configuration;
        private readonly IDataBaseAccess _dataBaseAccess;
        private readonly IValidator<RegisterViewModel> _validator;

        public AuthRepository(ILogger<AuthRepository> logger, UserManager<AppUser> userManager,
                                                    AesGcmEncryption aesGcmEncryption, IConfiguration configuration,
                                                    IDataBaseAccess dataBaseAccess, IValidator<RegisterViewModel> validator,
                                                    ConfigHandler configHandler)
        {
            _logger = logger;
            _userManager = userManager;
            _aesGcmEncryption = aesGcmEncryption;
            _configuration = configuration;
            _dataBaseAccess = dataBaseAccess;
            _validator = validator;
            _configHandler = configHandler;
        }
        public async Task<MobileResponse<LoginResponseModel>> LoginUser(LoginViewModel model, CancellationToken cancellationToken)
        {
            var response = new MobileResponse<LoginResponseModel>(_configHandler, "user");

            var user = await _userManager.Users.AsNoTracking().FirstOrDefaultAsync(x => x.CNIC == model.CNIC, cancellationToken);

            if (user is null)
                return response.SetError("ERR-1001", "CNIC  is Invalid.");

            if (!await _userManager.CheckPasswordAsync(user, model.Password))
                return response.SetError("ERR-1003", "Password is Invalid.");

            // var jwtToken = GenerateJwtToken(user.Id);
            var jwtToken = GenerateSecureJwtToken(user.Id, "Admin", user.Email);
            var encryptedJwtToken = _aesGcmEncryption.Encrypt(jwtToken);


            var refreshToken = GenerateRefreshJwtToken(user.Id);
            var encryptedRefreshToken = _aesGcmEncryption.Encrypt(refreshToken);

            await SaveRefreshToken(user, refreshToken);

            var expiryMinutes = Convert.ToInt64(_configuration["JWTKey:TokenExpiryTimeInMinutes"]);
            var tokenValidity = DateTime.UtcNow.AddMinutes(expiryMinutes);

            _logger.LogInformation("User {CNIC} authenticated successfully.", model.CNIC);

            return response.SetSuccess("SUCCESS-200", "Login Successful", new LoginResponseModel
            {
                AccessToken = encryptedJwtToken,
                Id = user.Id,
                ExpireTokenTime = tokenValidity,
                RefreshToken = encryptedRefreshToken
            });
        }

        public async Task<MobileResponse<RegisterViewDto>> RegisterUser(RegisterViewModel model, CancellationToken cancellationToken)
        {
            var response = new MobileResponse<RegisterViewDto>(_configHandler, "user");

            if (await _dataBaseAccess.FindEmailAsync(model.Email, cancellationToken))
                return response.SetError("EMAIL_REGISTERED", "Email is already registered.");

            if (await _dataBaseAccess.FindCNICAsync(model.CNIC, cancellationToken))
                return response.SetError("CNIC_REGISTERED", "CNIC number is already registered.");

            if (await _dataBaseAccess.FindMobileAsync(model.MobileNo, cancellationToken))
                return response.SetError("MOBILE_REGISTERED", "Mobile number is already registered.");

            var user = new AppUser
            {
                UserName = model.Username,
                CNIC = model.CNIC,
                PhoneNumber = model.MobileNo,
                Email = model.Email,
                DateCreated = DateTime.UtcNow
            };

            await using var transaction = await _dataBaseAccess.BeginTransactionAsync(cancellationToken);

            try
            {
                var result = await _userManager.CreateAsync(user);
                if (!result.Succeeded)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return response.SetError("REGISTRATION_FAILED", result.Errors.FirstOrDefault()?.Description ?? "Unknown error.");
                }

                await transaction.CommitAsync(cancellationToken);

                return response.SetSuccess("SUCCESS-200", "Register Successful", new RegisterViewDto
                {
                    Id = user.Id,
                    Identity = user.CNIC
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                return response.SetError("REGISTRATION_ERROR", "An error occurred during registration. Please try again.");
            }
        }

        public async ValueTask<MobileResponse<bool>> SavePasswordAsync(PasswordViewModel model)
        {
            var response = new MobileResponse<bool>(_configHandler, "user");

            var (isValid, message) = IsValidPassword(model.Password);
            if (!isValid)
                return response.SetError("ERR-1000", message);

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user is null)
                return response.SetError("ERR-1000", "User not found.");

            var hashedPassword = _userManager.PasswordHasher.HashPassword(user, model.Password);
            user.PasswordHash = hashedPassword;

            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded
                ? response.SetSuccess("Success-200", "Password saved successfully.")
                : response.SetError("ERR-1000", "Failed to save password.");
        }

        public async Task<MobileResponse<RefreshTokenResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
        {
            var response = new MobileResponse<RefreshTokenResponse>(_configHandler, "user");

            string decryptedToken;
            try
            {
                decryptedToken = _aesGcmEncryption.Decrypt(request.RefreshToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to decrypt refresh token.");
                return response.SetError("ERR-1004", "Invalid refresh token format.");
            }

            var user = await _userManager.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (user is null)
                return response.SetError("ERR-1001", "User not found.");

            if (!string.Equals(user.RefreshGuid, decryptedToken, StringComparison.Ordinal))
                return response.SetError("ERR-1005", "Refresh token is invalid or expired.");

            var newAccessToken = GenerateSecureJwtToken(user.Id, "User", user.Email);
            var encryptedAccessToken = _aesGcmEncryption.Encrypt(newAccessToken);

            var newRefreshToken = GenerateRefreshJwtToken(user.Id);
            var encryptedRefreshToken = _aesGcmEncryption.Encrypt(newRefreshToken);

            await SaveRefreshToken(user, newRefreshToken);

            var tokenExpiry = long.TryParse(_configuration["JWTKey:TokenExpiryTimeInMinutes"], out var expiryMinutes)
                ? expiryMinutes
                : 30; // default to 30 mins

            var tokenValidity = DateTime.UtcNow.AddMinutes(tokenExpiry);

            return response.SetSuccess("SUCCESS-200", "Token refreshed successfully.", new RefreshTokenResponse
            {
                AccessToken = encryptedAccessToken,
                RefreshToken = encryptedRefreshToken,
                ExpireTokenTime = tokenValidity
            });
        }

        #region Private Methods

        private async Task<bool> SaveRefreshToken(AppUser user, string refreshToken)
        {
            return await _dataBaseAccess.SaveRefreshTokenAsync(user, refreshToken);
        }

        private static (bool isValid, string message) IsValidPassword(string password)
        {
            var errors = new List<string>();
            if (password.Length != 6) errors.Add("Password must be exactly 6 digits.");
            if (!password.All(char.IsDigit)) errors.Add("Password must contain only digits.");
            return errors.Count == 0 ? (true, "Password is valid.") : (false, string.Join(" ", errors));
        }

        private string GenerateJwtToken(string userId)
        {
            var key = Encoding.UTF8.GetBytes(_configuration["JWTKey:Secret"]);
            var signingCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha512);

            var expiryTime = DateTime.UtcNow.AddSeconds(Convert.ToInt64(_configuration["JWTKey:TokenExpiryTimeInMinutes"]) * 2);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new(JwtRegisteredClaimNames.Nbf, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["JWTKey:ValidIssuer"],
                audience: _configuration["JWTKey:ValidAudience"],
                claims: claims,
                expires: expiryTime,
                signingCredentials: signingCredentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateSecureJwtToken(string userId, string role, string email)
        {
            var utcNow = DateTime.UtcNow;
            var secret = _configuration["JWTKey:Secret"];
            var issuer = _configuration["JWTKey:ValidIssuer"];
            var audience = _configuration["JWTKey:ValidAudience"];
            var expiryMinutes = int.Parse(_configuration["JWTKey:TokenExpiryTimeInMinutes"] ?? "30");

            if (string.IsNullOrEmpty(secret))
                throw new InvalidOperationException("JWT secret is missing.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

            var claims = new List<Claim>
    {
        new(JwtRegisteredClaimNames.Sub, userId),
        new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
        new(ClaimTypes.NameIdentifier, userId),
        new(ClaimTypes.Email, email),
        new(ClaimTypes.Role, role)
    };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = utcNow.AddMinutes(expiryMinutes),
                SigningCredentials = credentials,
                Issuer = issuer,
                Audience = audience
            };

            var handler = new JwtSecurityTokenHandler();
            var token = handler.CreateToken(tokenDescriptor);
            return handler.WriteToken(token);
        }

        private string GenerateRefreshJwtToken(string userId)
        {
            var key = Encoding.UTF8.GetBytes(_configuration["JWTKey:Secret"]);
            var signingCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha512);

            int expiryMinutes = int.Parse(_configuration["JWTKey:RefreshTokenValidityInMinutes"] ?? "60");

            var claims = new[]
            {
                  new Claim(JwtRegisteredClaimNames.Sub, userId),
                   new Claim("type", "refresh"),
                   new Claim(ClaimTypes.Role, "User")
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["JWTKey:ValidIssuer"],
                audience: _configuration["JWTKey:ValidAudience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: signingCredentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string GenerateRefreshToken()
        {
            Span<byte> randomBytes = stackalloc byte[64];
            RandomNumberGenerator.Fill(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        private static DateTime GetPakistanTime()
        {
            var pakistanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Karachi");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, pakistanTimeZone);
        }

        #endregion
    }

}
