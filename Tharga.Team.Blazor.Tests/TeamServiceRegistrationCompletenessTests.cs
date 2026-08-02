using Microsoft.Extensions.DependencyInjection;
using Tharga.Team;
using Tharga.Team.Blazor.Framework;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// Both <c>RegisterTeamService</c> overloads register the same injectable services.
/// </summary>
/// <remarks>
/// <b>This is the assertion that would have caught the reported defect</b> — and the repository had the
/// opposite one. The five facets were registered inside <c>if (o._memberType != null)</c>, set only by
/// the three-argument overload, and a test named
/// <c>RegisterTeamService_TwoParams_DoesNotRegisterITeamManagementService</c> asserted that gap as
/// intended behaviour. So it was not an oversight that slipped through: it was pinned, and any fix would
/// have looked like a regression. A consuming host's startup broke on it twice, at 3.5.2 and 3.10.0.
/// <para>
/// The sample never caught it either, because it uses the three-argument overload — the covered path was
/// the one this repo takes, not the one a host takes.
/// </para>
/// </remarks>
public class TeamServiceRegistrationCompletenessTests
{
    private static ServiceCollection RegisterWith(Action<ThargaBlazorOptions> configure)
    {
        var services = new ServiceCollection();
        services.AddThargaTeamBlazor(configure);
        return services;
    }

    /// <summary>
    /// Driven from <see cref="TeamServiceFacets.All"/> so a facet added later is covered without editing
    /// this test — not editing it is exactly how the defect arrived twice.
    /// </summary>
    public static TheoryData<Type> Facets()
    {
        var data = new TheoryData<Type>();
        foreach (var facet in TeamServiceFacets.All) data.Add(facet);
        return data;
    }

    [Theory]
    [MemberData(nameof(Facets))]
    public void TwoArgOverload_RegistersEveryFacet_WhenTheMemberTypeIsInferable(Type facet)
    {
        var services = RegisterWith(o => o.RegisterTeamService<InferableStubTeamService, StubUserService>());

        Assert.Contains(services, d => d.ServiceType == facet);
    }

    [Theory]
    [MemberData(nameof(Facets))]
    public void ThreeArgOverload_RegistersEveryFacet(Type facet)
    {
        var services = RegisterWith(o => o.RegisterTeamService<StubTeamService, StubUserService, StubMember>());

        Assert.Contains(services, d => d.ServiceType == facet);
    }

    /// <summary>
    /// An explicit member type wins over inference. The three-argument overload records a decision;
    /// inference only fills a gap where none was expressed, and must never override one.
    /// </summary>
    [Fact]
    public void AnExplicitMemberType_WinsOverInference()
    {
        var services = RegisterWith(o => o.RegisterTeamService<InferableStubTeamService, StubUserService, OtherStubMember>());

        var descriptor = Assert.Single(services, d => d.ServiceType == typeof(TeamManagementService<OtherStubMember>));
        Assert.NotNull(descriptor);
    }

    /// <summary>
    /// A host that registered its own facet keeps it — <c>TryAdd</c>, not <c>Add</c>. That is the one
    /// legitimate reason this was ever left to the host to wire.
    /// </summary>
    [Fact]
    public void AHostsOwnFacetRegistration_Wins()
    {
        var services = new ServiceCollection();
        var own = new HostOwnOversightService();
        services.AddSingleton<ITeamOversightService>(own);
        services.AddThargaTeamBlazor(o => o.RegisterTeamService<InferableStubTeamService, StubUserService>());

        // Asserted on the descriptor rather than by resolving: resolving drags in the whole graph
        // (AuthenticationStateProvider and everything under it), which is a different question than
        // whether TryAdd left the host's registration alone.
        var descriptor = Assert.Single(services, d => d.ServiceType == typeof(ITeamOversightService));
        Assert.Same(own, descriptor.ImplementationInstance);
    }

    // ---- inference ----

    [Fact]
    public void Inference_FindsTheMemberTypeFromAGenericBase()
    {
        Assert.Equal(typeof(StubMember), TeamMemberTypeResolver.Resolve(typeof(InferableStubTeamService)));
    }

    /// <summary>It walks the whole chain, not just the immediate base.</summary>
    [Fact]
    public void Inference_WalksTheWholeBaseChain()
    {
        Assert.Equal(typeof(StubMember), TeamMemberTypeResolver.Resolve(typeof(DeeplyDerivedStubTeamService)));
    }

    /// <summary>
    /// A service deriving straight from <c>TeamServiceBase</c> carries no member type, so inference
    /// returns null rather than guessing. That is the case the startup check has to name.
    /// </summary>
    [Fact]
    public void Inference_ReturnsNullWhenThereIsNothingToInferFrom()
    {
        Assert.Null(TeamMemberTypeResolver.Resolve(typeof(StubTeamService)));
    }

    [Fact]
    public void ResolveOrDefault_FallsBackOnlyWhenInferenceFails()
    {
        Assert.Equal(typeof(StubMember),
            TeamMemberTypeResolver.ResolveOrDefault(typeof(InferableStubTeamService), typeof(OtherStubMember)));
        Assert.Equal(typeof(OtherStubMember),
            TeamMemberTypeResolver.ResolveOrDefault(typeof(StubTeamService), typeof(OtherStubMember)));
    }
}

internal class GenericStubTeamService<TMember> : StubTeamService where TMember : ITeamMember;
internal class InferableStubTeamService : GenericStubTeamService<StubMember>;
internal class DeeplyDerivedStubTeamService : InferableStubTeamService;
internal class OtherStubMember : StubMember;

/// <summary>Stands in for a host that registered its own facet before calling the toolkit.</summary>
internal sealed class HostOwnOversightService : ITeamOversightService
{
    public IAsyncEnumerable<ITeam> GetAllTeamsAsync() => throw new NotSupportedException();
    public IAsyncEnumerable<ITeam<TMember>> GetAllTeamsAsync<TMember>() where TMember : ITeamMember => throw new NotSupportedException();
}
