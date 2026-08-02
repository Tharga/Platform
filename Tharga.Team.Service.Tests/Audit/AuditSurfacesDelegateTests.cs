using System.Reflection;
using System.Text.RegularExpressions;
using Tharga.Team.Service.Audit;

namespace Tharga.Team.Service.Tests.Audit;

/// <summary>
/// No surface decides audit access for itself.
/// </summary>
/// <remarks>
/// Invariant <b>I5</b> is now a property of the shape rather than a coincidence between three
/// implementations — but only for as long as no surface quietly reintroduces a check. That is exactly
/// what happened before: <c>AuditAccess</c> was extracted so *"every surface asks the same question of
/// the same code"*, the UI and REST used it, and the MCP resource grew its own rule anyway. Nothing
/// failed, because nothing was looking.
/// <para>
/// A source scan rather than a behavioural test: the claim is about what the surfaces <i>do not do</i>,
/// and absence is not observable by calling them. It is the same reason the dialog button-order guard
/// scans source.
/// </para>
/// </remarks>
public class AuditSurfacesDelegateTests
{
    /// <summary>Removes line, doc and block comments, so a scan matches code rather than prose.</summary>
    private static string StripComments(string source)
    {
        source = Regex.Replace(source, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        return Regex.Replace(source, @"//.*?$", string.Empty, RegexOptions.Multiline);
    }

    private static DirectoryInfo RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !dir.GetDirectories("Tharga.Team.Blazor").Any()) dir = dir.Parent;
        return dir;
    }

    /// <summary>The files that read audit and must not authorize it themselves.</summary>
    private static readonly string[] SurfacePaths =
    [
        @"Tharga.Team.Service\Audit\AuditController.cs",
        @"Tharga.Team.Mcp\TeamSystemResourceProvider.cs",
        @"Tharga.Team.Blazor\Features\Audit\AuditLogView.razor.cs",
    ];

    public static TheoryData<string> Surfaces()
    {
        var data = new TheoryData<string>();
        foreach (var path in SurfacePaths) data.Add(path);
        return data;
    }

    /// <summary>
    /// A surface must not call <c>AuditAccess</c>. The gate lives on the services, and a second copy at a
    /// surface is a second answer waiting to disagree with the first.
    /// </summary>
    [Theory]
    [MemberData(nameof(Surfaces))]
    public void NoSurfaceCallsTheOldStaticGate(string relativePath)
    {
        var path = Path.Combine(RepoRoot().FullName, relativePath);
        if (!File.Exists(path)) return;   // the view is optional in some layouts; absence is not a failure

        // Comments stripped first. The naive version matched the comment *explaining* why the call was
        // removed, and failed on the very change it was written to verify -- a guard that cannot tell an
        // explanation from a call is worse than none, because its failures teach you to ignore it.
        Assert.DoesNotContain("AuditAccess.CanRead", StripComments(File.ReadAllText(path)));
    }

    /// <summary>
    /// The guard finds the files it claims to check. A path scan that silently matches nothing passes
    /// forever while reading as coverage — the mistake this session already made once with a container
    /// validation test.
    /// </summary>
    [Fact]
    public void TheGuard_FindsTheSurfacesItChecks()
    {
        var root = RepoRoot();
        Assert.NotNull(root);

        var found = SurfacePaths.Count(p => File.Exists(Path.Combine(root.FullName, p)));

        Assert.True(found >= 2, $"Expected to find at least two audit surfaces to check, found {found}.");
    }

    /// <summary>
    /// Both audit interfaces carry the scope attribute. Without it <c>ScopeProxy</c> passes the call
    /// straight through, and every surface would be unguarded precisely because they stopped guarding
    /// themselves.
    /// </summary>
    [Theory]
    [InlineData(typeof(IAuditReadService))]
    [InlineData(typeof(IAuditOversightService))]
    public void EveryAuditServiceMethod_CarriesTheScopeAttribute(Type contract)
    {
        var methods = contract.GetMethods();
        Assert.NotEmpty(methods);

        foreach (var method in methods)
        {
            var attribute = method.GetCustomAttribute<RequireScopeAttribute>();
            Assert.True(attribute != null, $"{contract.Name}.{method.Name} has no [RequireScope].");
            Assert.Equal(AuditScopes.Read, attribute.Scope);
        }
    }
}
