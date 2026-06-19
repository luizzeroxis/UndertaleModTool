using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using UndertaleModLib.Models;
using UndertaleModLib.Util;

namespace UndertaleModToolAvalonia.Tests;

public class EmbeddedTextureViewModelTest
{
    [Fact]
    public async Task ImportImageTask_ReturnsFalseWithoutView()
    {
        UndertaleEmbeddedTextureViewModel vm = CreateViewModel(out _);

        bool result = await vm.ImportImageTask();

        Assert.False(result);
    }

    [Fact]
    public async Task ImportImageTask_ReturnsFalseWhenPickerIsCanceled()
    {
        UndertaleEmbeddedTextureViewModel vm = CreateViewModel(out MainViewModel mainVM);
        DialogView view = new();
        mainVM.View = view;

        bool result = await vm.ImportImageTask();

        Assert.False(result);
        Assert.NotNull(view.LastOpenFileOptions);
        Assert.Equal("Import image", view.LastOpenFileOptions.Title);
    }

    [Fact]
    public async Task ExportImageTask_ReturnsFalseWithoutView()
    {
        UndertaleEmbeddedTextureViewModel vm = CreateViewModel(out _);

        bool result = await vm.ExportImageTask();

        Assert.False(result);
    }

    [Fact]
    public async Task ExportImageTask_ReturnsFalseWhenPickerIsCanceled()
    {
        UndertaleEmbeddedTextureViewModel vm = CreateViewModel(out MainViewModel mainVM);
        DialogView view = new();
        mainVM.View = view;

        bool result = await vm.ExportImageTask();

        Assert.False(result);
        Assert.NotNull(view.LastSaveFileOptions);
        Assert.Equal("Export image", view.LastSaveFileOptions.Title);
    }

    [Fact]
    public async Task ExportImageAsPNGTask_ReturnsFalseWithoutView()
    {
        UndertaleEmbeddedTextureViewModel vm = CreateViewModel(out _);

        bool result = await vm.ExportImageAsPNGTask();

        Assert.False(result);
    }

    [Fact]
    public async Task ExportImageAsPNGTask_ReturnsFalseWhenPickerIsCanceled()
    {
        UndertaleEmbeddedTextureViewModel vm = CreateViewModel(out MainViewModel mainVM);
        DialogView view = new();
        mainVM.View = view;

        bool result = await vm.ExportImageAsPNGTask();

        Assert.False(result);
        Assert.NotNull(view.LastSaveFileOptions);
        Assert.Equal("Export image as PNG", view.LastSaveFileOptions.Title);
    }

    private static UndertaleEmbeddedTextureViewModel CreateViewModel(out MainViewModel mainVM)
    {
        ServiceCollection services = new();
        services.AddSingleton<MainViewModel>();
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        mainVM = serviceProvider.GetRequiredService<MainViewModel>();
        mainVM.Initialize();

        UndertaleEmbeddedTexture texture = new()
        {
            Name = new UndertaleString("Texture 0"),
            TextureData = new UndertaleEmbeddedTexture.TexData
            {
                Image = new GMImage(1, 1),
            },
        };

        return new UndertaleEmbeddedTextureViewModel(texture, serviceProvider);
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
