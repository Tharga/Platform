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

        await inner.Received(1).CreateKeyAsync("team-1", "My Key", AccessLevel.Custom, null, null, null, tags);
    }
}
