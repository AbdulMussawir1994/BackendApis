using System.ComponentModel.DataAnnotations;

namespace DAL.DatabaseLayer.ViewModels.AuthModels;

public class RefreshTokenRequest
{
    //[Required(ErrorMessage = "User ID is required.")]
    // [MinLength(1, ErrorMessage = "User ID cannot be empty.")]
    // public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "Refresh token is required.")]
    [MinLength(1, ErrorMessage = "Refresh token cannot be empty.")]
    [DataType(DataType.Text)]
    public string RefreshToken { get; set; } = string.Empty;
}

public class RefreshTokenResponse
{
    public DateTime ExpireTokenTime { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}
