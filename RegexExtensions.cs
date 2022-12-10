using System.Text;
using System.Text.RegularExpressions;

namespace MultipleGroupRegexReplace
{
    public static class RegexExtensions
    {
        public static string ReplaceGroup(
            this Regex regex, string input, string groupName, string replacement)
        {
            Match match;
            while ((match = regex.Match(input)).Success)
            {
                var sb = new StringBuilder();
                var group = match.Groups[groupName];

                // Anything before the match
                if (match.Index > 0)
                    sb.Append(input.Substring(0, match.Index));

                // The match itself
                var startIndex = group.Index - match.Index;
                var length = group.Length;
                var original = match.Value;
                var prior = original.Substring(0, startIndex);
                var trailing = original.Substring(startIndex + length);
                sb.Append(prior);
                sb.Append(replacement);
                sb.Append(trailing);

                // Anything after the match
                if (match.Index + match.Length < input.Length)
                    sb.Append(input.Substring(match.Index + match.Length));

                input = sb.ToString();
            }

            return input;
        }
    }
}