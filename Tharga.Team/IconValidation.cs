namespace Tharga.Team;

/// <summary>
/// Outcome of validating an icon against <see cref="IconOptions"/>.
/// </summary>
/// <param name="IsValid">True when the icon is accepted.</param>
/// <param name="Error">A human-readable reason when rejected; null when valid.</param>
public sealed record IconValidationResult(bool IsValid, string Error)
{
    public static readonly IconValidationResult Valid = new(true, null);
    public static IconValidationResult Invalid(string error) => new(false, error);
}

/// <summary>
/// Shared size + content-type validation for icons, used by the store and by the operations that accept
/// an upload or a downloaded URL, so the same rules apply on every path.
/// </summary>
public static class IconValidation
{
    /// <summary>
    /// The bare, lower-cased media type — drops any parameters (e.g. <c>; charset=…</c>) and trims.
    /// Null/blank input returns null.
    /// </summary>
    public static string NormalizeContentType(string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType)) return null;
        var semicolon = contentType.IndexOf(';');
        var value = semicolon >= 0 ? contentType[..semicolon] : contentType;
        return value.Trim().ToLowerInvariant();
    }

    public static IconValidationResult Validate(byte[] data, string contentType, IconOptions options)
    {
        if (data == null || data.Length == 0)
            return IconValidationResult.Invalid("The image is empty.");

        if (options != null && data.Length > options.MaxBytes)
            return IconValidationResult.Invalid($"The image is {data.Length} bytes, exceeding the {options.MaxBytes}-byte limit.");

        var normalized = NormalizeContentType(contentType);
        if (normalized == null)
            return IconValidationResult.Invalid("The content type is missing.");

        if (options?.AllowedContentTypes is { Count: > 0 } allowed
            && !allowed.Any(t => NormalizeContentType(t) == normalized))
            return IconValidationResult.Invalid($"The content type '{normalized}' is not allowed.");

        return IconValidationResult.Valid;
    }
}
