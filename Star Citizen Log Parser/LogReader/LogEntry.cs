namespace Star_Citizen_Log_Parser.LogReader
{
    public class LogEntry
    {
        public DateTime Timestamp { get; }
        public List<string> Tags { get; set; } = [];
        public LogTemplate Template { get; }
        public Dictionary<string, string> Fields { get; set; } = [];
        public List<string> Lines { get; } = [];

        public LogEntry(DateTime timestamp, LogTemplate template)
        {
            Timestamp = timestamp;
            Template = template;
        }
    }
}
