using UndertaleModToolAvalonia;

namespace UndertaleModToolAvalonia.Tests;

public class AudioMetadataTest
{
    [Fact]
    public void DescribeFormat_RecognizesKnownHeaders()
    {
        Assert.Equal("WAV", AudioMetadata.DescribeFormat([0x52, 0x49, 0x46, 0x46, 0, 0, 0, 0, 0x57, 0x41, 0x56, 0x45]));
        Assert.Equal("Ogg", AudioMetadata.DescribeFormat([0x4f, 0x67, 0x67, 0x53]));
        Assert.Equal("FLAC", AudioMetadata.DescribeFormat([0x66, 0x4c, 0x61, 0x43]));
        Assert.Equal("MP3", AudioMetadata.DescribeFormat([0x49, 0x44, 0x33]));
        Assert.Equal("MP3", AudioMetadata.DescribeFormat([0xff, 0xfb]));
        Assert.Equal("AIFF", AudioMetadata.DescribeFormat([0x46, 0x4f, 0x52, 0x4d, 0, 0, 0, 0, 0x41, 0x49, 0x46, 0x46]));
    }

    [Fact]
    public void DescribeFormat_HandlesEmptyAndUnknownData()
    {
        Assert.Equal("Empty", AudioMetadata.DescribeFormat([]));
        Assert.Equal("Unknown", AudioMetadata.DescribeFormat([1, 2, 3, 4]));
    }

    [Fact]
    public void FormatByteCount_UsesInvariantSeparators()
    {
        Assert.Equal("1,024 bytes", AudioMetadata.FormatByteCount(1024));
    }
}
