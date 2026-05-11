using System.Text.Json;
using Tharga.Mcp;
using Tharga.Platform.Mcp;
using Tharga.Team;

namespace Tharga.Platform.Mcp.Tests;

public class PlatformTeamResourceProviderTests
{
    private readonly ITeamService _teamService = Substitute.For<ITeamService>();
    private readonly IApiKeyAdministrationService _apiKeyService = Substitute.For<IApiKeyAdministrationService>();

    private IMcpContext MakeContext(string teamId)
    {
        var ctx = Substitute.For<IMcpContext>();
        ctx.TeamId.Returns(teamId);
        ctx.Scope.Returns(McpScope.Team);
        return ctx;
    }

    private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(params T[] items)
    {
        foreach (var item in items) yield return item;
        await Task.CompletedTask;
    }

    [Fact]
    public async Task ListResourcesAsync_NoTeamId_ReturnsEmpty()
    {
        var sut = new PlatformTeamResourceProvider(_teamService, _apiKeyService);

        var result = await sut.ListResourcesAsync(MakeContext(teamId: null), TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ListResourcesAsync_WithTeamId_Returns_Three_When_ApiKeyServiceRegistered()
    {
        var sut = new PlatformTeamResourceProvider(_teamService, _apiKeyService);

        var result = await sut.ListResourcesAsync(MakeContext("T-1"), TestContext.Current.CancellationToken);

        Assert.Equal(3, result.Count);
        Assert.Contains(result, r => r.Uri == PlatformTeamResourceProvider.TeamUri);
        Assert.Contains(result, r => r.Uri == PlatformTeamResourceProvider.MembersUri);
        Assert.Contains(result, r => r.Uri == PlatformTeamResourceProvider.ApiKeysUri);
    }

    [Fact]
    public async Task ListResourcesAsync_WithTeamId_OmitsApiKeys_When_ApiKeyServiceNotRegistered()
    {
        var sut = new PlatformTeamResourceProvider(_teamService, apiKeyAdministrationService: null);

        var result = await sut.ListResourcesAsync(MakeContext("T-1"), TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, r => r.Uri == PlatformTeamResourceProvider.ApiKeysUri);
    }

    [Fact]
    public async Task ReadResourceAsync_NoTeamId_Throws()
    {
        var sut = new PlatformTeamResourceProvider(_teamService, _apiKeyService);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.ReadResourceAsync(PlatformTeamResourceProvider.TeamUri, MakeContext(teamId: null), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ReadResourceAsync_UnknownUri_Throws()
    {
        var sut = new PlatformTeamResourceProvider(_teamService, _apiKeyService);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ReadResourceAsync("platform://team/bogus", MakeContext("T-1"), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ReadResourceAsync_TeamUri_ReturnsTeamMetadata()
    {
        var team = Substitute.For<ITeam>();
        team.Key.Returns("T-1");
        team.Name.Returns("Acme");
        team.Icon.Returns((string)null);
        team.ConsentedRoles.Returns(new[] { "viewer" });
        _teamService.GetTeamsAsync().Returns(ToAsyncEnumerable(team));

        var sut = new PlatformTeamResourceProvider(_teamService, _apiKeyService);
        var content = await sut.ReadResourceAsync(PlatformTeamResourceProvider.TeamUri, MakeContext("T-1"), TestContext.Current.CancellationToken);

        using var doc = JsonDocument.Parse(content.Text);
        var root = doc.RootElement;
        Assert.Equal("T-1", root.GetProperty("key").GetString());
        Assert.Equal("Acme", root.GetProperty("name").GetString());
        Assert.Equal(1, root.GetProperty("consentedRoles").GetArrayLength());
    }

    [Fact]
    public async Task ReadResourceAsync_TeamUri_NotInCallerTeams_Throws()
    {
        // The MCP caller's TeamKey claim says T-1 but GetTeamsAsync() returns a different team.
        var otherTeam = Substitute.For<ITeam>();
        otherTeam.Key.Returns("T-OTHER");
        _teamService.GetTeamsAsync().Returns(ToAsyncEnumerable(otherTeam));

        var sut = new PlatformTeamResourceProvider(_teamService, _apiKeyService);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ReadResourceAsync(PlatformTeamResourceProvider.TeamUri, MakeContext("T-1"), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ReadResourceAsync_MembersUri_ReturnsMembers()
    {
        var m1 = Substitute.For<ITeamMember>();
        m1.Key.Returns("u-1");
        m1.Name.Returns("One");
        m1.AccessLevel.Returns(AccessLevel.Owner);
        m1.State.Returns(MembershipState.Member);
        var m2 = Substitute.For<ITeamMember>();
        m2.Key.Returns("u-2");
        m2.AccessLevel.Returns(AccessLevel.User);
        m2.Invitation.Returns(new Invitation { InviteKey = "inv-abc", EMail = "two@example.com", InviteTime = DateTime.UtcNow });
        _teamService.GetMembersAsync("T-1").Returns(ToAsyncEnumerable(m1, m2));

        var sut = new PlatformTeamResourceProvider(_teamService, _apiKeyService);
        var content = await sut.ReadResourceAsync(PlatformTeamResourceProvider.MembersUri, MakeContext("T-1"), TestContext.Current.CancellationToken);

        using var doc = JsonDocument.Parse(content.Text);
        var root = doc.RootElement;
        Assert.Equal("T-1", root.GetProperty("teamKey").GetString());
        var items = root.GetProperty("items");
        Assert.Equal(2, items.GetArrayLength());
        Assert.False(items[0].GetProperty("invited").GetBoolean());
        Assert.True(items[1].GetProperty("invited").GetBoolean());
    }

    [Fact]
    public async Task ReadResourceAsync_ApiKeysUri_ReturnsRedactedKeys()
    {
        var key = Substitute.For<IApiKey>();
        key.Key.Returns("k-1");
        key.Name.Returns("Default");
        key.ApiKey.Returns("RAW-SECRET-VALUE-SHOULD-BE-REDACTED");
        key.AccessLevel.Returns(AccessLevel.Administrator);
        key.Roles.Returns(new[] { "Editor" });
        key.CreatedBy.Returns("alice");
        _apiKeyService.GetKeysAsync("T-1").Returns(ToAsyncEnumerable(key));

        var sut = new PlatformTeamResourceProvider(_teamService, _apiKeyService);
        var content = await sut.ReadResourceAsync(PlatformTeamResourceProvider.ApiKeysUri, MakeContext("T-1"), TestContext.Current.CancellationToken);

        Assert.DoesNotContain("RAW-SECRET-VALUE", content.Text);

        using var doc = JsonDocument.Parse(content.Text);
        var item = doc.RootElement.GetProperty("items")[0];
        Assert.Equal("k-1", item.GetProperty("key").GetString());
        Assert.Equal("Default", item.GetProperty("name").GetString());
        Assert.Equal("alice", item.GetProperty("createdBy").GetString());
        Assert.False(item.TryGetProperty("apiKey", out _));
    }

    [Fact]
    public async Task ReadResourceAsync_ApiKeysUri_NoApiKeyService_Throws()
    {
        var sut = new PlatformTeamResourceProvider(_teamService, apiKeyAdministrationService: null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ReadResourceAsync(PlatformTeamResourceProvider.ApiKeysUri, MakeContext("T-1"), TestContext.Current.CancellationToken));
    }
}
