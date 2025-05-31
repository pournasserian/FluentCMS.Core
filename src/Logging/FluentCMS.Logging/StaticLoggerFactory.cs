using Microsoft.Extensions.Logging;

namespace FluentCMS.Logging;

public static class StaticLoggerFactory
{
    private static ILoggerFactory _loggerFactory = default!;
    private static bool _initialized = false;

    public static void Initialize(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ??
            throw new ArgumentNullException(nameof(loggerFactory));
        _initialized = true;
    }

    public static ILogger<T> CreateLogger<T>()
    {
        if (!_initialized)
            throw new InvalidOperationException("LoggerFactory has not been initialized. Call Initialize() first.");

        return _loggerFactory.CreateLogger<T>();
    }

    public static ILogger CreateLogger(Type type)
    {
        if (!_initialized)
            throw new InvalidOperationException("LoggerFactory has not been initialized. Call Initialize() first.");

        return _loggerFactory.CreateLogger(type);
    }

    public static ILogger CreateLogger(string categoryName)
    {
        if (!_initialized)
            throw new InvalidOperationException("LoggerFactory has not been initialized. Call Initialize() first.");

        return _loggerFactory.CreateLogger(categoryName);
    }
}