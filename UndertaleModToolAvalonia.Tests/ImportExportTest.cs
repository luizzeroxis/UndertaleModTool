using System;
using System.IO;
using System.Threading.Tasks;
using ImageMagick;
using UndertaleModLib.Models;
using UndertaleModLib.Util;

namespace UndertaleModToolAvalonia.Tests;

public class ImportExportTest
{
    [Fact]
    public async Task ImportEmbeddedAudio_ReadsNonSeekableStreams()
    {
        UndertaleEmbeddedAudio audio = new();
        byte[] data = [1, 2, 3, 4];

        using NonSeekableReadStream stream = new(data);

        await ImportExport.ImportEmbeddedAudio(audio, stream);

        Assert.Equal(data, audio.Data);
    }

    [Fact]
    public async Task ExportEmbeddedAudio_WritesEmptyDataWhenAudioIsNull()
    {
        UndertaleEmbeddedAudio audio = new();
        using MemoryStream stream = new();

        await ImportExport.ExportEmbeddedAudio(audio, stream);

        Assert.Empty(stream.ToArray());
    }

    [Fact]
    public async Task ImportTexturePageItemAsPNG_ReplacesAtlasRegionAndUpdatesTargetSize()
    {
        UndertaleEmbeddedTexture texture = new()
        {
            TextureData = new UndertaleEmbeddedTexture.TexData
            {
                Image = new GMImage(4, 4),
            },
        };
        UndertaleTexturePageItem item = new()
        {
            TexturePage = texture,
            SourceX = 1,
            SourceY = 1,
            SourceWidth = 2,
            SourceHeight = 2,
            TargetWidth = 1,
            TargetHeight = 1,
            BoundingWidth = 2,
            BoundingHeight = 2,
        };
        byte[] previousTextureData = texture.TextureData.Image.GetData();

        using MagickImage replacement = new(MagickColors.Red, 2, 2);
        replacement.Format = MagickFormat.Png32;
        using MemoryStream stream = new();
        replacement.Write(stream);
        stream.Position = 0;

        await ImportExport.ImportTexturePageItemAsPNG(item, stream);

        Assert.Equal(2, item.TargetWidth);
        Assert.Equal(2, item.TargetHeight);
        Assert.NotEqual(previousTextureData, texture.TextureData.Image.GetData());
    }

    private sealed class NonSeekableReadStream(byte[] data) : MemoryStream(data)
    {
        public override bool CanSeek => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin loc) => throw new NotSupportedException();
    }
}
