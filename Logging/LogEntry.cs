namespace Mochi.Logging
{
    public struct LogEntry
    {
        public readonly string Category;
        public readonly string Message;
        public readonly LogLevel Level;

        public LogEntry(string category, string message, LogLevel level)
        {
            Category = category;
            Message = message;
            Level = level;
        }

        public override string ToString()
        {
            return $"[{Level:G}] [{Category}]: {Message}";
        }
    }
}
