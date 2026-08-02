using System.Reflection;
using Tharga.Team.Support.Slack;

namespace Tharga.Team.Support.Tests;

/// <summary>
/// The Slack transport must stay ignorant of teams, users and audit entries, so lifting it into a
/// standalone <c>Tharga.Slack</c> package is a move rather than a rewrite.
/// </summary>
/// <remarks>
/// Written as a guard rather than a comment because this is the kind of rule that decays quietly: one
/// convenience overload taking an <c>AuditEntry</c> is all it takes, and nothing else would complain.
/// <para>
/// Each test carries a self-check. Three guards in this repo have shipped passing while examining
/// nothing — a scan that found no files, an assembly that was never loaded — so a reflection guard that
/// cannot demonstrate it looked at something is not evidence.
/// </para>
/// </remarks>
public class SlackNamespaceIsolationTests
{
    private const string SlackNamespace = "Tharga.Team.Support.Slack";
    private const string TeamAssemblyPrefix = "Tharga.Team";

    private static Type[] SlackTypes() =>
        typeof(ISlackClient).Assembly.GetTypes()
            .Where(t => t.Namespace == SlackNamespace)
            .ToArray();

    /// <summary>The self-check: without this, an empty scan would satisfy every assertion below.</summary>
    [Fact]
    public void TheScanFindsTheSlackTypes()
    {
        var types = SlackTypes();

        Assert.NotEmpty(types);
        Assert.Contains(types, t => t == typeof(SlackClient));
        Assert.Contains(types, t => t == typeof(ISlackClient));
        Assert.Contains(types, t => t == typeof(SlackOptions));
    }

    [Fact]
    public void NoSlackTypeExposesATeamType()
    {
        var offenders = new List<string>();

        foreach (var type in SlackTypes())
        {
            foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                foreach (var referenced in ReferencedTypes(member))
                {
                    if (IsTeamType(referenced)) offenders.Add($"{type.Name}.{member.Name} -> {referenced.FullName}");
                }
            }
        }

        Assert.Empty(offenders);
    }

    /// <summary>
    /// The self-check for the guard above: a type that <i>does</i> expose a Team type must be detected,
    /// otherwise "no offenders" only means the detector never fires.
    /// </summary>
    [Fact]
    public void TheDetector_RecognisesATeamType()
    {
        Assert.True(IsTeamType(typeof(Tharga.Team.Service.Audit.AuditEntry)));
        Assert.True(IsTeamType(typeof(Tharga.Team.AccessLevel)));
        Assert.False(IsTeamType(typeof(string)));
        Assert.False(IsTeamType(typeof(SlackPostResult)));
    }

    /// <remarks>
    /// Recurses into element and argument types only. <c>GetGenericTypeDefinition()</c> looks like the
    /// natural third case and is a trap: on a definition it returns itself, so the recursion never ends.
    /// It is also unnecessary — <c>List&lt;AuditEntry&gt;</c> and <c>List&lt;&gt;</c> report the same
    /// assembly, so the outer type is already covered by the check below.
    /// </remarks>
    private static bool IsTeamType(Type type)
    {
        if (type == null || type.IsGenericParameter) return false;
        if (type.HasElementType) return IsTeamType(type.GetElementType());
        if (type.IsGenericType && type.GetGenericArguments().Any(IsTeamType)) return true;

        var assembly = type.Assembly.GetName().Name;
        if (assembly == null || !assembly.StartsWith(TeamAssemblyPrefix, StringComparison.Ordinal)) return false;

        // Types in this namespace are the transport itself, not something crossing in.
        return type.Namespace?.StartsWith(SlackNamespace, StringComparison.Ordinal) != true;
    }

    private static IEnumerable<Type> ReferencedTypes(MemberInfo member)
    {
        switch (member)
        {
            case MethodInfo method:
                yield return method.ReturnType;
                foreach (var parameter in method.GetParameters()) yield return parameter.ParameterType;
                break;
            case ConstructorInfo constructor:
                foreach (var parameter in constructor.GetParameters()) yield return parameter.ParameterType;
                break;
            case PropertyInfo property:
                yield return property.PropertyType;
                break;
            case FieldInfo field:
                yield return field.FieldType;
                break;
        }
    }
}
