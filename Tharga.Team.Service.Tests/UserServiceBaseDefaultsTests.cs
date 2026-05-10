using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Tharga.Team.Service.Tests;

public class UserServiceBaseDefaultsTests
{
    [Fact]
    public async Task SeedUserNameAsync_DefaultIsNoOp()
    {
        var sut = new StubUserService();
        var ex = await Record.ExceptionAsync(() => sut.SeedUserNameAsync("any-key", "any-name"));
        Assert.Null(ex);
    }

    [Fact]
    public async Task SetUserNameAsync_DefaultIsNoOp()
    {
        var sut = new StubUserService();
        var ex = await Record.ExceptionAsync(() => sut.SetUserNameAsync("any-key", "any-name"));
        Assert.Null(ex);
    }

    private sealed class StubUserService : UserServiceBase
    {
        public StubUserService() : base(authenticationStateProvider: null) { }
        protected override Task<IUser> GetUserAsync(ClaimsPrincipal claimsPrincipal) => throw new NotImplementedException();
        protected override IAsyncEnumerable<IUser> GetAllAsync() => throw new NotImplementedException();
    }
}
