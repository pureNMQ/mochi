using System;

namespace Mochi.Logging
{
    public sealed class Logger : ILogger
    {
        public readonly string Category;
        private readonly Action<LogEntry> Write;

        public bool Enabled { get; set; } = true;
        public LogLevel MinLevel { get; set; } = LogLevel.Debug;

        public Logger(string category, Action<LogEntry> write)
        {
            Category = category;
            this.Write = write;
        }

        public void Log(string message, LogLevel level = LogLevel.Info)
        {
            if (!Enabled) return;
            if (level < MinLevel) return;

            var entry = new LogEntry(Category, message, level);
            Write(entry);
        }

        public void LogFormat(string format, LogLevel level, params object[] args)
        {
            if (!Enabled) return;
            if (level < MinLevel) return;

            var message = string.Format(format, args);

            var entry = new LogEntry(Category, message, level);
            Write(entry);
        }

        public void Debug(string message)
        {
            Log(message, LogLevel.Debug);
        }

        public void DebugFormat(string format, params object[] args)
        {
            LogFormat(format, LogLevel.Debug, args);
        }

        public void Info(string message)
        {
            Log(message, LogLevel.Info);
        }

        public void InfoFormat(string format, params object[] args)
        {
            LogFormat(format, LogLevel.Info, args);
        }

        public void Warn(string message)
        {
            Log(message, LogLevel.Warn);
        }

        public void WarnFormat(string format, params object[] args)
        {
            LogFormat(format, LogLevel.Warn, args);
        }

        public void Error(string message)
        {
            Log(message, LogLevel.Error);
        }

        public void ErrorFormat(string format, params object[] args)
        {
            LogFormat(format, LogLevel.Error, args);
        }
    }
}
