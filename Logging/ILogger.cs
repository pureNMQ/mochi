namespace Mochi.Logging
{
    public interface ILogger
    {
        bool Enabled { get; set; }
        LogLevel MinLevel { get; set; }

        void Log(string message, LogLevel level = LogLevel.Info);

        void LogFormat(string format, LogLevel level, params object[] args);

        void Debug(string message);

        void DebugFormat(string format, params object[] args);

        void Info(string message);

        void InfoFormat(string format, params object[] args);

        void Warn(string message);

        void WarnFormat(string format, params object[] args);

        void Error(string message);

        void ErrorFormat(string format, params object[] args);
    }
}
