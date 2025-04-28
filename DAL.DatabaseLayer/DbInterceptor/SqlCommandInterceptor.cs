using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Data.Common;

namespace DAL.DatabaseLayer.DbInterceptor;

public sealed class SqlCommandInterceptor : DbCommandInterceptor
{
    private readonly ILogger<SqlCommandInterceptor> _logger;

    public SqlCommandInterceptor(ILogger<SqlCommandInterceptor> logger)
    {
        _logger = logger;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("SQL Executed: {CommandText}", command.CommandText);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }
}
