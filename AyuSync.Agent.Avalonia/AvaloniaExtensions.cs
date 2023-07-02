#region

using Avalonia.Controls;
using Avalonia.Logging;
using Serilog;

#endregion

namespace AyuSync.Client.App;

public static class AvaloniaExtensions
{
    public static TAppBuilder UseSerilog<TAppBuilder>(this TAppBuilder builder)
        where TAppBuilder : AppBuilderBase<TAppBuilder>, new()
    {
        Log.Logger = new LoggerConfiguration()
                     .MinimumLevel.Debug()
                     .Enrich.FromLogContext()
                     .WriteTo.Console()
                     .CreateLogger();

        Logger.Sink = new SerilogSink();
        return builder;
    }
}

public class SerilogSink : ILogSink
{
    /// <inheritdoc />
    public bool IsEnabled(LogEventLevel level, string area)
    {
        return level >= LogEventLevel.Information;
    }

    /// <inheritdoc />
    public void Log(LogEventLevel level, string area, object? source, string messageTemplate)
    {
        Serilog.Log.Write((Serilog.Events.LogEventLevel)level, messageTemplate);
    }

    /// <inheritdoc />
    public void Log<T0>(LogEventLevel level, string area, object? source, string messageTemplate, T0 propertyValue0)
    {
        Serilog.Log.Write((Serilog.Events.LogEventLevel)level, messageTemplate, propertyValue0);
    }

    /// <inheritdoc />
    public void Log<T0, T1>(LogEventLevel level, string area, object? source, string messageTemplate, T0 propertyValue0,
                            T1 propertyValue1)
    {
        Serilog.Log.Write((Serilog.Events.LogEventLevel)level, messageTemplate, propertyValue0, propertyValue1);
    }

    /// <inheritdoc />
    public void Log<T0, T1, T2>(LogEventLevel level, string area, object? source, string messageTemplate,
                                T0 propertyValue0,
                                T1 propertyValue1, T2 propertyValue2)
    {
        Serilog.Log.Write((Serilog.Events.LogEventLevel)level, messageTemplate, propertyValue0, propertyValue1,
                          propertyValue2);
    }

    /// <inheritdoc />
    public void Log(LogEventLevel level, string area, object? source, string messageTemplate,
                    params object?[] propertyValues)
    {
        Serilog.Log.Write((Serilog.Events.LogEventLevel)level, messageTemplate, propertyValues);
    }
}
