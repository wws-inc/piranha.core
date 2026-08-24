using SkiaSharp;

namespace Piranha.SkiaSharp;

public class SkiaSharpProcessor : IImageProcessor
{
    public void GetSize(Stream stream, out int width, out int height)
    {
        if(stream.Length< 0) throw new NullReferenceException(nameof(stream)); 
        if(!stream.CanRead) throw new ArgumentException(nameof(stream));

        var clonedStream = CloneStream(stream);

        using var codec = SKCodec.Create(clonedStream);

        width = codec.Info.Width;
        height = codec.Info.Height;
    }

    public void GetSize(byte[] bytes, out int width, out int height)
    {
        using var data = SKData.CreateCopy(bytes);
        using var codec = SKCodec.Create(data);

        width = codec.Info.Width;
        height = codec.Info.Height;
    }

    public void Scale(Stream source, Stream dest, int width)
    {
        using var bitmap = SKBitmap.Decode(source);

        var ratio = (float)width / bitmap.Width;
        var height = (int)(bitmap.Height * ratio);

        using var resized = bitmap.Resize(
            new SKImageInfo(width, height),
            SKSamplingOptions.Default);

        SaveBitmap(resized, dest);
    }
    public void Crop(Stream source, Stream dest, int width, int height)
    {
        using var bitmap = SKBitmap.Decode(source);

        var x = Math.Max(0, (bitmap.Width - width) / 2);
        var y = Math.Max(0, (bitmap.Height - height) / 2);

        using var cropped = new SKBitmap();
        if (bitmap.ExtractSubset(cropped, SKRectI.Create(x, y, width, height)))
        {
            SaveBitmap(cropped, dest);
        }
    }

public void CropScale(Stream source, Stream dest, int width, int height)
{
    using var bitmap = SKBitmap.Decode(source);

    var scale = Math.Max(
        (float)width / bitmap.Width,
        (float)height / bitmap.Height);

    var scaledWidth = (int)(bitmap.Width * scale);
    var scaledHeight = (int)(bitmap.Height * scale);

    using var scaled = bitmap.Resize(
        new SKImageInfo(scaledWidth, scaledHeight),
        SKSamplingOptions.Default);

    var cropX = (scaledWidth - width) / 2;
    var cropY = (scaledHeight - height) / 2;

    using var result = new SKBitmap(width, height);

    using (var canvas = new SKCanvas(result))
    {
        var srcRect = SKRect.Create(cropX, cropY, width, height);
        var destRect = SKRect.Create(0, 0, width, height);

        // Pass SKSamplingOptions.Default to fix CS0618
        canvas.DrawBitmap(scaled, srcRect, destRect, SKSamplingOptions.Default);
    }

    SaveBitmap(result, dest);
}

    public void AutoOrient(Stream source, Stream dest)
    {
        using var codec = SKCodec.Create(source);

        source.Position = 0;

        using var bitmap = SKBitmap.Decode(source);

        var oriented = ApplyOrientation(bitmap, codec.EncodedOrigin);

        SaveBitmap(oriented, dest);
    }

    private static SKBitmap ApplyOrientation(
        SKBitmap bitmap,
        SKEncodedOrigin origin)
    {
        if (origin == SKEncodedOrigin.TopLeft)
        {
            return bitmap.Copy();
        }

        using var surface = SKSurface.Create(
            new SKImageInfo(bitmap.Width, bitmap.Height));

        var canvas = surface.Canvas;

        switch (origin)
        {
            case SKEncodedOrigin.RightTop:
                canvas.RotateDegrees(90);
                canvas.Translate(0, -bitmap.Height);
                break;

            case SKEncodedOrigin.BottomRight:
                canvas.RotateDegrees(180);
                canvas.Translate(-bitmap.Width, -bitmap.Height);
                break;

            case SKEncodedOrigin.LeftBottom:
                canvas.RotateDegrees(270);
                canvas.Translate(-bitmap.Width, 0);
                break;
        }

        canvas.DrawBitmap(bitmap, 0, 0);

        return SKBitmap.FromImage(surface.Snapshot());
    }

    private static void SaveBitmap(SKBitmap bitmap, Stream dest)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);

        data.SaveTo(dest);
    }

    public static Stream CloneStream(Stream source)
    {
        if (source.CanSeek)
            source.Position = 0;

        var clone = new MemoryStream();
        source.CopyTo(clone);

        if (source.CanSeek)
            source.Position = 0;

        clone.Position = 0;
        return clone;
    }
}