using Star_Citizen_Log_Parser.LogReader;
using System.Text.RegularExpressions;

namespace Star_Citizen_Log_Parser.Parsing
{
    public class LogParser
    {
        private readonly List<LogTemplate> templates;
        private LogEntry? currentMultilineEntry;

        // Matches a contiguous block of tags (e.g., [Tag1][Tag2][Tag3]) at the end of a line.
        private static readonly Regex tagBlockAtEnd = new(@"(\[(?:[^\[\]]+)\])+$", RegexOptions.Compiled);

        // Extracts individual tags from within square brackets (e.g., [Tag] => "Tag").
        private static readonly Regex extractTags = new(@"\[(?<tag>[^\[\]]+)\]", RegexOptions.Compiled);

        public event Action<LogEntry>? EntryCompleted;

        public LogParser(List<LogTemplate> templates)
        {
            this.templates = templates;
        }

        /// <summary>
        /// Processes an incoming log line using a state machine.
        /// Handles both single-line and multi-line log entries.
        /// </summary>
        /// <param name="line">The raw log line to process.</param>
        public void HandleLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return;

            DateTime? timestamp = null;
            string message;

            if (line.Length > 26 && line[0] == '<' && line[25] == '>' && DateTime.TryParse(line[1..25], out var ts))
            {
                timestamp = ts;
                message = line[27..].Trim();
            }
            else
            {
                message = line.Trim();
            }

            if (currentMultilineEntry != null)
            {
                currentMultilineEntry.Lines.Add(line);
                if (currentMultilineEntry.Template?.MatchesEnd(line) == true)
                {
                    EntryCompleted?.Invoke(currentMultilineEntry);
                    currentMultilineEntry = null;
                }
                return;
            }

            foreach (var template in templates)
            {
                if (!template.PreMatch(message))
                    continue;

                if (timestamp == null)
                    return;

                var entry = new LogEntry(timestamp.Value, template);
                entry.Tags.AddRange(ExtractTagsFromEnd(message, out string messageWithoutTags));
                entry.Lines.Add(messageWithoutTags);

                if (template.IsMultiline && template.MatchesStart(message))
                {
                    currentMultilineEntry = entry;
                    return;
                }

                if (template.TryMatch(messageWithoutTags, out var values) && values != null)
                {
                    entry.Fields = values;
                }

                EntryCompleted?.Invoke(entry);
                return;
            }
        }

        /// <summary>
        /// Extracts trailing tags (e.g., [Tag1][Tag2]) from the end of the log line, if any.
        /// Removes the tag block from the message.
        /// </summary>
        /// <param name="line">The original log line.</param>
        /// <param name="message">The cleaned message with tags removed.</param>
        /// <returns>A list of extracted tag strings.</returns>
        private static List<string> ExtractTagsFromEnd(string line, out string message)
        {
            var tags = new List<string>();
            var tagBlockMatch = tagBlockAtEnd.Match(line);

            if (tagBlockMatch.Success)
            {
                string tagBlock = tagBlockMatch.Value;
                foreach (Match m in extractTags.Matches(tagBlock))
                {
                    tags.Add(m.Groups["tag"].Value);
                }
                message = line[..tagBlockMatch.Index].TrimEnd();
            }
            else
            {
                message = line;
            }

            return tags;
        }
    }
}