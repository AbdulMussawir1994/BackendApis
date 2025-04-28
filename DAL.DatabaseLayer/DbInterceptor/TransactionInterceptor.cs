using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Data.Common;

namespace DAL.DatabaseLayer.DbInterceptor;

public sealed class TransactionInterceptor : IDbTransactionInterceptor
{
    private readonly ILogger<TransactionInterceptor> _logger;

    public TransactionInterceptor(ILogger<TransactionInterceptor> logger)
    {
        _logger = logger;
    }

    public Task TransactionStartedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Transaction Started: {TransactionId}", eventData.TransactionId);
        return Task.CompletedTask;
    }

    public Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Transaction Committed: {TransactionId}", eventData.TransactionId);
        return Task.CompletedTask;
    }

    public Task TransactionRolledBackAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Transaction Rolled Back: {TransactionId}", eventData.TransactionId);
        return Task.CompletedTask;
    }
}
