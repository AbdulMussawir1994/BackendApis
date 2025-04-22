using DAL.DatabaseLayer.DataContext;
using DAL.DatabaseLayer.Models;
using DAL.RepositoryLayer.IDataAccess;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DAL.RepositoryLayer.DataAccess
{
    public class DataBaseAccess : IDataBaseAccess
    {
        private readonly WebContextDb _context;
        private readonly UserManager<AppUser> _userManager;

        public DataBaseAccess(WebContextDb context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async ValueTask<bool> SaveRefreshTokenAsync(AppUser user, string refreshToken)
        {
            var User = await _context.Users.FindAsync(user.Id);

            if (User is null)
                return false;

            // Only update if value has changed
            if (User.RefreshGuid != refreshToken)
            {
                User.RefreshGuid = refreshToken;
                User.UpdatedDate = DateTime.UtcNow; // optional audit
            }

            _context.Users.Update(User);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken) =>
            await _context.Database.BeginTransactionAsync(cancellationToken);

        public Task<bool> FindEmailAsync(string email, CancellationToken cancellationToken) =>
            _context.Users.AsNoTracking().AnyAsync(u => u.Email == email, cancellationToken);

        public Task<bool> FindCNICAsync(string cnic, CancellationToken cancellationToken) =>
            _context.Users.AsNoTracking().AnyAsync(u => u.CNIC == cnic, cancellationToken);

        public Task<bool> FindMobileAsync(string mobile, CancellationToken cancellationToken) =>
            _context.Users.AsNoTracking().AnyAsync(u => u.PhoneNumber == mobile, cancellationToken);

        public async Task<bool> InActivateUserAsync(AppUser user, CancellationToken cancellationToken)
        {
            user.IsActive = false; // ❗ Set to false to mark inactive
            user.UpdatedDate = DateTime.UtcNow;

            _context.Entry(user).Property(e => e.IsActive).IsModified = true;
            _context.Entry(user).Property(e => e.UpdatedDate).IsModified = true;

            return await _context.SaveChangesAsync(cancellationToken) > 0;
        }
    }
}
