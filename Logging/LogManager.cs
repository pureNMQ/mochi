using System.Collections.Generic;

namespace Mochi.Logging
{
    public static class LogManager
    {
        private static Dictionary<string, ILogger> loggers;
        private static List<ILogWriter> writers;

        public static LogLevel MinLevel { get; set; } = LogLevel.Debug;

        static LogManager()
        {
            loggers = new Dictionary<string, ILogger>();
            writers = new List<ILogWriter>();
        }

        public static ILogger GetLogger(string category)
        {
            if (loggers.TryGetValue(category, out var logger))
            {
                return logger;
            }

            logger = new Logger(category, WriteLog);
            loggers[category] = logger;
            return logger;
        }

        public static ILogger GetLogger(string category, LogLevel minLevel)
        {
            if (loggers.TryGetValue(category, out var logger))
            {
                return logger;
            }

            logger = new Logger(category, WriteLog) { MinLevel = minLevel };
            loggers[category] = logger;
            return logger;
        }

        public static void AddWriter(ILogWriter writer)
        {
            if (!writers.Contains(writer))
            {
                writers.Add(writer);
            }
        }

        public static void RemoveWriter(ILogWriter writer)
        {
            if (writers.Contains(writer))
            {
                writers.Remove(writer);
            }
        }

        private static void WriteLog(LogEntry entry)
        {
            if (entry.Level < MinLevel) return;

            foreach (var writer in writers)
            {
                writer.WriteLog(entry);
            }
        }
    }
}
