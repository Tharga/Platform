using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Tharga.Team.MongoDB;

namespace Tharga.Team.MongoDB.Tests;

/// <summary>
/// What an <see cref="AccessLevel"/> means when nobody chose one.
/// </summary>
/// <remarks>
/// These tests document current behaviour rather than desired behaviour. They exist to prove the exposure
/// is real before changing anything, and to fail loudly if the fix is ever reverted.
/// </remarks>
public class AccessLevelDefaultTests
{
    private sealed record TestMember : TeamMemberBase;

    /// <summary>
    /// The root of it: <c>Owner</c> is the zero value, so "not set" and "the most powerful level" are the
    /// same bit pattern and cannot be told apart.
    /// </summary>
    [Fact]
    public void TheDefaultAccessLevel_IsOwner()
    {
        Assert.Equal(AccessLevel.Owner, default(AccessLevel));
        Assert.Equal(0, (int)AccessLevel.Owner);
    }

    /// <summary>
    /// A stored member document with no <c>AccessLevel</c> field — written before the field existed, or by
    /// any writer that omitted it — comes back as an Owner.
    /// </summary>
    [Fact]
    public void AStoredMemberMissingTheField_DeserializesAsOwner()
    {
        var document = new BsonDocument
        {
            { "Key", "member-1" },
            { "Name", "No level was ever stored for this member" }
        };

        var member = BsonSerializer.Deserialize<TestMember>(document);

        Assert.Equal(AccessLevel.Owner, member.AccessLevel);
    }

    /// <summary>
    /// The contrast that shows this is fixable: <c>State</c> sits directly above <c>AccessLevel</c> in the
    /// same record and is nullable, so its absence is preserved as "unknown" instead of being invented.
    /// </summary>
    [Fact]
    public void AStoredMemberMissingState_DeserializesAsNull()
    {
        var document = new BsonDocument { { "Key", "member-1" } };

        var member = BsonSerializer.Deserialize<TestMember>(document);

        Assert.Null(member.State);
    }

    /// <summary>
    /// The path a host actually hits. <c>CreateTeamMember</c> is abstract, so an override that forgets to
    /// copy <c>AccessLevel</c> from the invite does not fail to compile and does not throw — it produces
    /// an Owner.
    /// </summary>
    [Fact]
    public void AMemberBuiltWithoutChoosingALevel_IsAnOwner()
    {
        var invited = new TestMember { Key = "member-1", Name = "Invited as a Viewer" };

        Assert.Equal(AccessLevel.Owner, invited.AccessLevel);
    }

    /// <summary>
    /// Member levels are stored by name, so renumbering the enum would not re-grade a stored member.
    /// </summary>
    [Fact]
    public void AMemberLevel_IsStoredByName()
    {
        var document = new TestMember { Key = "member-1", AccessLevel = AccessLevel.Viewer }.ToBsonDocument();

        Assert.Equal(BsonType.String, document["AccessLevel"].BsonType);
        Assert.Equal("Viewer", document["AccessLevel"].AsString);
    }

    /// <summary>
    /// Consent is not. It carries no <c>BsonRepresentation</c>, so the driver stores the ordinal — which
    /// means renumbering the enum *would* silently re-grade every stored consent decision.
    /// </summary>
    /// <remarks>
    /// This is what makes the sentinel fix cost more than it appears: the three persisted
    /// <see cref="AccessLevel"/> fields do not agree on how they are written, and only one is safe to
    /// renumber. Asserting the current representation here means a fix cannot change it unnoticed.
    /// </remarks>
    [Fact]
    public void AConsentLevel_IsStoredByNumber()
    {
        var document = new TestTeam { Key = "team-1", Name = "B", ConsentAccessLevel = AccessLevel.Viewer }
            .ToBsonDocument();

        Assert.Equal(BsonType.Int32, document["ConsentAccessLevel"].BsonType);
        Assert.Equal((int)AccessLevel.Viewer, document["ConsentAccessLevel"].AsInt32);
    }

    private sealed record TestTeam : TeamEntityBase<TestMember>;
}
