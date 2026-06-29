using System.Text.RegularExpressions;

namespace LinkUp.SharedKernel.Helpers;

/// <summary>Extracts @username mentions from free text (posts, comments, chat messages).</summary>
public static partial class MentionParser
{
    [GeneratedRegex(@"@([A-Za-z0-9_]{2,50})", RegexOptions.Compiled)]
    private static partial Regex MentionRegex();

    /// <summary>Returns the distinct set of usernames referenced via @ in the text (without the @).</summary>
    public static IReadOnlyCollection<string> ExtractUsernames(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        return MentionRegex()
            .Matches(text)
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
