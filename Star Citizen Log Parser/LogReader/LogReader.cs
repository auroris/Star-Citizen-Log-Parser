namespace Star_Citizen_Log_Parser.LogReader
{
    public class LogReader : IDisposable
    {
        private readonly LogParser parser;
        private readonly FileTailReader? tailReader; // optional in test
        private bool hasRaisedIdle = true;
        private CancellationTokenSource idleTokenSource = new();
        private DateTime lastLineReceived = DateTime.UtcNow;

        public List<LogEntry> LogEntries { get; } = [];

        public event Action<LogEntry>? LogEntryRead;
        public event Action? ReadingIdle;

        public LogReader(string logFilePath)
        {
            parser = new LogParser(TemplateLoader.LoadFromYaml("templates.yaml"));
            InitializeParserEvents();

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
        /// For unit testing: accepts a parser and feeds lines manually.
        /// </summary>
        internal LogReader(LogParser parser)
        {
            this.parser = parser;
            InitializeParserEvents();
        }

        private void InitializeParserEvents()
        {
            parser.EntryCompleted += entry =>
            {
                LogEntries.Add(entry);
                LogEntryRead?.Invoke(entry);
                DebounceIdleEvent();
            };
        }

        public void FeedTestLine(string line)
        {
            lastLineReceived = DateTime.UtcNow;
            hasRaisedIdle = false;
            parser.HandleLine(line);
            DebounceIdleEvent();
        }

        public void Dispose()
        {
            if (tailReader != null)
            {
                tailReader.LineRead -= parser.HandleLine;
                tailReader.Stop();
            }
        }

        private void DebounceIdleEvent()
        {
            idleTokenSource.Cancel();
            idleTokenSource = new CancellationTokenSource();
            var token = idleTokenSource.Token;

            Task.Delay(500, token).ContinueWith(t =>
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