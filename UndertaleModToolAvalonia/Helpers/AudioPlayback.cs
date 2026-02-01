//#define USE_AUDIO_GROUPS

using Avalonia.Controls;
using NAudio.Wave;
using NLayer;
using NVorbis;
using OpenTK.Audio.OpenAL;
using System;
#if USE_AUDIO_GROUPS
using System.Collections.Generic;
#endif
using System.IO;
using System.Threading.Tasks;
using UndertaleModLib;
using UndertaleModLib.Models;
using Path = System.IO.Path;

namespace UndertaleModToolAvalonia;

public static class AudioPlayback
{
#if USE_AUDIO_GROUPS
    readonly static Dictionary<string, UndertaleData> AudioGroups = [];
#endif
    static Window _window = null!;
    static MainViewModel _mvm = null!;
    readonly static ALDevice Device = ALC.OpenDevice(null);
    readonly static ALContext Context = ALC.CreateContext(Device, [0]);
    static int _source;
    static int _buffer;

    public static void Initialize(Window window, MainViewModel mvm)
    {
        ALC.MakeContextCurrent(Context);
        _source = AL.GenSource();
        _buffer = AL.GenBuffer();
        _window = window;
        _mvm = mvm;
    }

    public static void ClearResources()
    {
        StopResource();
#if USE_AUDIO_GROUPS
        foreach (var ag in AudioGroups.Values)
            ag.Dispose();
        AudioGroups.Clear();
#endif
    }

    public static async Task PlayResource(UndertaleResource resource)
    {
        StopResource();
        Stream? target = null;
        try
        {
            if (resource is UndertaleSound snd)
            {
                if (snd.AudioFile != null)
                    target = new MemoryStream(snd.AudioFile.Data);
                else if ((snd.Flags & UndertaleSound.AudioEntryFlags.IsEmbedded) == 0)
                {
                    var path = Path.Combine(Path.GetDirectoryName(_mvm.DataPath) ?? "", snd.File.Content.Contains('.') ? snd.File.Content : (snd.File.Content + ".ogg"));
                    if (File.Exists(path))
                        target = File.OpenRead(path);
                    else
                        throw new("Failed to find the audio file.");
                }
                else if (snd.AudioID != -1 && snd.GroupID != 0)
                {
                    if (_mvm.DataPath == null)
                        throw new("Failed to find the game's data folder.");
#if USE_AUDIO_GROUPS
                    var path = Path.Combine(Path.GetDirectoryName(_mvm.DataPath) ?? "", snd.AudioGroup is { Path: not null } ? snd.AudioGroup.Path.Content : $"audiogroup{snd.GroupID}.dat");
                    if (File.Exists(path))
                    {
                        if (!AudioGroups.TryGetValue(path, out var data))
                        {
                            await using var stream = File.OpenRead(path);
                            data = await Task.Run(() => UndertaleIO.Read(stream, (warn, _) => throw new(warn)));
                            AudioGroups.Add(path, data);
                        }
                        target = new MemoryStream(data.EmbeddedAudio[snd.AudioID].Data);
                    }
                    else
                        throw new("Failed to find the audio group file.");
#else
                    throw new("Audio groups are unsupported as of this moment.");
#endif
                }
            }
            else if (resource is UndertaleEmbeddedAudio emb)
                target = new MemoryStream(emb.Data);
        }
        catch (Exception ex)
        {
            if (target != null)
            {
                await target.DisposeAsync();
                target = null;
            }
            await new MessageWindow($"Failed to play the audio.\n{ex.Message}", "Audio playback", true).ShowDialog(_window);
        }
        if (target != null)
        {
            try
            {
                var header = new byte[4];
                target.ReadAtLeast(header, 4);
                target.Position = 0;
                if ("RIFF"u8.SequenceEqual(header))
                {
                    var wav = new WaveFileReader(target);
                    var buf = new byte[wav.Length];
                    var read = wav.Read(buf, 0, buf.Length);
                    if (wav.WaveFormat.BitsPerSample == 16)
                    {
                        var shortBuf = new short[read / 2];
                        Buffer.BlockCopy(buf, 0, shortBuf, 0, read);
                        AL.BufferData(_buffer, wav.WaveFormat.Channels == 1 ? ALFormat.Mono16 : ALFormat.Stereo16, shortBuf, wav.WaveFormat.SampleRate);
                    }
                    else if (wav.WaveFormat.BitsPerSample != 8)
                        throw new("unsupported wav file");
                    else
                    {
                        AL.BufferData(_buffer, wav.WaveFormat.Channels == 1 ? ALFormat.Mono8 : ALFormat.Stereo8, buf[..read], wav.WaveFormat.SampleRate);
                    }
                    await wav.DisposeAsync();
                    AL.SourceQueueBuffer(_source, _buffer);
                    AL.SourcePlay(_source);
                }
                else
                {
                    float[] buf;
                    int read;
                    int channels;
                    int freq;
                    if ("OggS"u8.SequenceEqual(header))
                    {
                        var ogg = new VorbisReader(target);
                        buf = new float[ogg.TotalSamples];
                        read = ogg.ReadSamples(buf);
                        channels = ogg.Channels;
                        freq = ogg.SampleRate;
                        ogg.Dispose();
                    }
                    else if ("ID3"u8.SequenceEqual(header.AsSpan()[..3]))
                    {
                        var mp3 = new MpegFile(target);
                        buf = new float[mp3.Length];
                        read = mp3.ReadSamples(buf, 0, buf.Length);
                        channels = mp3.Channels;
                        freq = mp3.SampleRate;
                        mp3.Dispose();
                    }
                    else
                        throw new();
                    var shortBuf = new short[read];
                    for (var i = 0; i < read; i++)
                        shortBuf[i] = (short)Math.Clamp(buf[i] * (1 << 15), short.MinValue, short.MaxValue);
                    AL.BufferData(_buffer, channels == 1 ? ALFormat.Mono16 : ALFormat.Stereo16, shortBuf[..read], freq);
                    AL.SourceQueueBuffer(_source, _buffer);
                    AL.SourcePlay(_source);
                }
            }
            catch (Exception ex)
            {
                await new MessageWindow($"Failed to play the audio.\n{ex.Message}", "Audio playback", true).ShowDialog(_window);
            }
            await target.DisposeAsync();
        }
    }

    public static void StopResource()
    {
        AL.SourceStop(_source);
        AL.SourceUnqueueBuffer(_source);
    }
}