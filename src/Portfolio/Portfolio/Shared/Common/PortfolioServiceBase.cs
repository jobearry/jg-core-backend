using Portfolio.Application.Common;
using Portfolio.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Portfolio.Application.Services;

/// <summary>
/// Shared behaviour for the portfolio read services: it owns the repository, turns a
/// failed call into a typed failure instead of letting the exception escape, and logs
/// the failure with the detail that must not reach the client.
/// </summary>
/// <typeparam name="T">The domain entity the service reads.</typeparam>
/// <typeparam name="TRepository">The repository port the service reads through.</typeparam>
public abstract class PortfolioServiceBase<T, TRepository>
    where T : class
    where TRepository : IRepository<T>
{
    protected TRepository Repository { get; }

    private readonly ILogger _logger;

    protected PortfolioServiceBase(TRepository repository, ILogger logger)
    {
        Repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Runs <paramref name="operation"/> and wraps its outcome.
    /// </summary>
    /// <remarks>
    /// The operation receives the cancellation token rather than capturing it, so a
    /// caller cannot pass a token to the service and have it silently dropped before it
    /// reaches the database.
    /// </remarks>
    protected async Task<Result<TValue>> ExecuteAsync<TValue>(
        Func<CancellationToken, Task<TValue>> operation,
        string operationDescription,
        CancellationToken cancellationToken)
    {
        try
        {
            return Result<TValue>.Success(await operation(cancellationToken));
        }
        catch (OperationCanceledException)
        {
            // A cancelled request is not a failure to report — it is the caller giving
            // up. Turning it into a Result would hide that from the framework and make
            // it impossible to tell "we failed" from "nobody is listening".
            throw;
        }
        catch (Exception ex)
        {
            var error = ResultErrors.From(ex, operationDescription);
            LogFailure(ex, error, operationDescription);
            return Result<TValue>.Failure(error);
        }
    }

    /// <summary>
    /// Logs at the level the failure deserves. A dependency outage is a warning — it is
    /// expected to happen and is already visible to the caller as a 503. Only a genuinely
    /// unexpected exception is an error, because only that needs someone to act.
    /// </summary>
    private void LogFailure(Exception exception, Error error, string operationDescription)
    {
        if (error.Kind == ErrorKind.Unexpected)
        {
            _logger.LogError(
                exception,
                "Unexpected failure while trying to {Operation} ({ErrorCode}).",
                operationDescription,
                error.Code);
        }
        else
        {
            _logger.LogWarning(
                exception,
                "Dependency failure while trying to {Operation} ({ErrorCode}).",
                operationDescription,
                error.Code);
        }
    }
}
