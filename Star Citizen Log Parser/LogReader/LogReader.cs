using Star_Citizen_Log_Parser.Parsing;

namespace Star_Citizen_Log_Parser.LogReader
{
    /// <summary>
    /// Monitors and parses a Star Citizen log file in real time.
    /// </summary>
    public class LogReader : IDisposable
    {
        private readonly FileTailReader tailReader;
        private readonly LogParser parser;
        private bool hasRaisedIdle = true;
        private CancellationTokenSource idleTokenSource = new();
        private DateTime lastLineReceived = DateTime.UtcNow;

        public List<LogEntry> LogEntries { get; } = [];

        /// <summary>
        /// Raised when a log entry has been parsed and completed.
        /// </summary>
        public event Action<LogEntry>? LogEntryRead;

        /// <summary>
        /// Raised when reading has temporarily finished (no more lines to read and parser is idle).
        /// </summary>
        public event Action? ReadingIdle;

        /// <summary>
        /// Initializes a new LogReader that reads from the given log file and parses entries in real time.
        /// </summary>
        /// <param name="logFilePath">Path to the Star Citizen game.log file.</param>
        public LogReader(string logFilePath)
        {
            parser = new LogParser(TemplateLoader.LoadFromYaml("templates.yaml"));
            parser.EntryCompleted += entry =>
            {
                LogEntries.Add(entry);
                LogEntryRead?.Invoke(entry);
                DebounceIdleEvent();
            };

            tailReader = new FileTailReader(logFilePath);
            tailReader.LineRead += line =>
            {
                lastLineReceived = DateTime.UtcNow;
                hasRaisedIdle = false;
                parser.HandleLine(line);
                DebounceIdleEvent();
            };
            tailReader.Start();
        }

        /// <summary>
        /// Stops tailing the file and detaches from events.
        /// </summary>
        public void Dispose()
        {
            tailReader.LineRead -= parser.HandleLine;
            tailReader.Stop();
        }

        /// <summary>
        /// Starts or resets a debounce timer. If no new lines are received within the delay, ReadingIdle is raised.
        /// </summary>
        private void DebounceIdleEvent()
        {
            idleTokenSource.Cancel();
            idleTokenSource = new CancellationTokenSource();
            var token = idleTokenSource.Token;

            Task.Delay(200, token).ContinueWith(t =>
            {
                if (!t.IsCanceled && !hasRaisedIdle)
                {
                    hasRaisedIdle = true;
                    ReadingIdle?.Invoke();
                }
            }, token);
        }
    }
}