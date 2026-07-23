using System.Linq.Expressions;
using Microsoft.Extensions.Options;
using Tharga.Team;

namespace Tharga.Team.MongoDB.Tests;

/// <summary>
/// <see cref="MongoIconStore"/> against a substituted collection: save validates + persists + returns a
/// reference (content type normalized), load round-trips or returns null, delete removes, and blank
/// references short-circuit without a query.
/// </summary>
public class MongoIconStoreTests
{
    private readonly IIconRepositoryCollection _collection = Substitute.For<IIconRepositoryCollection>();

    private MongoIconStore Build(IconOptions options = null)
        => new(_collection, Options.Create(options ?? new IconOptions()));

    [Fact]
    public async Task Save_ValidPng_InsertsAndReturnsReference()
    {
        IconEntity captured = null;
        await _collection.AddAsync(Arg.Do<IconEntity>(e => captured = e));

        var reference = await Build().SaveAsync(IconKind.Team, "T1", [1, 2, 3], "image/png");

        Assert.False(string.IsNullOrEmpty(reference));
        Assert.NotNull(captured);
        Assert.Equal(reference, captured.Key);
        Assert.Equal(IconKind.Team, captured.Kind);
        Assert.Equal("T1", captured.OwnerKey);
        Assert.Equal("image/png", captured.ContentType);
        Assert.Equal(3, captured.Size);
    }

    [Fact]
    public async Task Save_NormalizesContentType()
    {
        IconEntity captured = null;
        await _collection.AddAsync(Arg.Do<IconEntity>(e => captured = e));

        await Build().SaveAsync(IconKind.Team, "T1", [1], "IMAGE/PNG; charset=binary");

        Assert.Equal("image/png", captured.ContentType);
    }

    [Fact]
    public async Task Save_Oversize_ThrowsAndDoesNotInsert()
    {
        var store = Build(new IconOptions { MaxBytes = 2 });

        await Assert.ThrowsAsync<InvalidOperationException>(() => store.SaveAsync(IconKind.Team, "T1", [1, 2, 3], "image/png"));
        await _collection.DidNotReceiveWithAnyArgs().AddAsync(default);
    }

    [Fact]
    public async Task Save_DisallowedType_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => Build().SaveAsync(IconKind.Team, "T1", [1], "application/pdf"));
    }

    [Fact]
    public async Task Load_Existing_ReturnsContent()
    {
        _collection.GetOneAsync(Arg.Any<Expression<Func<IconEntity, bool>>>())
            .Returns(new IconEntity { Key = "r1", ContentType = "image/png", Data = [9, 8, 7] });

        var content = await Build().LoadAsync("r1");

        Assert.NotNull(content);
        Assert.Equal("image/png", content.ContentType);
        Assert.Equal([9, 8, 7], content.Data);
    }

    [Fact]
    public async Task Load_Missing_ReturnsNull()
    {
        _collection.GetOneAsync(Arg.Any<Expression<Func<IconEntity, bool>>>()).Returns((IconEntity)null);
        Assert.Null(await Build().LoadAsync("nope"));
    }

    [Fact]
    public async Task Load_BlankReference_ReturnsNullWithoutQuery()
    {
        Assert.Null(await Build().LoadAsync(""));
        await _collection.DidNotReceiveWithAnyArgs().GetOneAsync(default(Expression<Func<IconEntity, bool>>));
    }

    [Fact]
    public async Task Delete_CallsCollection()
    {
        await Build().DeleteAsync("r1");
        await _collection.ReceivedWithAnyArgs(1).DeleteOneAsync(default(Expression<Func<IconEntity, bool>>));
    }

    [Fact]
    public async Task Delete_BlankReference_NoOp()
    {
        await Build().DeleteAsync("");
        await _collection.DidNotReceiveWithAnyArgs().DeleteOneAsync(default(Expression<Func<IconEntity, bool>>));
    }
}
