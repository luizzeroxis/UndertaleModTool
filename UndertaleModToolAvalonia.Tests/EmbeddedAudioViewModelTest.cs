using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.Tests;

public class EmbeddedAudioViewModelTest
{
    [Fact]
    public async Task PlayAudioTask_ReturnsFalseWithoutAudioData()
    {
        UndertaleEmbeddedAudioViewModel vm = CreateViewModel(out _);

        bool result = await vm.PlayAudioTask();

        Assert.False(result);
    }

    [Fact]
    public async Task ImportAudioTask_ReturnsFalseWithoutView()
    {
        UndertaleEmbeddedAudioViewModel vm = CreateViewModel(out _);

        bool result = await vm.ImportAudioTask();

        Assert.False(result);
    }

    [Fact]
    public async Task ImportAudioTask_ReturnsFalseWhenPickerIsCanceled()
    {
        UndertaleEmbeddedAudioViewModel vm = CreateViewModel(out MainViewModel mainVM);
        DialogView view = new();
        mainVM.View = view;

        bool result = await vm.ImportAudioTask();

        Assert.False(result);
        Assert.NotNull(view.LastOpenFileOptions);
        Assert.Equal("Import audio", view.LastOpenFileOptions.Title);
    }

    [Fact]
    public async Task ExportAudioTask_ReturnsFalseWithoutView()
    {
        UndertaleEmbeddedAudioViewModel vm = CreateViewModel(out _);

        bool result = await vm.ExportAudioTask();

        Assert.False(result);
    }

    [Fact]
    public async Task ExportAudioTask_ReturnsFalseWhenPickerIsCanceled()
    {
        UndertaleEmbeddedAudioViewModel vm = CreateViewModel(out MainViewModel mainVM);
        DialogView view = new();
        mainVM.View = view;

        bool result = await vm.ExportAudioTask();

        Assert.False(result);
        Assert.NotNull(view.LastSaveFileOptions);
        Assert.Equal("Export audio", view.LastSaveFileOptions.Title);
    }

    private static UndertaleEmbeddedAudioViewModel CreateViewModel(out MainViewModel mainVM)
    {
        ServiceCollection services = new();
        services.AddSingleton<MainViewModel>();
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        mainVM = serviceProvider.GetRequiredService<MainViewModel>();
        mainVM.Initialize();

        UndertaleEmbeddedAudio audio = new()
        {
            Name = new UndertaleString("audio_test"),
        };

        return new UndertaleEmbeddedAudioViewModel(audio, serviceProvider);
    }

    private sealed class DialogView : IView
    {
        public FilePickerOpenOptions? LastOpenFileOptions { get; private set; }
        public FilePickerSaveOptions? LastSaveFileOptions { get; private set; }

        public Task<MessageWindow.Result> MessageDialog(
            string message,
            string? title = null,
            MessageWindow.Buttons buttons = MessageWindow.Buttons.OK)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<IStorageFile>> OpenFileDialog(FilePickerOpenOptions options)
        {
            LastOpenFileOptions = options;
            return Task.FromResult<IReadOnlyList<IStorageFile>>([]);
        }

        public Task<IStorageFile?> SaveFileDialog(FilePickerSaveOptions options)
        {
            LastSaveFileOptions = options;
            return Task.FromResult<IStorageFile?>(null);
        }

        public Task<IReadOnlyList<IStorageFolder>> OpenFolderDialog(FolderPickerOpenOptions options)
        {
            throw new NotSupportedException();
        }

        public Task<bool> LaunchUriAsync(Uri uri)
        {
            throw new NotSupportedException();
        }

        public Task<string?> TextBoxDialog(string message, string text = "", string? title = null, bool isMultiline = false, bool isReadOnly = false)
        {
            throw new NotSupportedException();
        }

        public ILoaderWindow LoaderOpen()
        {
            throw new NotSupportedException();
        }

        public IInputElement? GetFocusedElement()
        {
            return null;
        }
    }
}
