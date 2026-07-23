using Tharga.Team;

namespace Tharga.Team.Service.Tests;

/// <summary>
/// Core icon abstractions (`Tharga.Team`): validation limits, initials fallback, the built-in
/// stored-icon source, and the resolver's ordering/first-non-null-wins behavior.
/// </summary>
public class IconAbstractionsTests
{
    // ---- IconOptions defaults ----

    [Fact]
    public void IconOptions_Defaults()
    {
        var options = new IconOptions();
        Assert.Equal(256 * 1024, options.MaxBytes);
        Assert.Contains("image/png", options.AllowedContentTypes);
        Assert.Contains("image/svg+xml", options.AllowedContentTypes);
    }

    // ---- IconValidation ----

    [Fact]
    public void Validate_EmptyData_Invalid()
    {
        var result = IconValidation.Validate([], "image/png", new IconOptions());
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_Oversize_Invalid()
    {
        var options = new IconOptions { MaxBytes = 10 };
        var result = IconValidation.Validate(new byte[11], "image/png", options);
        Assert.False(result.IsValid);
        Assert.Contains("limit", result.Error);
    }

    [Fact]
    public void Validate_DisallowedContentType_Invalid()
    {
        var result = IconValidation.Validate([1, 2, 3], "application/pdf", new IconOptions());
        Assert.False(result.IsValid);
        Assert.Contains("not allowed", result.Error);
    }

    [Fact]
    public void Validate_MissingContentType_Invalid()
    {
        var result = IconValidation.Validate([1, 2, 3], "  ", new IconOptions());
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_AllowedTypeWithCharsetParameter_Valid()
    {
        var result = IconValidation.Validate([1, 2, 3], "image/png; charset=binary", new IconOptions());
        Assert.True(result.IsValid);
        Assert.Null(result.Error);
    }

    [Theory]
    [InlineData("IMAGE/PNG", "image/png")]
    [InlineData("image/jpeg; q=1", "image/jpeg")]
    [InlineData("  image/webp  ", "image/webp")]
    [InlineData(null, null)]
    public void NormalizeContentType(string input, string expected)
    {
        Assert.Equal(expected, IconValidation.NormalizeContentType(input));
    }

    // ---- IconInitials ----

    [Theory]
    [InlineData("Acme Corp", "AC")]
    [InlineData("Acme", "AC")]
    [InlineData("X", "X")]
    [InlineData("First Middle Last", "FL")]
    [InlineData("  ", "?")]
    [InlineData(null, "?")]
    [InlineData("acme corp", "AC")]
    public void Initials(string name, string expected)
    {
        Assert.Equal(expected, IconInitials.From(name));
    }

    // ---- StoredIconSource ----

    [Fact]
    public async Task StoredIconSource_WithReference_ReturnsEndpointUrl()
    {
        var subject = new IconSubject { Kind = IconKind.Team, Key = "T1", IconReference = "abc123" };
        var image = await new StoredIconSource().ResolveAsync(subject);
        Assert.NotNull(image);
        Assert.Equal(IconRoute.Url("abc123"), image.Url);
        Assert.StartsWith(IconRoute.Base, image.Url);
    }

    [Fact]
    public async Task StoredIconSource_NoReference_ReturnsNull()
    {
        var subject = new IconSubject { Kind = IconKind.Team, Key = "T1" };
        Assert.Null(await new StoredIconSource().ResolveAsync(subject));
    }

    // ---- IconResolver ----

    private sealed class FixedSource(IconImage image) : IIconSource
    {
        public Task<IconImage> ResolveAsync(IconSubject subject, CancellationToken cancellationToken = default)
            => Task.FromResult(image);
    }

    private static IconSubject Team(string key = "T1") => new() { Kind = IconKind.Team, Key = key, Name = "Team One" };

    [Fact]
    public async Task Resolver_NoSources_ReturnsNull()
    {
        var resolver = new IconResolver([]);
        Assert.Null(await resolver.ResolveAsync(Team()));
    }

    [Fact]
    public async Task Resolver_FirstNonNullWins_InOrder()
    {
        var resolver = new IconResolver(
        [
            new FixedSource(null),
            new FixedSource(new IconImage("https://custom/one")),
            new FixedSource(new IconImage("https://custom/two"))
        ]);

        var image = await resolver.ResolveAsync(Team());

        Assert.Equal("https://custom/one", image.Url);
    }

    [Fact]
    public async Task Resolver_AllDefer_ReturnsNull()
    {
        var resolver = new IconResolver([new FixedSource(null), new FixedSource(null)]);
        Assert.Null(await resolver.ResolveAsync(Team()));
    }

    [Fact]
    public async Task Resolver_NullSubject_ReturnsNull()
    {
        var resolver = new IconResolver([new FixedSource(new IconImage("https://x"))]);
        Assert.Null(await resolver.ResolveAsync(null));
    }

    [Fact]
    public async Task Resolver_StoredIconTakesPrecedenceOverCustom()
    {
        // Mirrors the platform's registration order: StoredIconSource first, custom sources after —
        // an explicitly-set (platform-stored) icon wins.
        var resolver = new IconResolver(
        [
            new StoredIconSource(),
            new FixedSource(new IconImage("https://custom/override"))
        ]);

        var subject = Team() with { IconReference = "stored-ref" };
        var image = await resolver.ResolveAsync(subject);

        Assert.Equal(IconRoute.Url("stored-ref"), image.Url);
    }

    [Fact]
    public async Task Resolver_NoStoredIcon_FallsToCustomSource()
    {
        // With no explicitly-set icon, a custom source fills in.
        var resolver = new IconResolver(
        [
            new StoredIconSource(),
            new FixedSource(new IconImage("https://custom/fill"))
        ]);

        var image = await resolver.ResolveAsync(Team());

        Assert.Equal("https://custom/fill", image.Url);
    }
}
