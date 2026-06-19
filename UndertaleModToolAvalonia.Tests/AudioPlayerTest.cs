using System;

namespace UndertaleModToolAvalonia.Tests;

public class AudioPlayerTest
{
    [Fact]
    public void Constructor_RejectsEmptyAudioBeforeInitializingBackend()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new AudioPlayer([]));

        Assert.Equal("data", exception.ParamName);
    }
}
