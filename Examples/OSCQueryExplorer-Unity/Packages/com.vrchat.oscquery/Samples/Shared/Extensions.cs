using System;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace VRC.OSCQuery.Samples.Shared
{
    public static class Extensions
    {
        public static string UpperCaseFirstChar(this string text) {
            return Regex.Replace(text, "^[a-z]", m => m.Value.ToUpper());
        }

    }

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