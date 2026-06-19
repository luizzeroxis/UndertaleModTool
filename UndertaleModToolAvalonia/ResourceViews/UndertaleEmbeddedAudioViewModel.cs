using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using PropertyChanged.SourceGenerator;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public partial class UndertaleEmbeddedAudioViewModel : IUndertaleResourceViewModel
{
    public MainViewModel MainVM;
    public UndertaleResource Resource => EmbeddedAudio;
    public UndertaleEmbeddedAudio EmbeddedAudio { get; }

    [Notify]
    private string _AudioFormat = "";

    [Notify]
    private string _AudioSize = "";

    [Notify]
    private string _LinkedSoundsSummary = "";

    AudioPlayer? audioPlayer = null;

    public UndertaleEmbeddedAudioViewModel(UndertaleEmbeddedAudio embeddedAudio, IServiceProvider serviceProvider)
    {
        MainVM = serviceProvider.GetRequiredService<MainViewModel>();

        EmbeddedAudio = embeddedAudio;
        RefreshAudioDetails();
    }

    public void OnDetached()
    {
        StopAudio();
    }

    public async void PlayAudio()
    {
        await PlayAudioTask();
    }

    public async Task<bool> PlayAudioTask()
    {
        StopAudio();

        byte[] data = EmbeddedAudio.Data ?? [];

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

    public async void ImportAudio()
    {
        await ImportAudioTask();
    }

    public async Task<bool> ImportAudioTask()
    {
        if (MainVM.View is not { } view)
            return false;

        IReadOnlyList<IStorageFile> files = await view.OpenFileDialog(new FilePickerOpenOptions
        {
            Title = "Import audio",
            FileTypeFilter = FilePickerFileTypes.WAV,
        });

        if (files.Count != 1)
            return false;

        using (Stream stream = await files[0].OpenReadAsync())
        {
            await ImportExport.ImportEmbeddedAudio(EmbeddedAudio, stream);
        }

        RefreshAudioDetails();
        return true;
    }

    public async void ExportAudio()
    {
        await ExportAudioTask();
    }

    public async Task<bool> ExportAudioTask()
    {
        if (MainVM.View is not { } view)
            return false;

        IStorageFile? file = await view.SaveFileDialog(new FilePickerSaveOptions()
        {
            Title = "Export audio",
            FileTypeChoices = FilePickerFileTypes.WAV,
            DefaultExtension = ".wav",
            SuggestedFileName = $"{EmbeddedAudio.Name.Content}.wav",
        });

        if (file is null)
            return false;

        using (Stream stream = await file.OpenWriteAsync())
        {
            await ImportExport.ExportEmbeddedAudio(EmbeddedAudio, stream);
        }

        return true;
    }

    void RefreshAudioDetails()
    {
        byte[] data = EmbeddedAudio.Data ?? [];

        AudioFormat = AudioMetadata.DescribeFormat(data);
        AudioSize = AudioMetadata.FormatByteCount(data.Length);
        LinkedSoundsSummary = GetLinkedSoundsSummary();
    }

    string GetLinkedSoundsSummary()
    {
        if (MainVM.Data?.Sounds is null)
            return "No data file loaded.";

        string[] linkedSounds = MainVM.Data.Sounds
            .Select((sound, index) => new { Sound = sound, Index = index })
            .Where(entry => entry.Sound.AudioFile == EmbeddedAudio)
            .Take(5)
            .Select(entry => $"#{entry.Index} {entry.Sound.Name?.Content ?? "(unnamed)"}")
            .ToArray();

        int linkedCount = MainVM.Data.Sounds.Count(sound => sound.AudioFile == EmbeddedAudio);

        if (linkedCount == 0)
            return "Not referenced by any sound entry.";

        string suffix = linkedCount > linkedSounds.Length ? $", +{linkedCount - linkedSounds.Length} more" : "";
        return $"Used by {linkedCount} sound(s): {string.Join(", ", linkedSounds)}{suffix}.";
    }
}
