# Tharga.Team.Images

Image processing for Tharga Team icons. Registers an `IIconProcessor` (backed by
[ImageSharp](https://github.com/SixLabors/ImageSharp)) that **automatically downscales** uploaded icons
larger than the configured maximum, instead of rejecting them.

## Registration

```csharp
builder.Services.AddThargaImageProcessing();
```

The built-in icon store runs the processor before validating/storing. Any uploaded image (team or user)
wider or taller than `IconOptions.MaxDimension` (default **256px**) is resized to fit within that box —
aspect ratio preserved, never upscaled — and re-encoded as PNG. Images already within the box, and
formats ImageSharp can't decode (e.g. SVG), pass through unchanged.

Configure the maximum via the platform options:

```csharp
builder.AddThargaTeam(o => o.Icon.MaxDimension = 256);
```
