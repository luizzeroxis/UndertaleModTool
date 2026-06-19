using System;
using System.Reflection;
using System.Runtime.InteropServices;
using SDL3;

namespace UndertaleModToolAvalonia;

public class AudioPlayer : IDisposable
{
    static Action<Action> mainThreadAction = action => action();

    static IntPtr mixer = IntPtr.Zero;
    static bool mixerInitialized;

    IntPtr audio;
    IntPtr track;

    readonly Mixer.TrackStoppedCallback trackStoppedCallback;
    GCHandle trackStoppedCallbackHandle;

    public AudioPlayer(byte[] data)
    {
        if (data.Length == 0)
            throw new ArgumentException("No audio data was provided.", nameof(data));

        EnsureInitialized();

        // Don't allow this be deallocated until the sound stops.
        trackStoppedCallback = new(OnTrackStoppped);
        trackStoppedCallbackHandle = GCHandle.Alloc(trackStoppedCallback);

        GCHandle dataHandle = default;
        try
        {
            // Load audio
            dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);

            IntPtr io = SDL.IOFromConstMem(dataHandle.AddrOfPinnedObject(), (nuint)data.Length);
            if (io == IntPtr.Zero)
                throw new InvalidOperationException($"{SDL.GetError()}");

            audio = Mixer.LoadAudioIO(mixer, io, predecode: true, closeio: true);
            if (audio == IntPtr.Zero)
                throw new InvalidOperationException($"{SDL.GetError()}");

            // Create track and play
            track = Mixer.CreateTrack(mixer);
            if (track == IntPtr.Zero)
                throw new InvalidOperationException($"{SDL.GetError()}");

            if (!Mixer.SetTrackAudio(track, audio))
                throw new InvalidOperationException($"{SDL.GetError()}");

            if (!Mixer.PlayTrack(track, 0))
                throw new InvalidOperationException($"{SDL.GetError()}");

            if (!Mixer.SetTrackStoppedCallback(track, trackStoppedCallback, IntPtr.Zero))
                throw new InvalidOperationException($"{SDL.GetError()}");
        }
        catch
        {
            Dispose();
            throw;
        }
        finally
        {
            if (dataHandle.IsAllocated)
                dataHandle.Free();
        }
    }

    public static void Configure(Action<Action> _mainThreadAction)
    {
        mainThreadAction = _mainThreadAction;
    }

    static void EnsureInitialized()
    {
        if ((SDL.WasInit(SDL.InitFlags.Audio) & SDL.InitFlags.Audio) == 0)
        {
            SDL.SetHint(SDL.Hints.AppName, Assembly.GetExecutingAssembly().GetName().Name ?? "");

            if (!SDL.Init(SDL.InitFlags.Audio))
                throw new InvalidOperationException($"{SDL.GetError()}");

        }

        if (!mixerInitialized)
        {
            if (!Mixer.Init())
                throw new InvalidOperationException($"{SDL.GetError()}");

            mixerInitialized = true;
        }

        if (mixer == IntPtr.Zero)
        {
            mixer = Mixer.CreateMixerDevice(SDL.AudioDeviceDefaultPlayback, IntPtr.Zero);
            if (mixer == IntPtr.Zero)
                throw new InvalidOperationException($"{SDL.GetError()}");
        }
    }

    public void Stop()
    {
        Dispose();
    }

    public void Dispose()
    {
        // If those are null, nothing happens. They also don't call the track stopped callback.
        Mixer.DestroyTrack(track);
        Mixer.DestroyAudio(audio);

        if (trackStoppedCallbackHandle.IsAllocated)
            trackStoppedCallbackHandle.Free();

        track = IntPtr.Zero;
        audio = IntPtr.Zero;

        GC.SuppressFinalize(this);
    }

    void OnTrackStoppped(IntPtr userdata, IntPtr track)
    {
        // The callback happens in a separate thread, so we defer to the main thread.
        mainThreadAction(() =>
        {
            Dispose();
        });
    }
}
