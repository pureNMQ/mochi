using System;

namespace Mochi.Logging
{
    public interface ILogWriter : IDisposable
    {
        LogLevel MinLevel { get; set; }
        void WriteLog(LogEntry entry);
        void Flush();
    }
}
