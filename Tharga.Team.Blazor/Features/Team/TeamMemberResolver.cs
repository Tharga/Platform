using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Tharga.Team.Blazor.Features.Team;

internal static class TeamMemberResolver
{
    public static TMember Resolve<TMember>(
        IEnumerable<TMember> members,
        Func<TMember, bool> predicate,
        ILogger logger,
        string teamKey,
        string lookupKey)
        where TMember : class, ITeamMember
    {
        var matches = members.Where(predicate).ToArray();

        if (matches.Length == 0) return null;
        if (matches.Length == 1) return matches[0];

        logger?.LogWarning(
            "Duplicate TeamMember rows for team {TeamKey} key {LookupKey}: found {Count}. Using first; clean up duplicates.",
            teamKey, lookupKey, matches.Length);

        return matches[0];
    }
}
