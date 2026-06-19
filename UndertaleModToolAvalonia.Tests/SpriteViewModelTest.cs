using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.Tests;

public class SpriteViewModelTest
{
    [Fact]
    public async Task ExportAllTexturesAsPNGsTask_ReturnsFalseWithoutView()
    {
        UndertaleSpriteViewModel vm = CreateViewModel(out _);

        bool result = await vm.ExportAllTexturesAsPNGsTask();

        Assert.False(result);
    }

    [Fact]
    public async Task ExportAllTexturesAsPNGsTask_ReturnsFalseWhenFolderPickerIsCanceled()
    {
        UndertaleSpriteViewModel vm = CreateViewModel(out MainViewModel mainVM);
        DialogView view = new();
        mainVM.View = view;

        bool result = await vm.ExportAllTexturesAsPNGsTask();

        Assert.False(result);
        Assert.NotNull(view.LastOpenFolderOptions);
        Assert.Equal("Export all textures into folder", view.LastOpenFolderOptions.Title);
    }

    [Fact]
    public async Task ImportCollisionMaskDataTask_ReturnsFalseWithoutView()
    {
        UndertaleSpriteViewModel vm = CreateViewModel(out _);

        bool result = await vm.ImportCollisionMaskDataTask();

        Assert.False(result);
    }

    [Fact]
    public async Task ImportCollisionMaskDataTask_ReturnsFalseWithoutSelectedMask()
    {
        UndertaleSpriteViewModel vm = CreateViewModel(out _, includeMask: false);

        bool result = await vm.ImportCollisionMaskDataTask();

        Assert.False(result);
    }

    [Fact]
    public async Task ImportCollisionMaskDataTask_ReturnsFalseWhenPickerIsCanceled()
    {
        UndertaleSpriteViewModel vm = CreateViewModel(out MainViewModel mainVM);
        DialogView view = new();
        mainVM.View = view;

        bool result = await vm.ImportCollisionMaskDataTask();

        Assert.False(result);
        Assert.NotNull(view.LastOpenFileOptions);
        Assert.Equal("Import collision mask data", view.LastOpenFileOptions.Title);
    }

    [Fact]
    public async Task ExportCollisionMaskDataTask_ReturnsFalseWithoutView()
    {
        UndertaleSpriteViewModel vm = CreateViewModel(out _);

        bool result = await vm.ExportCollisionMaskDataTask();

        Assert.False(result);
    }

    [Fact]
    public async Task ExportCollisionMaskDataTask_ReturnsFalseWithoutSelectedMask()
    {
        UndertaleSpriteViewModel vm = CreateViewModel(out _, includeMask: false);

        bool result = await vm.ExportCollisionMaskDataTask();

        Assert.False(result);
    }

    [Fact]
    public async Task ExportCollisionMaskDataTask_ReturnsFalseWhenPickerIsCanceled()
    {
        UndertaleSpriteViewModel vm = CreateViewModel(out MainViewModel mainVM);
        DialogView view = new();
        mainVM.View = view;

        bool result = await vm.ExportCollisionMaskDataTask();

        Assert.False(result);
        Assert.NotNull(view.LastSaveFileOptions);
        Assert.Equal("Export collision mask data", view.LastSaveFileOptions.Title);
    }

    private static UndertaleSpriteViewModel CreateViewModel(out MainViewModel mainVM, bool includeMask = true)
    {
        ServiceCollection services = new();
        services.AddSingleton<MainViewModel>();
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        mainVM = serviceProvider.GetRequiredService<MainViewModel>();
        mainVM.Initialize();

        UndertaleSprite sprite = new()
        {
            Name = new UndertaleString("spr_test"),
        };

        if (includeMask)
            sprite.CollisionMasks.Add(new UndertaleSprite.MaskEntry([0], 1, 1));

        return new UndertaleSpriteViewModel(sprite, serviceProvider);
    }

    private sealed class DialogView : IView
    {
        public FilePickerOpenOptions? LastOpenFileOptions { get; private set; }
        public FilePickerSaveOptions? LastSaveFileOptions { get; private set; }
        public FolderPickerOpenOptions? LastOpenFolderOptions { get; private set; }

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
            LastOpenFolderOptions = options;
            return Task.FromResult<IReadOnlyList<IStorageFolder>>([]);
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
