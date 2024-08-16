using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Workleap.Authentication.ClientCredentialsGrant.Tests;

internal sealed class XunitLoggerProvider(ITestOutputHelper testOutput, string appName) : ILoggerProvider, ILogger
{
    private static readonly Dictionary<LogLevel, string> LogLevelStrings = new Dictionary<LogLevel, string>
    {
        [LogLevel.None] = "none",
        [LogLevel.Trace] = "trce",
        [LogLevel.Debug] = "dbug",
        [LogLevel.Information] = "info",
        [LogLevel.Warning] = "warn",
        [LogLevel.Error] = "fail",
        [LogLevel.Critical] = "crit"
    };

    public ILogger CreateLogger(string categoryName)
    {
        return this;
    }

    [SuppressMessage("ApiDesign", "RS0030:Do not use banned APIs")]
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        testOutput.WriteLine("[{0:HH:mm:ss:ffff} {1} {2}] {3}", DateTime.Now, appName, LogLevelStrings[logLevel], message);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull
    {
        return new NoopDisposable();
    }

    public void Dispose()
    {
    }

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}