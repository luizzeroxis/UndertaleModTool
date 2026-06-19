using System;
using System.IO;
using System.Threading.Tasks;
using ImageMagick;
using SkiaSharp;
using UndertaleModLib.Models;
using UndertaleModLib.Util;

namespace UndertaleModToolAvalonia;

public static class ImportExport
{
    public static async Task ImportEmbeddedAudio(UndertaleEmbeddedAudio embeddedAudio, Stream stream)
    {
        embeddedAudio.Data = await ReadAllBytesAsync(stream);
    }

    public static async Task ExportEmbeddedAudio(UndertaleEmbeddedAudio embeddedAudio, Stream stream)
    {
        await stream.WriteAsync(embeddedAudio.Data ?? []);
    }

    public static async Task ImportEmbeddedTexture(UndertaleEmbeddedTexture embeddedTexture, Stream stream)
    {
        byte[] bytes = await ReadAllBytesAsync(stream);

        GMImage gmImage = GMImage.FromPng(bytes, verifyHeader: true);
        gmImage.ConvertToFormat(embeddedTexture.TextureData.Image.Format);

        embeddedTexture.TextureData.Image = gmImage;
        embeddedTexture.TextureWidth = gmImage.Width;
        embeddedTexture.TextureHeight = gmImage.Height;
    }

    public static async Task ExportEmbeddedTexture(UndertaleEmbeddedTexture embeddedTexture, Stream stream)
    {
        await stream.WriteAsync(embeddedTexture.TextureData.Image.GetData());
    }

    public static Task ExportEmbeddedTextureAsPNG(UndertaleEmbeddedTexture embeddedTexture, Stream stream)
    {
        embeddedTexture.TextureData.Image.SavePng(stream);
        return Task.CompletedTask;
    }

    public static Task ImportTexturePageItemAsPNG(UndertaleTexturePageItem texturePageItem, Stream stream)
    {
        MagickReadSettings settings = new()
        {
            ColorSpace = ColorSpace.sRGB,
        };
        using MagickImage image = new(stream, settings);
        image.Alpha(AlphaOption.Set);
        image.Format = MagickFormat.Bgra;
        image.Depth = 8;
        image.SetCompression(CompressionMethod.NoCompression);

        texturePageItem.ReplaceTexture(image);
        return Task.CompletedTask;
    }

    public static Task ExportRoomAsPNG(UndertaleRoom room, Stream stream)
    {
        // NOTE: This is a CPU bitmap, unlike the GPU surface used when rendering in the UI.
        SKBitmap bitmap = new((int)room.Width, (int)room.Height, SKColorType.Bgra8888, SKAlphaType.Unpremul);
        SKCanvas canvas = new(bitmap);

        RoomRenderer renderer = new();
        renderer.RenderCommands(new RoomRenderer.RenderCommandsBuilder(room).RenderCommands, canvas);

        bool result = bitmap.Encode(stream, SKEncodedImageFormat.Png, 100);
        if (!result)
            throw new InvalidOperationException("Failed to encode room preview as PNG.");

        return Task.CompletedTask;
    }

    public static async Task ImportSpriteCollisionMaskData(UndertaleSprite sprite, int collisionMaskIndex, Stream stream, MainViewModel mainVM)
    {
        byte[] bytes = await ReadAllBytesAsync(stream);

        (int width, int height) = sprite.CalculateMaskDimensions(mainVM.Data);
        UndertaleSprite.MaskEntry maskEntry = new(bytes, width, height);

        sprite.CollisionMasks[collisionMaskIndex] = maskEntry;
    }

    public static async Task ExportSpriteCollisionMaskData(UndertaleSprite sprite, int collisionMaskIndex, Stream stream)
    {
        await stream.WriteAsync(sprite.CollisionMasks[collisionMaskIndex].Data);
    }

    public static Task ExportTexturePageItemAsPNG(UndertaleTexturePageItem texturePageItem, Stream stream, MainViewModel mainVM)
    {
        SKBitmap bitmap = new(texturePageItem.BoundingWidth, texturePageItem.BoundingHeight, SKColorType.Bgra8888, SKAlphaType.Unpremul);
        SKCanvas canvas = new(bitmap);

        SKImage? image = mainVM.ImageCache.GetCachedImageFromTexturePageItem(texturePageItem);

        if (image is null)
            throw new InvalidOperationException("Texture page item image is not available in the cache.");

        canvas.DrawImage(image, SKRect.Create(texturePageItem.TargetX, texturePageItem.TargetY, texturePageItem.TargetWidth, texturePageItem.TargetHeight));

        bool result = bitmap.Encode(stream, SKEncodedImageFormat.Png, 100);
        if (!result)
            throw new InvalidOperationException("Failed to encode texture page item as PNG.");

        return Task.CompletedTask;
    }

    private static async Task<byte[]> ReadAllBytesAsync(Stream stream)
    {
        if (stream is MemoryStream memoryStream && memoryStream.TryGetBuffer(out ArraySegment<byte> buffer))
            return buffer.ToArray();

        using MemoryStream output = new();
        await stream.CopyToAsync(output);
        return output.ToArray();
    }
}
