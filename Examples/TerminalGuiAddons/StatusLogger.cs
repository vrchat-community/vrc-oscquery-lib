using System;
using Terminal.Gui;
using Common.Logging;
using Common.Logging.Simple;

namespace VRC.OSCQuery.Examples
{
     public class StatusLogger : AbstractSimpleLogger
     {
            private Window _logsView;
            private StatusItem _item;
            private TextView _textView;

            public StatusLogger(string logName, LogLevel logLevel, bool showlevel, bool showDateTime, bool showLogName, string dateTimeFormat) : base(logName, logLevel, showlevel, showDateTime, showLogName, dateTimeFormat)
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
            
            private void ShowLogs()
            {
                _logsView.Visible = !_logsView.Visible;
                if (_logsView.Visible)
                {
                    _logsView.SetFocus();
                }
            }
            
            protected override void WriteInternal(LogLevel level, object message, Exception exception)
            {
                // Log to status bar
                _item.Title = message.ToString();
                
                // Log to text view
                _textView.Text += $"{message}{Environment.NewLine}";
            }
     }
}