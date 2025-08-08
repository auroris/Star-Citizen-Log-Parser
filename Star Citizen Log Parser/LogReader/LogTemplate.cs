using System.Text;
using System.Text.RegularExpressions;

namespace Star_Citizen_Log_Parser.LogReader
{
    /// <summary>
    /// A structured log template that can match either a single-line log message or a multi-line block
    /// based on configured template rules. Supports YAML-driven configuration.
    /// </summary>
    public class LogTemplate
    {
        // Required identifier
        public string? Id { get; set; }

        // Optional label for debugging, UIs, etc.
        public string? Label { get; set; }

        // Optional tag list
        public List<string> Tags { get; set; } = [];

        // Single-line format
        public string? Template { get; set; }

        // Internal parser state
        private Regex? pattern;                  // Regex compiled from Template
        private string? simpleStartsWith;
        private string? simpleEndsWith;
        private List<string> tokens = [];        // Ordered token names in regex capture groups
        public bool IsMultiline => Template?.Trim().Contains('\n') == true;

        /// <summary>
        /// Initialize this log template: validate fields, prepare regex and token list if needed.
        /// Call after loading from YAML.
        /// </summary>
        internal void Init()
        {
            // Validation
            if (Id == null)
                throw new InvalidDataException("LogTemplate is missing 'id'");

            if (Template == null)
                throw new InvalidDataException($"LogTemplate '{Id}' is missing both 'template'");

            var (regex, tokens) = CompileTemplate(Template.Trim());
            pattern = regex;
            this.tokens = tokens;

            // Infer start/end from template lines
            var lines = Template.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToArray();
            simpleStartsWith = lines.First().Split('{')[0];
            if (lines.Length >= 2)
            {
                simpleEndsWith = lines.Last().Split('{')[0];
            }
        }

        /// <summary>
        /// Fast pre-filtering for a log line before calling TryMatch().
        /// Avoids expensive regex evaluation when possible.
        /// </summary>
        public bool PreMatch(string line)
        {
            if (MatchesStart(line))
                return true;

            if (MatchesEnd(line))
                return true;

            return false;
        }

        public bool TryMatch(string joinedLines, out Dictionary<string, string>? values)
        {
            values = null;

            if (pattern == null)
                return false;

            var match = pattern.Match(joinedLines);
            if (!match.Success)
                return false;

            values = tokens.ToDictionary(t => t, t => match.Groups[t].Value);
            return true;
        }

        public bool MatchesStart(string line)
        {
            return simpleStartsWith != null && line.StartsWith(simpleStartsWith, StringComparison.OrdinalIgnoreCase);
        }

        public bool MatchesEnd(string line)
        {
            return simpleEndsWith != null && line.Contains(simpleEndsWith, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Compiles a log template string into a regex with named capture groups.
        /// Returns both the regex and the ordered list of token names.
        /// </summary>
        public static (Regex regex, List<string> tokens) CompileTemplate(string template)
        {
            var tokens = new List<string>();
            var sb = new StringBuilder();
            int pos = 0;
            var tokenRegex = new Regex(@"\{(\w+)\}");

            foreach (Match match in tokenRegex.Matches(template))
            {
                // Append the literal text before the token, but escape only regex-sensitive characters (not spaces)
                string literal = template.Substring(pos, match.Index - pos);
                sb.Append(SafeEscape(literal));

                string tokenName = match.Groups[1].Value;
                tokens.Add(tokenName);

                char charBefore = match.Index > 0 ? template[match.Index - 1] : '\0';
                char charAfter = match.Index + match.Length < template.Length ? template[match.Index + match.Length] : '\0';

                string pattern = InferPattern(charBefore, charAfter);
                sb.Append($@"(?<{tokenName}>{pattern})");

                pos = match.Index + match.Length;
            }

            if (pos < template.Length)
                sb.Append(SafeEscape(template.Substring(pos)));

            return (new Regex(sb.ToString(), RegexOptions.Compiled | RegexOptions.Multiline), tokens);
        }

        private static string SafeEscape(string input)
        {
            // Escape all special characters, but leave literal spaces unescaped
            return Regex.Escape(input).Replace(@"\ ", " ");
        }

        /// <summary>
        /// Chooses a default regex pattern for capturing values based on surrounding characters.
        /// Helps prevent greedy matches that consume surrounding syntax.
        /// </summary>
        private static string InferPattern(char before, char after)
        {
            if (before == '"' && after == '"') return @"[^""]+";
            if (before == '\'' && after == '\'') return @"[^']+";
            if (before == '[' && after == ']') return @"[^\]]+";
            if (before == '{' && after == '}') return @"[^}]+";
            return @".*?"; // Non-greedy fallback
        }
    }
}
