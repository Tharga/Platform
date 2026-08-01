# Tharga.Team.Images

Image processing for Tharga Team icons. Registers an `IIconProcessor` (backed by
[ImageSharp](https://github.com/SixLabors/ImageSharp)) that **squares and downscales** uploaded icons,
instead of rejecting ones larger than the configured maximum.

## Registration

```csharp
builder.Services.AddThargaImageProcessing();
```

The built-in icon store runs the processor before validating/storing. Any uploaded image (team or user)
is fitted within `IconOptions.MaxDimension` (default **256px**), **squared by padding the short side
with transparency**, and re-encoded as PNG. Formats ImageSharp can't decode (e.g. SVG) pass through
unchanged, as do images that are already square and within the box.

**Content is never cropped and never upscaled.** The output side is `min(max(width, height), MaxDimension)`:

| Source | Output | Why |
|---|---|---|
| 1000×500 | 256×256 | scaled to 256×128, then padded |
| 100×50 | 100×100 | already fits, so padded only — no pixel is invented |
| 300×300 | 256×256 | square, but larger than the box |
| 100×100 | unchanged | square and within bounds |

Squaring exists so avatar surfaces that reserve a square box stop letterboxing wide and tall sources
inconsistently. Cropping would square them too, and is exactly what to avoid — it takes a face out of a
portrait photo.

> **Behaviour change.** Before this, output preserved the source aspect ratio, so a 1000×500 upload was
> stored as 256×128. New uploads are now squared. **Already-stored icons are not reprocessed** — only
> images uploaded from here on change shape.

Configure the maximum via the platform options:

```csharp
builder.AddThargaTeam(o => o.Icon.MaxDimension = 256);
```
