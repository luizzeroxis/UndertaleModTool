using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PropertyChanged.SourceGenerator;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public partial class UndertaleSoundViewModel : IUndertaleResourceViewModel
{
    public MainViewModel MainVM;
    public UndertaleResource Resource => Sound;
    public UndertaleSound Sound { get; }

    [Notify]
    private bool _IsBuiltinAudioGroup;

    [Notify]
    private string _AudioRoutingSummary = "";

    [Notify]
    private string _AudioDataSummary = "";

    [Notify]
    private string _PlaybackSummary = "";

    AudioPlayer? audioPlayer = null;

    public UndertaleSoundViewModel(UndertaleSound sound, IServiceProvider serviceProvider)
    {
        MainVM = serviceProvider.GetRequiredService<MainViewModel>();

        Sound = sound;

        RefreshAudioDetails();
    }

    public void OnAttached()
    {
        Sound.PropertyChanged += OnSoundPropertyChanged;
        RefreshAudioDetails();
    }

    public void OnDetached()
    {
        Sound.PropertyChanged -= OnSoundPropertyChanged;
        StopAudio();
    }

    public async void PlayAudio()
    {
        await PlayAudioTask();
    }

    public async Task<bool> PlayAudioTask()
    {
        StopAudio();

        byte[] data = Sound.AudioFile?.Data ?? [];

        if (data.Length == 0)
            return false;

        try
        {
            audioPlayer = new(data);
            return true;
        }
        catch (Exception e)
        {
            if (MainVM.View is not null)
                await MainVM.View.MessageDialog($"Failed to play audio: {e.Message}");

            return false;
        }
    }

    public void StopAudio()
    {
        audioPlayer?.Stop();
        audioPlayer = null;
    }

    void OnSoundPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(UndertaleSound.AudioGroup) ||
            e.PropertyName == nameof(UndertaleSound.AudioFile) ||
            e.PropertyName == nameof(UndertaleSound.AudioID) ||
            e.PropertyName == nameof(UndertaleSound.GroupID))
        {
            RefreshAudioDetails();
        }
    }

    void UpdateIsBuiltinAudioGroup()
    {
        UndertaleData? data = MainVM.Data;
        int groupIndex = GetAudioGroupIndex();
        int builtinGroupId = data?.GetBuiltinSoundGroupID() ?? 0;

        IsBuiltinAudioGroup = Sound.AudioGroup is null || groupIndex == builtinGroupId || Sound.GroupID == builtinGroupId;
    }

    void RefreshAudioDetails()
    {
        UpdateIsBuiltinAudioGroup();

        AudioRoutingSummary = GetAudioRoutingSummary();
        AudioDataSummary = GetAudioDataSummary();
        PlaybackSummary = GetPlaybackSummary();
    }

    string GetAudioRoutingSummary()
    {
        UndertaleAudioGroup? group = Sound.AudioGroup;
        int groupIndex = GetAudioGroupIndex();
        string groupName = group?.Name?.Content ?? "(none)";
        string groupIdText = groupIndex >= 0 ? $"group #{groupIndex}" : $"group id {Sound.GroupID}";

        if (IsBuiltinAudioGroup)
            return $"{groupIdText} {groupName}; uses embedded audio stored in the data file.";

        string path = group?.Path?.Content ?? "";
        string pathText = string.IsNullOrWhiteSpace(path) ? "" : $" Path: {path}.";

        return $"{groupIdText} {groupName}; audio id {Sound.AudioID} inside the group file.{pathText}";
    }

    string GetAudioDataSummary()
    {
        if (IsBuiltinAudioGroup)
            return AudioMetadata.DescribeEmbeddedAudio(Sound.AudioFile, MainVM.Data);

        return $"External audio group entry id {Sound.AudioID}. Import/export is handled through the audio group data.";
    }

    string GetPlaybackSummary()
    {
        List<string> parts =
        [
            $"flags {DescribeFlags(Sound.Flags)}",
            $"volume {Sound.Volume.ToString("0.###", CultureInfo.InvariantCulture)}",
            $"pitch {Sound.Pitch.ToString("0.###", CultureInfo.InvariantCulture)}",
            $"preload {Sound.Preload}",
            $"effects {Sound.Effects}",
        ];

        if (Sound.AudioLength > 0)
            parts.Add($"length {Sound.AudioLength} us");

        return string.Join("; ", parts) + ".";
    }

    int GetAudioGroupIndex()
    {
        if (MainVM.Data?.AudioGroups is null || Sound.AudioGroup is null)
            return -1;

        return MainVM.Data.AudioGroups.IndexOf(Sound.AudioGroup);
    }

    static string DescribeFlags(UndertaleSound.AudioEntryFlags flags)
    {
        List<string> parts = [];

        if ((flags & UndertaleSound.AudioEntryFlags.IsEmbedded) != 0)
            parts.Add("embedded");

        if ((flags & UndertaleSound.AudioEntryFlags.IsCompressed) != 0)
            parts.Add("compressed");

        if ((flags & UndertaleSound.AudioEntryFlags.IsDecompressedOnLoad) == UndertaleSound.AudioEntryFlags.IsDecompressedOnLoad)
            parts.Add("decompress on load");

        if ((flags & UndertaleSound.AudioEntryFlags.Regular) == UndertaleSound.AudioEntryFlags.Regular)
            parts.Add("regular");

        string rawValue = ((uint)flags).ToString(CultureInfo.InvariantCulture);

        if (parts.Count == 0)
            return $"{flags} ({rawValue})";

        return $"{flags} ({rawValue}, {string.Join(", ", parts)})";
    }
}
