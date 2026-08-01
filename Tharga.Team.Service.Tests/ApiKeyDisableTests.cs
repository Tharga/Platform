using Tharga.Team;
using Tharga.Toolkit;
using Tharga.Toolkit.Password;

namespace Tharga.Team.Service.Tests;

/// <summary>
/// Disabling an API key: the reversible alternative to deletion.
/// </summary>
/// <remarks>
/// Deletion loses the key's name, scopes, roles, tags and audit trail, which is far too final for the
/// ordinary cases — a key suspected of leaking, a partner integration paused, a key parked while an
/// incident is investigated.
/// <para>
/// <b>The test that matters most is <see cref="RefreshingADisabledKey_LeavesItDisabled"/>.</b> Refresh
/// rebuilds the entity from selected fields, so a state not carried forward is silently dropped —
/// meaning the remedy for a compromise would quietly undo the containment. That is not a hypothetical:
/// the first implementation of the disabled state had exactly that defect, because
/// <c>BuildKey</c> never saw it.
/// </para>
/// </remarks>
public class ApiKeyDisableTests
{
    private const string TeamKey = "T1";

    private static (ApiKeyAdministrationService Sut, IApiKeyRepository Repository) Build(ApiKeyEntity existing)
    {
        var repository = Substitute.For<IApiKeyRepository>();
        repository.GetAsync(Arg.Any<string>()).Returns(existing);

        var apiKeyService = Substitute.For<IApiKeyService>();
        apiKeyService.BuildApiKey(Arg.Any<string>(), Arg.Any<Func<string>>()).Returns("raw-key");
        apiKeyService.Encrypt(Arg.Any<string>()).Returns("hashed");

        var sut = new ApiKeyAdministrationService(repository, apiKeyService);
        return (sut, repository);
    }

    private static ApiKeyEntity TeamKeyEntity(DateTime? disabledAt = null, string disabledBy = null) => new()
    {
        Key = "k1",
        Name = "Integration",
        TeamKey = TeamKey,
        ApiKeyHash = "hash",
        AccessLevel = AccessLevel.Administrator,
        DisabledAt = disabledAt,
        DisabledBy = disabledBy
    };

    [Fact]
    public async Task Disabling_RecordsWhenAndByWhom()
    {
        var (sut, repository) = Build(TeamKeyEntity());

        await sut.SetKeyDisabledAsync(TeamKey, "k1", disabled: true, actor: "alice");

        await repository.Received(1).SetDisabledAsync("k1",
            Arg.Is<DateTime?>(d => d != null),
            "alice");
    }

    /// <summary>Enabling clears both, so a re-enabled key carries no stale trace of the old decision.</summary>
    [Fact]
    public async Task Enabling_ClearsBoth()
    {
        var (sut, repository) = Build(TeamKeyEntity(DateTime.UtcNow, "alice"));

        await sut.SetKeyDisabledAsync(TeamKey, "k1", disabled: false, actor: "bob");

        await repository.Received(1).SetDisabledAsync("k1", null, null);
    }

    /// <summary>
    /// A refresh mints a new secret. It is <b>not</b> a decision to trust the key again, so the disabled
    /// state must survive it — otherwise the standard response to a suspected leak silently re-enables
    /// the very key that was contained.
    /// </summary>
    [Fact]
    public async Task RefreshingADisabledKey_LeavesItDisabled()
    {
        var disabledAt = new DateTime(2026, 8, 2, 10, 0, 0, DateTimeKind.Utc);
        var (sut, repository) = Build(TeamKeyEntity(disabledAt, "alice"));

        var refreshed = await sut.RefreshKeyAsync(TeamKey, "k1");

        Assert.Equal(disabledAt, refreshed.DisabledAt);
        Assert.Equal("alice", refreshed.DisabledBy);
        await repository.Received(1).UpdateAsync("k1",
            Arg.Is<ApiKeyEntity>(e => e.DisabledAt == disabledAt && e.DisabledBy == "alice"));
    }

    /// <summary>An enabled key refreshes to an enabled key — the carry-forward is not a one-way latch.</summary>
    [Fact]
    public async Task RefreshingAnEnabledKey_LeavesItEnabled()
    {
        var (sut, repository) = Build(TeamKeyEntity());

        var refreshed = await sut.RefreshKeyAsync(TeamKey, "k1");

        Assert.Null(refreshed.DisabledAt);
        Assert.Null(refreshed.DisabledBy);
    }

    /// <summary>The same rule for system keys; they refresh through a different builder.</summary>
    [Fact]
    public async Task RefreshingADisabledSystemKey_LeavesItDisabled()
    {
        var disabledAt = new DateTime(2026, 8, 2, 10, 0, 0, DateTimeKind.Utc);
        var (sut, _) = Build(new ApiKeyEntity
        {
            Key = "s1",
            Name = "Infra",
            TeamKey = null,
            ApiKeyHash = "hash",
            SystemScopes = ["monitor:read"],
            DisabledAt = disabledAt,
            DisabledBy = "alice"
        });

        var refreshed = await sut.RefreshSystemKeyAsync("s1");

        Assert.Equal(disabledAt, refreshed.DisabledAt);
        Assert.Equal("alice", refreshed.DisabledBy);
    }

    /// <summary>Disabling a key belonging to another team is refused, like every other key operation.</summary>
    [Fact]
    public async Task DisablingAnotherTeamsKey_IsRefused()
    {
        var (sut, _) = Build(TeamKeyEntity());

        await Assert.ThrowsAnyAsync<Exception>(
            () => sut.SetKeyDisabledAsync("other-team", "k1", disabled: true));
    }
}
