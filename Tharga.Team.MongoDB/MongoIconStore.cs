using Microsoft.Extensions.Options;

namespace Tharga.Team.MongoDB;

/// <summary>
/// Built-in <see cref="IIconStore"/> backed by MongoDB — the zero-configuration default (MongoDB is
/// already a platform dependency). Bytes live in their own <see cref="IconRepositoryCollection"/>, keyed
/// by a generated reference. Registered by <c>AddThargaTeamRepository</c> via <c>TryAdd</c>, so a
/// consumer-supplied <see cref="IIconStore"/> takes precedence.
/// </summary>
public class MongoIconStore : IIconStore
{
    private readonly IIconRepositoryCollection _collection;
    private readonly IconOptions _options;

    public MongoIconStore(IIconRepositoryCollection collection, IOptions<IconOptions> options = null)
    {
        _collection = collection;
        _options = options?.Value ?? new IconOptions();
    }

    public async Task<string> SaveAsync(IconKind kind, string ownerKey, byte[] data, string contentType, CancellationToken cancellationToken = default)
    {
        var validation = IconValidation.Validate(data, contentType, _options);
        if (!validation.IsValid)
            throw new InvalidOperationException(validation.Error);

        var entity = new IconEntity
        {
            Key = Guid.NewGuid().ToString("N"),
            Kind = kind,
            OwnerKey = ownerKey,
            ContentType = IconValidation.NormalizeContentType(contentType),
            Data = data,
            Size = data.Length,
            CreatedUtc = DateTime.UtcNow
        };

        await _collection.AddAsync(entity);
        return entity.Key;
    }

    public async Task<IconContent> LoadAsync(string reference, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(reference)) return null;

        var entity = await _collection.GetOneAsync(x => x.Key == reference);
        return entity == null ? null : new IconContent(entity.Data, entity.ContentType);
    }

    public async Task DeleteAsync(string reference, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(reference)) return;

        await _collection.DeleteOneAsync(x => x.Key == reference);
    }
}
