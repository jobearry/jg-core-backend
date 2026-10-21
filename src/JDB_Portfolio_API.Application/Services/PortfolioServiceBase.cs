using JDB_Portfolio_API.Application.Common;
using JDB_Portfolio_API.Domain.Interfaces;
using System;
using System.Threading.Tasks;

namespace JDB_Portfolio_API.Application.Services;

/// <summary>
/// Shared behaviour for the portfolio read services: it owns the repository and it
/// turns a failed repository call into a <see cref="Result{T}"/> instead of letting
/// the exception escape as an unhandled 500.
/// </summary>
/// <typeparam name="T">The domain entity the service reads.</typeparam>
/// <typeparam name="TRepository">The repository port the service reads through.</typeparam>
public abstract class PortfolioServiceBase<T, TRepository>
    where T : class
    where TRepository : IRepository<T>
{
    protected TRepository Repository { get; }

    protected PortfolioServiceBase(TRepository repository)
    {
        Repository = repository;
    }

    /// <summary>
    /// Runs <paramref name="operation"/> and wraps its outcome. A failure is reported
    /// as a failed <see cref="Result{T}"/>; cancellation is never swallowed.
    /// </summary>
    protected async Task<Result<TValue>> ExecuteAsync<TValue>(Func<Task<TValue>> operation, string operationDescription)
    {
        try
        {
            return Result<TValue>.Success(await operation());
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Result<TValue>.Failure($"Failed to {operationDescription}: {ex.Message}");
        }
    }
}
