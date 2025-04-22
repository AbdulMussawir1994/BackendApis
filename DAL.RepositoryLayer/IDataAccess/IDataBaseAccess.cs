using DAL.DatabaseLayer.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace DAL.RepositoryLayer.IDataAccess
{
    public interface IDataBaseAccess
    {
        ValueTask<bool> SaveRefreshTokenAsync(AppUser user, string refreshToken);
        Task<bool> FindEmailAsync(string email, CancellationToken cancellationToken);
        Task<bool> FindCNICAsync(string cnic, CancellationToken cancellationToken);
        Task<bool> FindMobileAsync(string mobile, CancellationToken cancellationToken);
        Task<bool> InActivateUserAsync(AppUser user, CancellationToken cancellationToken);

        Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
    }
}
