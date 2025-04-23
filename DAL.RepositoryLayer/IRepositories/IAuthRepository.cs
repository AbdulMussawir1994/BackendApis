using DAL.DatabaseLayer.DTOs.AuthDto;
using DAL.DatabaseLayer.ViewModels.AuthModels;
using DAL.ServiceLayer.Models;

namespace DAL.RepositoryLayer.IRepositories
{
    public interface IAuthRepository
    {
        Task<MobileResponse<LoginResponseModel>> LoginUser(LoginViewModel model, CancellationToken cancellationToken);
        Task<MobileResponse<RegisterViewDto>> RegisterUser(RegisterViewModel model, CancellationToken cancellationToken);
        ValueTask<MobileResponse<bool>> SavePasswordAsync(PasswordViewModel model);
        Task<MobileResponse<RefreshTokenResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken);
        Task<MobileResponse<bool>> InActivateUserAsync(UserIdViewModel model, CancellationToken cancellationToken);
        Task<MobileResponse<IEnumerable<GetUsersDto>>> GetUsersAsync(CancellationToken cancellationToken);
    }
}
