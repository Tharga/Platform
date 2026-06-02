using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Tharga.Team;

namespace Tharga.Team.Service.Tests;

public class ApiKeyManagementServiceTests
{
    [Fact]
    public async Task CreateKeyAsync_Forwards_Tags_To_Inner()
    {
        var inner = Substitute.For<IApiKeyAdministrationService>();
        var sut = new ApiKeyManagementService(inner);
        var tags = new[] { new Tag("Type", "firewall") };

        await sut.CreateKeyAsync("team-1", "My Key", AccessLevel.Custom, tags: tags);

        await inner.Received(1).CreateKeyAsync("team-1", "My Key", AccessLevel.Custom, null, null, null, tags, null);
    }

    [Fact]
    public async Task CreateKeyAsync_Forwards_CurrentUser_As_CreatedBy()
    {
        var inner = Substitute.For<IApiKeyAdministrationService>();
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "alice@example.com") })),
        });
        var sut = new ApiKeyManagementService(inner, accessor);

        await sut.CreateKeyAsync("team-1", "My Key", AccessLevel.User);

        await inner.Received(1).CreateKeyAsync("team-1", "My Key", AccessLevel.User, null, null, null, null, "alice@example.com");
    }
}
