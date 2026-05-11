using Microsoft.Extensions.Logging;

namespace Tharga.Team;

/// <summary>
/// Resilient lookup against in-memory sequences that are expected to contain at most one match
/// but where duplicate rows have been observed in production data (see GitHub issue Tharga/Platform#64).
/// Returns the first match and logs a warning when more than one match is present, instead of
/// throwing the way <see cref="System.Linq.Enumerable.Single{TSource}(IEnumerable{TSource}, Func{TSource, bool})"/>
/// would. Duplicates are surfaced via <c>ILogger</c> so they can be found and cleaned up out of band.
/// </summary>
public static class ResilientMemberLookup
{
    /// <summary>
    /// Returns the first element matching <paramref name="predicate"/>, or <c>default</c> if there is no match.
    /// When more than one element matches, logs a warning carrying <paramref name="teamKey"/>,
    /// <paramref name="lookupKey"/>, and the match count, then returns the first match.
    /// </summary>
    public static T PickOneOrDefault<T>(
        this IEnumerable<T> source,
        Func<T, bool> predicate,
        ILogger logger,
        string teamKey,
        string lookupKey)
    {
        if (source == null) return default;

        var matches = source.Where(predicate).ToArray();

        if (matches.Length == 0) return default;
        if (matches.Length == 1) return matches[0];

        logger?.LogWarning(
            "Duplicate rows for team {TeamKey} key {LookupKey}: found {Count}. Using first; clean up duplicates.",
            teamKey, lookupKey, matches.Length);

        return matches[0];
    }

    /// <summary>
    /// Returns the first element matching <paramref name="predicate"/>, or <c>default</c> if there is no match.
    /// When more than one element matches, logs a warning carrying <paramref name="context"/> and the match count.
    /// Use this overload outside the team/member domain (e.g. API-key hash-collision lookups).
    /// </summary>
    public static T PickOneOrDefault<T>(
        this IEnumerable<T> source,
        Func<T, bool> predicate,
        ILogger logger,
        string context)
    {
        if (source == null) return default;

        var matches = source.Where(predicate).ToArray();

        if (matches.Length == 0) return default;
        if (matches.Length == 1) return matches[0];

        logger?.LogWarning(
            "Duplicate rows in {Context}: found {Count}. Using first; clean up duplicates.",
            context, matches.Length);

        return matches[0];
    }

    /// <summary>
    /// Returns a new array containing every element of <paramref name="source"/>, with the single instance
    /// referentially equal to <paramref name="target"/> replaced by <paramref name="replacement"/>.
    /// Used by repository write paths to update a picked row without stripping its duplicate-keyed siblings
    /// (issue Tharga/Platform#64).
    /// </summary>
    public static T[] ReplaceByReference<T>(this IEnumerable<T> source, T target, T replacement)
    {
        if (source == null) return [];
        return source.Select(x => ReferenceEquals(x, target) ? replacement : x).ToArray();
    }
}
