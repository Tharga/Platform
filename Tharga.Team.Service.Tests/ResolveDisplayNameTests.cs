using NSubstitute;

namespace Tharga.Team.Service.Tests;

public class ResolveDisplayNameTests
{
    [Fact]
    public void NullUser_ReturnsUnknown()
    {
        Assert.Equal("Unknown", TeamServiceBase.ResolveDisplayName(null));
    }

    [Fact]
    public void NullEmail_ReturnsUnknown()
    {
        var user = Substitute.For<IUser>();
        user.EMail.Returns((string)null);
        Assert.Equal("Unknown", TeamServiceBase.ResolveDisplayName(user));
    }

    [Fact]
    public void EmptyEmail_ReturnsUnknown()
    {
        var user = Substitute.For<IUser>();
        user.EMail.Returns("");
        Assert.Equal("Unknown", TeamServiceBase.ResolveDisplayName(user));
    }

    [Fact]
    public void WithName_PrefersNameOverEmail()
    {
        var user = Substitute.For<IUser>();
        user.Name.Returns("Daniel Bohlin");
        user.EMail.Returns("daniel.bohlin@example.com");
        Assert.Equal("Daniel Bohlin", TeamServiceBase.ResolveDisplayName(user));
    }

    [Fact]
    public void StandardEmail_ReturnsUsernameWithDotsReplaced()
    {
        var user = Substitute.For<IUser>();
        user.EMail.Returns("john.doe@example.com");
        Assert.Equal("John Doe", TeamServiceBase.ResolveDisplayName(user));
    }

    [Fact]
    public void EmailWithoutDots_ReturnsUsername()
    {
        var user = Substitute.For<IUser>();
        user.EMail.Returns("admin@example.com");
        Assert.Equal("Admin", TeamServiceBase.ResolveDisplayName(user));
    }

    [Fact]
    public void EmailWithoutAtSign_ReturnsFullValue()
    {
        var user = Substitute.For<IUser>();
        user.EMail.Returns("localuser");
        Assert.Equal("Localuser", TeamServiceBase.ResolveDisplayName(user));
    }
}
