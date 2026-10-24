// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Identity.Tests/Fakes/RecordingLogger.cs
// Version : 1.0.0
// Latest commit: task/api-handle-cooldown
// Author : Gerrah
// Purpose : A logger that keeps what it was asked to write, so tests can check what was logged.

using Microsoft.Extensions.Logging;

namespace TeamSamsara.Modules.Identity.Tests.Fakes;

public class RecordingLogger<T> : ILogger<T>
{
    #region Fields

    private readonly List<LogEntry> _entries = new();

    #endregion

    #region Properties

    public IReadOnlyList<LogEntry> Entries => _entries;

    #endregion

    #region Public Methods

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        _entries.Add(new LogEntry(logLevel, formatter(state, exception)));
    }

    #endregion
}

public record LogEntry(LogLevel Level, string Message);
