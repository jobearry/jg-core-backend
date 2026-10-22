using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace JDB_Portfolio_API.Application.Tests.Fakes;

/// <summary>
/// Captures log entries so a test can assert that a failure was actually reported.
/// <see cref="Microsoft.Extensions.Logging.Abstractions.NullLogger{T}"/> proves a service
/// compiles; this proves it says something worth reading when it fails.
/// </summary>
internal sealed class RecordingLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, Exception? Exception, string? Message)> Entries { get; } = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Entries.Add((logLevel, exception, formatter(state, exception)));
    }
}
