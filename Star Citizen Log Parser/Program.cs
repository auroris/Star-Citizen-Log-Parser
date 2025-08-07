using System.Runtime.InteropServices;

namespace Star_Citizen_Log_Parser
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ConsoleHelper.ShowConsole();

            using var reader = new LogReader.LogReader("Game.log");
            reader.ReadingIdle += () => Reader_ReadingIdle(reader);

            //ApplicationConfiguration.Initialize();
            //Application.Run(new Form1());

            Console.WriteLine("Application exited. Press any key...");
            Console.ReadKey();
        }

        private static void Reader_ReadingIdle(LogReader.LogReader reader)
        {
            Console.WriteLine($"Total entries: {reader.LogEntries.Count}");

            var byLabel = reader.LogEntries
                .Where(e => e.Template?.Label != null)
                .GroupBy(e => e.Template!.Label)
                .OrderByDescending(g => g.Count());

            foreach (var group in byLabel)
            {
                Console.WriteLine($"  {group.Key}: {group.Count()}");
            }
        }
    }

    public static class ConsoleHelper
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        public static void ShowConsole()
        {
            if (GetConsoleWindow() == IntPtr.Zero)
            {
                AllocConsole();
            }
        }

        public static void HideConsole()
        {
            if (GetConsoleWindow() != IntPtr.Zero)
            {
                FreeConsole();
            }
        }
    }
}