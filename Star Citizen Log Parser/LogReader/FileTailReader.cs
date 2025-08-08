using System.Text;

namespace Star_Citizen_Log_Parser.LogReader
{
    internal class FileTailReader
    {
        public event Action<string>? LineRead;

        private readonly string filePath;
        private CancellationTokenSource? cts;

        public FileTailReader(string filePath)
        {
            this.filePath = filePath;
        }

        public void Start()
        {
            cts = new CancellationTokenSource();
            Task.Run(() => ReadLoop(cts.Token), cts.Token);
        }

        public void Stop()
        {
            cts?.Cancel();
        }

        private async Task ReadLoop(CancellationToken token)
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fs, Encoding.UTF8);

            // Process existing content
            while (!reader.EndOfStream && !token.IsCancellationRequested)
            {
                string? line = await reader.ReadLineAsync();
                if (line != null) LineRead?.Invoke(line);
            }

            // Tail new content
            while (!token.IsCancellationRequested)
            {
                string? line = await reader.ReadLineAsync();
                if (line != null)
                {
                    LineRead?.Invoke(line);
                }
                else
                {
                    await Task.Delay(100, token);
                }
            }
        }

        public static bool IsFileInUse(string path)
        {
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
                return false;
            }
            catch (IOException)
            {
                return true;
            }
        }
    }
}
