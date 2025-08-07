using System.Text;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;

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

        // Multi-line format
        [YamlMember(Alias = "starts-with", ApplyNamingConventions = false)]
        public string? StartsWith { get; set; }

        [YamlMember(Alias = "ends-with", ApplyNamingConventions = false)]
        public string? EndsWith { get; set; }

        // Internal parser state
        private Regex? pattern;                  // Regex compiled from Template
        private string? simplePattern;           // Leading literal text from Template
        private string? simpleStartsWith;
        private string? simpleEndsWith;
        private List<string> tokens = [];        // Ordered token names in regex capture groups

        /// <summary>
        /// Whether this template describes a multiline log block.
        /// Automatically derived from StartsWith/EndsWith.
        /// </summary>
        public bool IsMultiline => StartsWith != null && EndsWith != null;

        /// <summary>
        /// Initialize this log template: validate fields, prepare regex and token list if needed.
        /// Call after loading from YAML.
        /// </summary>
        internal void Init()
        {
            // Validation
            if (Id == null)
                throw new InvalidDataException("LogTemplate is missing 'id'");

            bool hasSingle = Template != null;
            bool hasMulti = StartsWith != null || EndsWith != null;

            if (!hasSingle && !hasMulti)
                throw new InvalidDataException($"LogTemplate '{Id}' is missing both 'template' and 'starts-with'");

            if (hasSingle && hasMulti)
                throw new InvalidDataException($"LogTemplate '{Id}' cannot define both 'template' and 'starts-with'/'ends-with'");

            if (StartsWith != null && EndsWith == null)
                throw new InvalidDataException($"LogTemplate '{Id}' defines 'starts-with' but is missing 'ends-with'");

            if (EndsWith != null && StartsWith == null)
                throw new InvalidDataException($"LogTemplate '{Id}' defines 'ends-with' but is missing 'starts-with'");

            if (Template != null)
            {
                Template = Template.Trim();

                var (regex, tokens) = CompileTemplate(Template);
                pattern = regex;
                this.tokens = tokens;

                // Save the leading literal (before first token) for fast pre-match
                simplePattern = Template.Split('{')[0];
            }

            if (StartsWith != null)
            {
                StartsWith = StartsWith.Trim();
                simpleStartsWith = StartsWith.Split('{')[0];
            }
            if (EndsWith != null)
            {
                EndsWith = EndsWith.Trim();
                simpleEndsWith = EndsWith.Split('{')[0];
            }
        }

        /// <summary>
        /// Fast pre-filtering for a log line before calling TryMatch().
        /// Avoids expensive regex evaluation when possible.
        /// </summary>
        public bool PreMatch(string line)
        {
            if (simplePattern != null && line.StartsWith(simplePattern, StringComparison.OrdinalIgnoreCase))
                return true;

            if (StartsWith != null && line.StartsWith(simpleStartsWith, StringComparison.OrdinalIgnoreCase))
                return true;

            if (EndsWith != null && line.Contains(simpleEndsWith, StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        /// <summary>
        /// Attempts to match a log line to this template and extract its fields.
        /// Only works for single-line templates.
        /// </summary>
        public bool TryMatch(string line, out Dictionary<string, string>? values)
        {
            values = null;

            if (pattern == null || IsMultiline)
                return false;

            var match = pattern.Match(line);
            if (!match.Success)
                return false;

            values = tokens.ToDictionary(t => t, t => match.Groups[t].Value);
            return true;
        }


        /// <summary>
        /// Returns true if this line matches the configured block start for a multiline template.
        /// </summary>
        public bool MatchesStart(string line)
        {
            return StartsWith != null && line.StartsWith(simpleStartsWith, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns true if this line matches the configured block end for a multiline template.
        /// </summary>
        public bool MatchesEnd(string line)
        {
            return EndsWith != null && line.Contains(simpleEndsWith, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Compiles a log template string into a regex with named capture groups.
        /// Returns both the regex and the ordered list of token names.
        /// </summary>
        private static (Regex regex, List<string> tokens) CompileTemplate(string template)
        {
            var tokens = new List<string>();
            var sb = new StringBuilder();
            sb.Append("^");

            int pos = 0;
            var tokenRegex = new Regex(@"\{(\w+)\}");

            foreach (Match match in tokenRegex.Matches(template))
            {
                // Literal part before the token
                string literal = template.Substring(pos, match.Index - pos);
                sb.Append(Regex.Escape(literal));

                // Determine context
                string tokenName = match.Groups[1].Value;
                tokens.Add(tokenName);

                char charBefore = match.Index > 0 ? template[match.Index - 1] : '\0';
                char charAfter = match.Index + match.Length < template.Length ? template[match.Index + match.Length] : '\0';

                string pattern = InferPattern(charBefore, charAfter);
                sb.Append($@"(?<{tokenName}>{pattern})");

                pos = match.Index + match.Length;
            }

            // Remaining text
            if (pos < template.Length)
                sb.Append(Regex.Escape(template.Substring(pos)));

            sb.Append("$");

            return (new Regex(sb.ToString(), RegexOptions.Compiled), tokens);
        }

        /// <summary>
        /// Chooses a default regex pattern for capturing values based on surrounding characters.
        /// Helps prevent greedy matches that consume surrounding syntax.
        /// </summary>
        private static string InferPattern(char before, char after)
        {
            return (before, after) switch
            {
                ('\'', '\'') => @"[^']+",      // Inside single quotes
                ('[', ']') => @"[^\]]+",     // Inside square brackets
                _ => @".+?"         // Fallback non-greedy match
            };
        }
    }
}
