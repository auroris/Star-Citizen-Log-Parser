using System.Text.RegularExpressions;

namespace Star_Citizen_Log_Parser.LogReader
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
            File.WriteAllText("unmatched.log", string.Empty);
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

            // First, split the line into timestamp (optional) and message.
            if (line.Length > 26 && line[0] == '<' && line[25] == '>' && DateTime.TryParse(line[1..25], out var ts))
            {
                timestamp = ts;
                message = line[27..].Trim();
            }
            else
            {
                message = line.Trim();
            }

            // If we are currently handling a multiline entry
            if (currentMultilineEntry != null)
            {
                // Are we at the end of the multiline entry?
                if (currentMultilineEntry.Template?.MatchesEnd(message) == true)
                {
                    currentMultilineEntry.Tags.AddRange(ExtractTagsFromEnd(message, out string messageWithoutTags));
                    currentMultilineEntry.Lines.Add(messageWithoutTags.Trim());

                    var joined = string.Join('\n', currentMultilineEntry.Lines);
                    if (currentMultilineEntry.Template.TryMatch(joined, out var values) && values != null)
                    {
                        currentMultilineEntry.Fields = values;
                    }

                    EntryCompleted?.Invoke(currentMultilineEntry);
                    currentMultilineEntry = null;
                }
                else
                {
                    // Still within a multiline entry, just add the line
                    currentMultilineEntry.Lines.Add(line);
                }
                return;
            }

            // We are not in a multiline entry, so we need to find a template that matches this line.
            foreach (var template in templates)
            {
                if (!template.PreMatch(message))
                    continue;

                // If the timestamp is null, we cannot create a log entry.
                if (timestamp == null)
                    return;

                // Create a new log entry with the matched template. Handle tags (if they are present) and the message.
                var entry = new LogEntry(timestamp.Value, template);
                entry.Tags.AddRange(ExtractTagsFromEnd(message, out string messageWithoutTags));
                entry.Lines.Add(messageWithoutTags.Trim());

                // If the template specifies this is a multiline entry, begin multiline entry processing.
                if (template.IsMultiline && template.MatchesStart(message))
                {
                    currentMultilineEntry = entry;
                    return;
                }

                // If this is a single-line template, try to match and extract fields.
                if (template.TryMatch(messageWithoutTags, out var values) && values != null)
                {
                    entry.Fields = values;
                }
                else
                {
                    // The template didn't actually match; some templates have the same prematch but different fields. So we try another template.
                    continue;
                }

                // The log entry processing is complete
                EntryCompleted?.Invoke(entry);
                return;
            }

            // Debug: No template matched this line, log it for analysis.
            AppendUnhandledLine(line);
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
                message = line[..tagBlockMatch.Index];
            }
            else
            {
                message = line;
            }

            return tags;
        }

        /// <summary>
        /// Appends an unhandled log line to unmatched.log for later analysis.
        /// </summary>
        /// <param name="line"></param>
        private void AppendUnhandledLine(string line)
        {
            File.AppendAllText("unmatched.log", line + Environment.NewLine);
        }
    }
}