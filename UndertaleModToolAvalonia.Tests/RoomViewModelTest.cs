using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.Tests;

public class RoomViewModelTest
{
    [Fact]
    public async Task SaveAsImageTask_ReturnsFalseWithoutView()
    {
        UndertaleRoomViewModel vm = await CreateViewModel();

        bool result = await vm.SaveAsImageTask();

        Assert.False(result);
    }

    [Fact]
    public async Task SaveAsImageTask_ReturnsFalseWhenPickerIsCanceled()
    {
        UndertaleRoomViewModel vm = await CreateViewModel();
        DialogView view = new();
        vm.MainVM.View = view;

        bool result = await vm.SaveAsImageTask();

        Assert.False(result);
        Assert.NotNull(view.LastSaveFileOptions);
        Assert.Equal("Save image", view.LastSaveFileOptions.Title);
    }

    private static async Task<UndertaleRoomViewModel> CreateViewModel()
    {
        ServiceCollection services = new();
        services.AddSingleton<MainViewModel>();
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        MainViewModel mainVM = serviceProvider.GetRequiredService<MainViewModel>();
        mainVM.Initialize();
        mainVM.Settings = new SettingsFile(serviceProvider);
        await mainVM.NewData();

        UndertaleRoom room = new()
        {
            Name = mainVM.Data!.Strings.MakeString("room_test", createNew: true),
        };
        mainVM.Data.Rooms.Add(room);

        return new UndertaleRoomViewModel(room, serviceProvider);
    }

    private sealed class DialogView : IView
    {
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
            throw new NotSupportedException();
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
