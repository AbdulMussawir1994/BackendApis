using DAL.DatabaseLayer.DbInterceptor;
using DAL.DatabaseLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.DatabaseLayer.DataContext
{
    public class InterceptorDbContext : DbContext
    {
        private readonly AuditableEntitySaveChangesInterceptor _auditableInterceptor;
        private readonly SqlCommandInterceptor _sqlCommandInterceptor;
        private readonly TransactionInterceptor _transactionInterceptor;
        private readonly ConnectionInterceptor _connectionInterceptor;
        private readonly DataReaderInterceptor _dataReaderInterceptor;

        public InterceptorDbContext(
            DbContextOptions<InterceptorDbContext> options,
            AuditableEntitySaveChangesInterceptor auditableInterceptor,
            SqlCommandInterceptor sqlCommandInterceptor,
            TransactionInterceptor transactionInterceptor,
            ConnectionInterceptor connectionInterceptor,
            DataReaderInterceptor dataReaderInterceptor)
            : base(options)
        {
            _auditableInterceptor = auditableInterceptor;
            _sqlCommandInterceptor = sqlCommandInterceptor;
            _transactionInterceptor = transactionInterceptor;
            _connectionInterceptor = connectionInterceptor;
            _dataReaderInterceptor = dataReaderInterceptor;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.AddInterceptors(
                _auditableInterceptor,
                _sqlCommandInterceptor,
                _transactionInterceptor,
                _connectionInterceptor,
                _dataReaderInterceptor
            );
        }

        public DbSet<Employee> Employees { get; set; }
    }
}