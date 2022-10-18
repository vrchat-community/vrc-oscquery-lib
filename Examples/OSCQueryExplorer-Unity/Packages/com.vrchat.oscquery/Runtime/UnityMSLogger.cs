using System;
using Microsoft.Extensions.Logging;

namespace VRC.OSCQuery
{
    public class UnityMSLogger : ILogger<OSCQueryService>
    {
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            switch (logLevel)
            {
                case LogLevel.Information:
                    UnityEngine.Debug.Log(state);
                    break;
                case LogLevel.Critical:
                case LogLevel.Error:
                    UnityEngine.Debug.LogError(state);
                    break;
                case LogLevel.Warning:
                    UnityEngine.Debug.LogWarning(state);
                    break;
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }
    }
}