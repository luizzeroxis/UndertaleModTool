using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.Tests;

public class TexturePageItemViewModelTest
{
    [Fact]
    public async Task ImportImageTask_ReturnsFalseWithoutView()
    {
        UndertaleTexturePageItemViewModel vm = CreateViewModel(out _);

        bool result = await vm.ImportImageTask();

        Assert.False(result);
    }

    [Fact]
    public async Task ImportImageTask_ReturnsFalseWhenPickerIsCanceled()
    {
        UndertaleTexturePageItemViewModel vm = CreateViewModel(out MainViewModel mainVM);
        DialogView view = new();
        mainVM.View = view;

        bool result = await vm.ImportImageTask();

        Assert.False(result);
        Assert.NotNull(view.LastOpenFileOptions);
        Assert.Equal("Import PNG", view.LastOpenFileOptions.Title);
    }

    [Fact]
    public async Task ExportImageTask_ReturnsFalseWithoutView()
    {
        UndertaleTexturePageItemViewModel vm = CreateViewModel(out _);

        bool result = await vm.ExportImageTask();

        Assert.False(result);
    }

    [Fact]
    public async Task ExportImageTask_ReturnsFalseWhenPickerIsCanceled()
    {
        UndertaleTexturePageItemViewModel vm = CreateViewModel(out MainViewModel mainVM);
        DialogView view = new();
        mainVM.View = view;

        bool result = await vm.ExportImageTask();

        Assert.False(result);
        Assert.NotNull(view.LastSaveFileOptions);
        Assert.Equal("Export PNG", view.LastSaveFileOptions.Title);
    }

    private static UndertaleTexturePageItemViewModel CreateViewModel(out MainViewModel mainVM)
    {
        ServiceCollection services = new();
        services.AddSingleton<MainViewModel>();
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        mainVM = serviceProvider.GetRequiredService<MainViewModel>();
        mainVM.Initialize();

        UndertaleTexturePageItem item = new()
        {
            Name = new UndertaleString("PageItem 0"),
        };

        return new UndertaleTexturePageItemViewModel(item, serviceProvider);
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
