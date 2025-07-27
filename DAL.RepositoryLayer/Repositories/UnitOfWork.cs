using DAL.RepositoryLayer.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DAL.RepositoryLayer.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly DbContext _context;
    private readonly Dictionary<Type, object> _repositories = new();

    public UnitOfWork(DbContext context)
    {
        _context = context;
    }

    public IRepository<T> Repository<T>() where T : class
    {
        var type = typeof(T);
        if (!_repositories.ContainsKey(type))
        {
            var repoInstance = new GenericRepository<T>(_context);
            _repositories[type] = repoInstance;
        }

        return (IRepository<T>)_repositories[type];
    }

    public Task<int> SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
