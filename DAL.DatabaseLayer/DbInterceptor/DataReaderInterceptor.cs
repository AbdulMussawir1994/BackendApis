using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Data.Common;

namespace DAL.DatabaseLayer.DbInterceptor;

public sealed class DataReaderInterceptor : DbCommandInterceptor
{
    private readonly ILogger<DataReaderInterceptor> _logger;

    public DataReaderInterceptor(ILogger<DataReaderInterceptor> logger)
    {
        _logger = logger;
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("DataReader Executed: {CommandText}", command.CommandText);
        return new ValueTask<DbDataReader>(result);
    }
}
