using Tharga.Team;

namespace Tharga.Team.Blazor.Framework;

/// <summary>
/// Data-access consent options — controls cross-team access granted by a team to global roles.
/// </summary>
public class ConsentOptions
{
    /// <summary>
    /// Global roles that can be granted access to a team via consent. The consent toggle in TeamComponent
    /// offers these roles. Default ["Developer"].
    /// </summary>
    public string[] Roles { get; set; } = ["Developer"];

    /// <summary>Show the consent toggle in TeamComponent for team administrators. Default false.</summary>
    public bool ShowToggle { get; set; } = false;

    /// <summary>Whether new teams start with consent enabled. Default true.</summary>
    public bool Default { get; set; } = true;

    /// <summary>
    /// Default access level granted via team consent, used when the consent itself doesn't carry a level.
    /// Default Viewer.
    /// </summary>
    public AccessLevel AccessLevel { get; set; } = AccessLevel.Viewer;
}
