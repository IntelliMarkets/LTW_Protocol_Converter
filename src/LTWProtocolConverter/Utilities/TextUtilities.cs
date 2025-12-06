using System.Linq;
using System.Text.RegularExpressions;

namespace LTWProtocolConverter.Utilities;

public static partial class TextUtilities
{
    private static readonly Regex NonSlugChars = SlugRegex();

    public static string Slugify(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var normalized = value.Trim().ToLowerInvariant();
        normalized = NonSlugChars.Replace(normalized, "_");
        normalized = Regex.Replace(normalized, "_+", "_");
        normalized = normalized.Trim('_');
        return string.IsNullOrEmpty(normalized) ? fallback : normalized;
    }

    public static string NormalizeIdentifier(string? value, string prefix)
    {
        var slug = Slugify(value, prefix);
        if (slug.Length < 3)
        {
            slug = slug.PadRight(3, '0');
        }

        return slug.Length > 32 ? slug[..32] : slug;
    }

    public static string ToJsonPointer(string? dotPath)
    {
        if (string.IsNullOrWhiteSpace(dotPath))
        {
            return string.Empty;
        }

        var parts = dotPath.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(RemoveIndexer)
            .Where(p => !string.IsNullOrWhiteSpace(p));

        return "/" + string.Join('/', parts.Select(EscapePointerSegment));
    }

    public static string FromJsonPointer(string? pointer)
    {
        if (string.IsNullOrWhiteSpace(pointer))
        {
            return string.Empty;
        }

        if (pointer.StartsWith('/'))
        {
            pointer = pointer[1..];
        }

        var parts = pointer.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(UnescapePointerSegment);

        return string.Join('.', parts);
    }

    private static string RemoveIndexer(string segment)
    {
        var index = segment.IndexOf('[');
        return index >= 0 ? segment[..index] : segment;
    }

    private static string EscapePointerSegment(string segment)
        => segment.Replace("~", "~0", StringComparison.Ordinal).Replace("/", "~1", StringComparison.Ordinal);

    private static string UnescapePointerSegment(string segment)
        => segment.Replace("~1", "/", StringComparison.Ordinal).Replace("~0", "~", StringComparison.Ordinal);

    [GeneratedRegex("[^a-z0-9_]+")]
    private static partial Regex SlugRegex();
}
