using Tharga.Team;

namespace Tharga.Team.Service.Tests;

public class IconCapabilityTests
{
    private record OptedIn : IUser
    {
        public string Key { get; init; }
        public string Identity { get; init; }
        public string EMail { get; init; }
        public string Icon { get; init; }
    }

    private record NotOptedIn : IUser
    {
        public string Key { get; init; }
        public string Identity { get; init; }
        public string EMail { get; init; }
    }

    [Fact]
    public void EntityDeclaringIcon_CanPersist()
    {
        Assert.True(IconCapability.CanPersistUserIcon(typeof(OptedIn)));
    }

    /// <summary>
    /// The decisive case. <see cref="IUser.Icon"/> is a default interface member returning null, so an
    /// entity that never opted in still compiles and still reads as null — which is exactly why the
    /// discarded write looked like success. Only the declared property makes it stick.
    /// </summary>
    [Fact]
    public void EntityRelyingOnTheDefaultInterfaceMember_CannotPersist()
    {
        Assert.False(IconCapability.CanPersistUserIcon(typeof(NotOptedIn)));
    }

    [Fact]
    public void NullType_CannotPersist()
    {
        Assert.False(IconCapability.CanPersistUserIcon(null));
    }

    /// <summary>The default processor returns the image untouched, so it cannot back a downscaling claim.</summary>
    [Fact]
    public void NoOpProcessor_CannotProcess()
    {
        Assert.False(IconCapability.CanProcessImages(new NoOpIconProcessor()));
    }

    [Fact]
    public void NoProcessor_CannotProcess()
    {
        Assert.False(IconCapability.CanProcessImages(null));
    }

    [Fact]
    public void RealProcessor_CanProcess()
    {
        Assert.True(IconCapability.CanProcessImages(new ResizingProcessor()));
    }

    private sealed class ResizingProcessor : IIconProcessor
    {
        public Task<IconContent> ProcessAsync(byte[] data, string contentType, CancellationToken cancellationToken = default)
            => Task.FromResult(new IconContent(data, contentType));
    }
}
