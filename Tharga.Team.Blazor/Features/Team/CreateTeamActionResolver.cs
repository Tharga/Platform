namespace Tharga.Team.Blazor.Features.Team;

/// <summary>
/// The action a built-in "Create team" entry point should take, resolved from the host's
/// override configuration. See <see cref="CreateTeamActionResolver"/>.
/// </summary>
internal enum CreateTeamAction
{
    /// <summary>Invoke the host-supplied <c>CreateTeamRequested</c> callback.</summary>
    Callback,

    /// <summary>Navigate to the configured <c>CreateTeamPath</c>.</summary>
    Navigate,

    /// <summary>Perform the built-in create (or default navigation for the selector).</summary>
    BuiltIn
}

/// <summary>
/// Resolves the precedence for the built-in "Create team" action shared by <c>TeamSelector</c>
/// and <c>TeamComponent</c>: a host callback wins over a configured path, which wins over the
/// built-in behavior.
/// </summary>
internal static class CreateTeamActionResolver
{
    public static CreateTeamAction Resolve(bool hasCallback, string createTeamPath)
    {
        if (hasCallback) return CreateTeamAction.Callback;
        if (!string.IsNullOrWhiteSpace(createTeamPath)) return CreateTeamAction.Navigate;
        return CreateTeamAction.BuiltIn;
    }
}
