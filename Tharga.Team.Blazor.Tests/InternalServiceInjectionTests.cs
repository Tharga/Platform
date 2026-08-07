using System.ComponentModel;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Tharga.Team;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// No component may take a dependency on an <b>internal service</b> — the deliberately unchecked contract a
/// host implements, which exists so framework code can read team data while constructing the very claims that
/// would authorize the read.
/// </summary>
/// <remarks>
/// <b>This is the guard, not the fix.</b> The structure it protects is already correct: components inject the
/// gated facets (<c>ITeamManagementService</c> and friends, every member carrying <c>[RequireScope]</c>), and
/// <see cref="ITeamService"/> is marked <see cref="EditorBrowsableState.Never"/> and documented as
/// host-facing. What was missing is anything that fails when that changes.
/// <para>
/// It matters because the toolkit has already paid for its absence once: <c>team:read</c> came to be
/// registered, documented, granted — and enforced by nothing — because a first-level surface injected the
/// unchecked contract. A component resolving <see cref="ITeamService"/> bypasses authorization completely,
/// and nothing about it looks wrong at the call site.
/// </para>
/// <para>
/// Internal services are identified by <see cref="EditorBrowsableAttribute"/>, not by a hard-coded list, so
/// marking a new contract internal enrols it here automatically.
/// </para>
/// </remarks>
public class InternalServiceInjectionTests
{
    /// <summary>
    /// Every contract in the contracts assembly marked <see cref="EditorBrowsableState.Never"/> — the
    /// "Internal" row of the service-classification table.
    /// </summary>
    private static Type[] InternalServices()
        => typeof(ITeamService).Assembly.GetTypes()
            .Where(t => t.IsInterface && t.IsPublic)
            .Where(t => t.GetCustomAttribute<EditorBrowsableAttribute>()?.State == EditorBrowsableState.Never)
            .OrderBy(t => t.Name, StringComparer.Ordinal)
            .ToArray();

    public static TheoryData<Type> Components()
    {
        var data = new TheoryData<Type>();
        foreach (var type in typeof(Framework.ThargaBlazorOptions).Assembly.GetTypes()
                     .Where(t => t is { IsClass: true, IsAbstract: false })
                     .Where(typeof(Microsoft.AspNetCore.Components.IComponent).IsAssignableFrom)
                     .OrderBy(t => t.FullName, StringComparer.Ordinal))
        {
            data.Add(type);
        }

        return data;
    }

    /// <summary>The internal services <paramref name="type"/> depends on. Empty for a correctly-written surface.</summary>
    private static string[] Offenders(Type type)
    {
        var internalServices = InternalServices();

        return Dependencies(type)
            .Where(internalServices.Contains)
            .Select(t => t.Name)
            .Distinct()
            .ToArray();
    }

    [Theory]
    [MemberData(nameof(Components))]
    public void AComponent_DoesNotDependOnAnInternalService(Type componentType)
    {
        var offenders = Offenders(componentType);

        Assert.True(
            offenders.Length == 0,
            $"'{componentType.Name}' depends on {string.Join(", ", offenders)}, which is deliberately " +
            "unchecked and bypasses authorization entirely. Inject a gated facet instead — " +
            $"'{nameof(ITeamManagementService)}' and its siblings carry [RequireScope] on every member. If the " +
            "read genuinely cannot be gated (it names no team), it belongs on a filtered service that " +
            "recomputes the caller's scopes per item, and that has to be said in its XML docs.");
    }

    /// <summary>
    /// Both halves of the theory can silently cover nothing — no components discovered, or no internal
    /// services to look for — and it would still pass, reading as "everything checked".
    /// </summary>
    [Fact]
    public void TheGuard_ActuallyCoversSomething()
    {
        var components = Components();
        var internalServices = InternalServices();

        Assert.True(components.Count > 20, $"Only {components.Count} components discovered; the scan is not reaching the component set.");
        Assert.NotEmpty(internalServices);
        Assert.Contains(typeof(ITeamService), internalServices);
    }

    /// <summary>
    /// The gated facets must NOT be marked internal — if they were, the theory above would start reporting
    /// every correctly-written component as an offender, and the natural fix would be to weaken the test.
    /// </summary>
    [Fact]
    public void TheGatedFacets_AreNotInternal()
    {
        var internalServices = InternalServices();

        Assert.DoesNotContain(typeof(ITeamManagementService), internalServices);
        Assert.DoesNotContain(typeof(ITeamDirectoryService), internalServices);
    }

    /// <summary>
    /// Proves the guard bites. Every component currently passes, so without this the theory could be broken —
    /// scanning the wrong member kind, or comparing types that never match — and stay green forever while
    /// reading as protection. The two fixtures cover both ways a component takes a dependency.
    /// </summary>
    [Theory]
    [InlineData(typeof(InjectsTheInternalService))]
    [InlineData(typeof(TakesTheInternalServiceOnItsConstructor))]
    public void TheGuard_CatchesAViolation(Type offendingType)
    {
        Assert.Equal([nameof(ITeamService)], Offenders(offendingType));
    }

    /// <summary>A component doing what `@inject ITeamService` compiles to — the historical shape of the hole.</summary>
    private sealed class InjectsTheInternalService : ComponentBase
    {
        [Inject] private ITeamService TeamService { get; set; }
    }

    private sealed class TakesTheInternalServiceOnItsConstructor : ComponentBase
    {
        public TakesTheInternalServiceOnItsConstructor(ITeamService teamService) { }
    }

    /// <summary>
    /// Constructor parameters and <c>[Inject]</c> properties. Blazor resolves <c>@inject</c> at render time
    /// through properties, so a constructor-only scan would miss how components actually take dependencies.
    /// </summary>
    private static IEnumerable<Type> Dependencies(Type type)
    {
        foreach (var parameter in type.GetConstructors().SelectMany(c => c.GetParameters()))
            yield return parameter.ParameterType;

        const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        foreach (var property in type.GetProperties(all))
        {
            if (property.GetCustomAttribute<InjectAttribute>() != null)
                yield return property.PropertyType;
        }
    }
}
