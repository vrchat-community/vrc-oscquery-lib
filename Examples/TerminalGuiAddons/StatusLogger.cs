using System;
using Microsoft.Extensions.Logging;
using Terminal.Gui;

namespace VRC.OSCQuery.Examples
{
     public class StatusLogger<T> : ILogger<T>
        {
            private Window _logsView;
            private StatusItem _item;
            private TextView _textView;
            
            public StatusLogger()
            {
                // Add window with text view
                _logsView = new Window("Logs")
                {
                    Visible = false
                };
                _textView = new TextView()
                {
                    Width = Dim.Fill(),
                    Height = Dim.Fill()
                };
                _logsView.Add(_textView);
                
                // Add to app
                Application.Top.Add(_logsView);
                
                _item = new StatusItem(Key.AltMask | Key.L, "Logs will show here, press to show/hide", ShowLogs);
                var bar = new StatusBar(new StatusItem[] { _item });
                
                // Add to app
                Application.Top.Add(bar);
            }
            
            /// <summary>
            /// Logs to Status Bar and TextView
            /// </summary>
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                // Log to status bar
                _item.Title = $"{state}";
                
                // Log to text view
                _textView.Text += $"{state}{Environment.NewLine}";
            }

            /// <summary>
            /// Show all logs for now
            /// </summary>
            /// <param name="logLevel"></param>
            /// <returns></returns>
            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            /// <summary>
            /// Not implemented yet
            /// </summary>
            /// <param name="state"></param>
            /// <typeparam name="TState"></typeparam>
            /// <returns></returns>
            public IDisposable BeginScope<TState>(TState state)
            {
                return null;
            }
            
            private void ShowLogs()
            {
                _logsView.Visible = !_logsView.Visible;
                if (_logsView.Visible)
                {
                    _logsView.SetFocus();
                }
            }
        }
}