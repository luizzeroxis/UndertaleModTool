using System.Globalization;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public static class AudioMetadata
{
    public static string DescribeFormat(byte[]? data)
    {
        data ??= [];

        if (data.Length == 0)
            return "Empty";

        if (HasAscii(data, 0, "RIFF") && HasAscii(data, 8, "WAVE"))
            return "WAV";

        if (HasAscii(data, 0, "OggS"))
            return "Ogg";

        if (HasAscii(data, 0, "fLaC"))
            return "FLAC";

        if (HasAscii(data, 0, "ID3") || IsMp3Frame(data))
            return "MP3";

        if (HasAscii(data, 0, "FORM") && HasAscii(data, 8, "AIFF"))
            return "AIFF";

        return "Unknown";
    }

    public static string FormatByteCount(int count)
    {
        return $"{count.ToString("N0", CultureInfo.InvariantCulture)} bytes";
    }

    public static string DescribeEmbeddedAudio(UndertaleEmbeddedAudio? audio, UndertaleData? data)
    {
        if (audio is null)
            return "No embedded audio linked.";

        int index = data?.EmbeddedAudio?.IndexOf(audio) ?? -1;
        string idText = index >= 0 ? $"Embedded audio #{index}" : "Embedded audio";
        byte[] audioData = audio.Data ?? [];

        return $"{idText}; {DescribeFormat(audioData)}; {FormatByteCount(audioData.Length)}.";
    }

    static bool HasAscii(byte[] data, int offset, string value)
    {
        if (offset < 0 || data.Length < offset + value.Length)
            return false;

        for (int i = 0; i < value.Length; i++)
        {
            if (data[offset + i] != (byte)value[i])
                return false;
        }

        return true;
    }

    static bool IsMp3Frame(byte[] data)
    {
        return data.Length >= 2 && data[0] == 0xff && (data[1] & 0xe0) == 0xe0;
    }
}
