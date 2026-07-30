using Tharga.Team;
using Tharga.Team.Blazor.Features.User;

namespace Tharga.Team.Blazor.Tests;

public class DirectoryLinkTests
{
    private record OptedIn : IUser
    {
        public string Key { get; init; }
        public string Identity { get; init; }
        public string EMail { get; init; }
        public string DirectoryId { get; init; }
    }

    private record NotOptedIn : IUser
    {
        public string Key { get; init; }
        public string Identity { get; init; }
        public string EMail { get; init; }
    }

    [Fact]
    public void EntityDeclaringDirectoryId_IsStored()
    {
        Assert.True(DirectoryLink.IsStored(typeof(OptedIn)));
    }

    /// <summary>
    /// The decisive case: <see cref="IUser.DirectoryId"/> is a default interface member, so an entity that
    /// never opted in still *reads* as null rather than failing to compile. If this returned true the UI
    /// would tell an operator the user "has not been resolved yet" for a store that will never resolve one.
    /// </summary>
    [Fact]
    public void EntityRelyingOnTheDefaultInterfaceMember_IsNotStored()
    {
        Assert.False(DirectoryLink.IsStored(typeof(NotOptedIn)));
    }

    [Fact]
    public void NullType_IsNotStored()
    {
        Assert.False(DirectoryLink.IsStored(null));
    }

    [Theory]
    [InlineData(true, "Not resolved yet")]
    [InlineData(false, "Not stored")]
    public void AbsenceText_DistinguishesTheTwoReasons(bool isStored, string expected)
    {
        Assert.Equal(expected, DirectoryLink.AbsenceText(isStored));
    }

    [Fact]
    public void AbsenceHint_DiffersByReason()
    {
        Assert.NotEqual(DirectoryLink.AbsenceHint(true), DirectoryLink.AbsenceHint(false));
    }
}
