using Common.Logging;
using Common.Logging.Configuration;
using Common.Logging.Simple;

namespace VRC.OSCQuery.Examples
{
    public class StatusLoggerFactoryAdapter : AbstractSimpleLoggerFactoryAdapter
    {
        public StatusLoggerFactoryAdapter()
            : base(null)
        { }
        
        
        public StatusLoggerFactoryAdapter(NameValueCollection properties)
            : base(properties)
        { }

        public StatusLoggerFactoryAdapter(LogLevel level, bool showDateTime, bool showLogName, bool showLevel, string dateTimeFormat)
            : base(level, showDateTime, showLogName, showLevel, dateTimeFormat)
        { }

        protected override ILog CreateLogger(string name, LogLevel level, bool showLevel, bool showDateTime, bool showLogName, string dateTimeFormat)
        {
            ILog log = new StatusLogger(name, level, showLevel, showDateTime, showLogName, dateTimeFormat);
            return log;
        }
    }
}