using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Portfolio.Api.Tests.Fakes;

/// <summary>
/// Captures log entries so a test can assert that a failure was reported at the level it
/// deserves. A handler that returns the right status but says nothing is half a handler.
/// </summary>
public sealed class RecordingLogger<T> : ILogger<T>
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
