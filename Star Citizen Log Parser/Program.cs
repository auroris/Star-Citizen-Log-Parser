using Star_Citizen_Log_Parser.LogReader;
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

            //Console.WriteLine("Application exited. Press any key...");
            Console.ReadKey();
        }

        private static void Reader_ReadingIdle(LogReader.LogReader reader)
        {
            Console.WriteLine($"Total entries: {reader.LogEntries.Count}");

            // Load all templates from YAML
            var allTemplates = TemplateLoader.LoadFromYaml("templates.yaml");

            // Group matched templates by label
            var matchedGroups = reader.LogEntries
                .Where(e => e.Template?.Label != null)
                .GroupBy(e => e.Template!.Label)
                .OrderByDescending(g => g.Count())
                .ToList();

            var matchedLabels = new HashSet<string>(matchedGroups.Select(g => g.Key!).Where(key => key != null));

            Console.WriteLine("\n--- Templates with Matches ---");
            foreach (var group in matchedGroups)
            {
                Console.WriteLine($"  {group.Key}: {group.Count()}");
            }

            // Find templates with zero matches
            var unmatchedTemplates = allTemplates
               .Where(t => t.Label != null && !matchedLabels.Contains(t.Label))
               .OrderBy(t => t.Label)
               .ToList();

            Console.WriteLine("\n--- Templates with ZERO Matches ---");
            foreach (var template in unmatchedTemplates)
            {
                Console.Write($"{template.Label} (id: {template.Id}), ");
            }
        }
    }

    public static class ConsoleHelper
    {
        private const int STD_OUTPUT_HANDLE = -11;
        private const int STD_ERROR_HANDLE = -12;
        private const int STD_INPUT_HANDLE = -10;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetStdHandle(int nStdHandle, IntPtr handle);

        public static void ShowConsole()
        {
            if (GetConsoleWindow() == IntPtr.Zero)
            {
                AllocConsole();

                var stdOut = GetStdHandle(STD_OUTPUT_HANDLE);
                var stdErr = GetStdHandle(STD_ERROR_HANDLE);
                var stdIn = GetStdHandle(STD_INPUT_HANDLE);

                StreamWriter writer = new(Console.OpenStandardOutput()) { AutoFlush = true };
                Console.SetOut(writer);

                StreamWriter errorWriter = new(Console.OpenStandardError()) { AutoFlush = true };
                Console.SetError(errorWriter);

                StreamReader reader = new(Console.OpenStandardInput());
                Console.SetIn(reader);
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