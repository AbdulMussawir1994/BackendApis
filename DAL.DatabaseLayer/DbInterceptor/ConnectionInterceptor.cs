using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Data.Common;

namespace DAL.DatabaseLayer.DbInterceptor;

public sealed class ConnectionInterceptor : IDbConnectionInterceptor
{
    private readonly ILogger<ConnectionInterceptor> _logger;

    public ConnectionInterceptor(ILogger<ConnectionInterceptor> logger)
    {
        _logger = logger;
    }

    public Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Database Connection Opened: {DataSource}", connection.DataSource);
        return Task.CompletedTask;
    }

    public Task ConnectionClosedAsync(DbConnection connection, ConnectionEndEventData eventData)
    {
        _logger.LogInformation("Database Connection Closed: {DataSource}", connection.DataSource);
        return Task.CompletedTask;
    }
}
