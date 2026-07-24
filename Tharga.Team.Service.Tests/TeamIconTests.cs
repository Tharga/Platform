using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Tharga.Team;
using Tharga.Team.Service.Audit;

namespace Tharga.Team.Service.Tests;

/// <summary>
/// Team-icon operations: the <see cref="TeamServiceBase"/> orchestration (store bytes → persist reference
/// → delete the previous blob; NotSupported without a store), plus the authorization (team:manage) and
/// audit (icon-set / icon-clear) decorators.
/// </summary>
public class TeamIconTests
{
    // ---- Orchestration (TeamServiceBase) ----

    private sealed class IconTestTeamService : TeamServiceBase
    {
        private readonly IIconStore _store;
        public ITeam Team { get; set; }
        public string SetReference { get; private set; }
        public bool SetReferenceCalled { get; private set; }

        public IconTestTeamService(IIconStore store) : base(Substitute.For<IUserService>(), iconStore: store)
        {
            _store = store;
        }

        protected override Task<ITeam> GetTeamAsync(string teamKey) => Task.FromResult(Team);

        protected override Task SetTeamIconReferenceInternalAsync(string teamKey, string reference)
        {
            SetReferenceCalled = true;
            SetReference = reference;
            return Task.CompletedTask;
        }

        // Unused abstracts.
        protected override IAsyncEnumerable<ITeam> GetTeamsAsync(IUser user) => throw new NotImplementedException();
        protected override Task<ITeam> CreateTeamAsync(string teamKey, string name, IUser user, string displayName = null) => throw new NotImplementedException();
        protected override Task SetTeamNameAsync(string teamKey, string name) => throw new NotImplementedException();
        protected override Task DeleteTeamAsync(string teamKey) => throw new NotImplementedException();
        protected override Task AddTeamMemberAsync(string teamKey, InviteUserModel model) => throw new NotImplementedException();
        protected override Task RemoveTeamMemberAsync(string teamKey, string userKey) => throw new NotImplementedException();
        protected override Task<ITeam> SetTeamMemberInvitationResponseAsync(string teamKey, string userKey, string inviteKey, bool accept) => throw new NotImplementedException();
        protected override Task SetTeamMemberLastSeenAsync(string teamKey, string userKey) => throw new NotImplementedException();
        protected override Task<ITeamMember> GetTeamMembersAsync(string teamKey, string userKey) => throw new NotImplementedException();
        protected override Task SetTeamMemberRoleAsync(string teamKey, string userKey, AccessLevel accessLevel) => throw new NotImplementedException();
        protected override Task SetTeamMemberTenantRolesAsync(string teamKey, string userKey, string[] tenantRoles) => throw new NotImplementedException();
        protected override Task SetTeamMemberScopeOverridesAsync(string teamKey, string userKey, string[] scopeOverrides) => throw new NotImplementedException();
        protected override Task SetTeamMemberNameAsync(string teamKey, string userKey, string name) => throw new NotImplementedException();
        protected override Task SetTeamConsentInternalAsync(string teamKey, string[] consentedRoles, AccessLevel? accessLevel) => throw new NotImplementedException();
        protected override IAsyncEnumerable<ITeam> GetConsentedTeamsInternalAsync(string[] userRoles) => throw new NotImplementedException();
        protected override Task SetTeamCustomRolesInternalAsync(string teamKey, IReadOnlyList<TenantRoleDefinition> customRoles) => throw new NotImplementedException();
    }

    [Fact]
    public async Task SetTeamIcon_StoresPersistsReference_NoPreviousBlobToDelete()
    {
        var store = Substitute.For<IIconStore>();
        store.SaveAsync(IconKind.Team, "T1", Arg.Any<byte[]>(), "image/png").Returns("new-ref");
        var sut = new IconTestTeamService(store) { Team = new TeamModel { Key = "T1", Name = "T", Icon = null } };

        await sut.SetTeamIconAsync("T1", [1, 2, 3], "image/png");

        Assert.Equal("new-ref", sut.SetReference);
        await store.Received(1).SaveAsync(IconKind.Team, "T1", Arg.Any<byte[]>(), "image/png");
        await store.DidNotReceiveWithAnyArgs().DeleteAsync(default);
    }

    [Fact]
    public async Task SetTeamIcon_ReplacingExisting_DeletesPreviousBlob()
    {
        var store = Substitute.For<IIconStore>();
        store.SaveAsync(IconKind.Team, "T1", Arg.Any<byte[]>(), "image/png").Returns("new-ref");
        var sut = new IconTestTeamService(store) { Team = new TeamModel { Key = "T1", Name = "T", Icon = "old-ref" } };

        await sut.SetTeamIconAsync("T1", [1], "image/png");

        Assert.Equal("new-ref", sut.SetReference);
        await store.Received(1).DeleteAsync("old-ref");
    }

    [Fact]
    public async Task ClearTeamIcon_WithIcon_ClearsAndDeletes()
    {
        var store = Substitute.For<IIconStore>();
        var sut = new IconTestTeamService(store) { Team = new TeamModel { Key = "T1", Name = "T", Icon = "ref-1" } };

        await sut.ClearTeamIconAsync("T1");

        Assert.True(sut.SetReferenceCalled);
        Assert.Null(sut.SetReference);
        await store.Received(1).DeleteAsync("ref-1");
    }

    [Fact]
    public async Task ClearTeamIcon_NoIcon_NoOp()
    {
        var store = Substitute.For<IIconStore>();
        var sut = new IconTestTeamService(store) { Team = new TeamModel { Key = "T1", Name = "T", Icon = null } };

        await sut.ClearTeamIconAsync("T1");

        Assert.False(sut.SetReferenceCalled);
        await store.DidNotReceiveWithAnyArgs().DeleteAsync(default);
    }

    [Fact]
    public async Task SetTeamIcon_NoStore_ThrowsNotSupported()
    {
        var sut = new IconTestTeamService(null) { Team = new TeamModel { Key = "T1", Name = "T" } };
        await Assert.ThrowsAsync<NotSupportedException>(() => sut.SetTeamIconAsync("T1", [1], "image/png"));
    }

    // ---- Authorization decorator ----

    private static (AuthorizationTeamServiceDecorator Sut, ITeamService Inner) BuildAuth(ClaimsPrincipal principal)
    {
        var inner = Substitute.For<ITeamService>();
        var accessor = Substitute.For<ITeamPrincipalAccessor>();
        accessor.GetCurrentAsync().Returns(new ValueTask<ClaimsPrincipal>(principal));
        var sut = new AuthorizationTeamServiceDecorator(inner, new TeamAuthorizer(accessor), new TeamLifecycleOptions { AllowTeamCreation = true });
        return (sut, inner);
    }

    private static ClaimsPrincipal Principal(string teamKey, params string[] scopes)
    {
        var claims = new List<Claim>();
        if (teamKey != null) claims.Add(new Claim(TeamClaimTypes.TeamKey, teamKey));
        foreach (var s in scopes) claims.Add(new Claim(TeamClaimTypes.Scope, s));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    [Fact]
    public async Task Auth_SetIcon_WithManageOnTeam_Delegates()
    {
        var (sut, inner) = BuildAuth(Principal("T1", TeamScopes.Manage));
        await sut.SetTeamIconAsync("T1", [1], "image/png");
        await inner.Received(1).SetTeamIconAsync("T1", Arg.Any<byte[]>(), "image/png");
    }

    [Fact]
    public async Task Auth_SetIcon_WithoutScope_Throws()
    {
        var (sut, inner) = BuildAuth(Principal("T1"));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.SetTeamIconAsync("T1", [1], "image/png"));
        await inner.DidNotReceiveWithAnyArgs().SetTeamIconAsync(default, default, default);
    }

    [Fact]
    public async Task Auth_ClearIcon_DifferentTeamSelected_Throws()
    {
        var (sut, inner) = BuildAuth(Principal("T2", TeamScopes.Manage));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.ClearTeamIconAsync("T1"));
        await inner.DidNotReceiveWithAnyArgs().ClearTeamIconAsync(default);
    }

    // ---- Audit decorator ----

    [Fact]
    public async Task Audit_SetIcon_LogsContentTypeAndSize()
    {
        var (logger, backend) = FakeAuditLoggerFactory.Create();
        var inner = Substitute.For<ITeamService>();
        var sut = new AuditingTeamServiceDecorator(inner, logger, Substitute.For<IHttpContextAccessor>());

        await sut.SetTeamIconAsync("T1", [1, 2, 3, 4], "image/png");

        var entry = Assert.Single(backend.Entries);
        Assert.Equal("team", entry.Feature);
        Assert.Equal("icon-set", entry.Action);
        Assert.True(entry.Success);
        Assert.Equal("T1", entry.TeamKey);
        Assert.Equal("image/png", entry.Metadata[AuditMetadataKeys.IconContentType]);
        Assert.Equal("4", entry.Metadata[AuditMetadataKeys.IconSize]);
    }

    [Fact]
    public async Task Audit_ClearIcon_Logs()
    {
        var (logger, backend) = FakeAuditLoggerFactory.Create();
        var inner = Substitute.For<ITeamService>();
        var sut = new AuditingTeamServiceDecorator(inner, logger, Substitute.For<IHttpContextAccessor>());

        await sut.ClearTeamIconAsync("T1");

        var entry = Assert.Single(backend.Entries);
        Assert.Equal("icon-clear", entry.Action);
        Assert.Equal("T1", entry.TeamKey);
    }
}
