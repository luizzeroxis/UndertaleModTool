using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Styling;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace UndertaleModToolAvalonia.Tests;

public class SettingsFileTest
{
    [Fact]
    public async Task SaveTask_WritesSettingsFile()
    {
        SettingsFile settings = CreateSettingsFile(out _);
        string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        string settingsPath = Path.Combine(directory, "Settings.json");

        try
        {
            bool result = await settings.SaveTask(settingsPath);

            Assert.True(result);
            Assert.True(File.Exists(settingsPath));
            Assert.Contains("\"Version\"", File.ReadAllText(settingsPath), StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SaveTask_QueuesErrorWhenSaveFailsWithoutView()
    {
        SettingsFile settings = CreateSettingsFile(out MainViewModel vm);
        string fileAsDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        File.WriteAllText(fileAsDirectory, "");

        try
        {
            bool result = await settings.SaveTask(Path.Combine(fileAsDirectory, "Settings.json"));

            Assert.False(result);
            Assert.Single(vm.LazyErrorMessages);
            Assert.StartsWith("Error when saving settings file:", vm.LazyErrorMessages[0], StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(fileAsDirectory);
        }
    }

    [Fact]
    public async Task SaveTask_ShowsErrorWhenSaveFailsWithView()
    {
        SettingsFile settings = CreateSettingsFile(out MainViewModel vm);
        DialogView view = new();
        vm.View = view;
        string fileAsDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        File.WriteAllText(fileAsDirectory, "");

        try
        {
            bool result = await settings.SaveTask(Path.Combine(fileAsDirectory, "Settings.json"));

            Assert.False(result);
            Assert.Empty(vm.LazyErrorMessages);
            Assert.StartsWith("Error when saving settings file:", view.LastMessage, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(fileAsDirectory);
        }
    }

    [Fact]
    public void TryApplyCustomStyles_ReturnsFalseWithoutApplication()
    {
        Styles styles = [];

        bool result = SettingsFile.TryApplyCustomStyles(styles, app: null);

        Assert.False(result);
    }

    private static SettingsFile CreateSettingsFile(out MainViewModel vm)
    {
        ServiceCollection services = new();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<SettingsFile>();
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        vm = serviceProvider.GetRequiredService<MainViewModel>();
        vm.Initialize();
        return serviceProvider.GetRequiredService<SettingsFile>();
    }

    private sealed class DialogView : IView
    {
        public string? LastMessage { get; private set; }

        public Task<MessageWindow.Result> MessageDialog(
            string message,
            string? title = null,
            MessageWindow.Buttons buttons = MessageWindow.Buttons.OK)
        {
            LastMessage = message;
            return Task.FromResult(MessageWindow.Result.OK);
        }

        public Task<IReadOnlyList<IStorageFile>> OpenFileDialog(FilePickerOpenOptions options)
        {
            throw new NotSupportedException();
        }

        public Task<IStorageFile?> SaveFileDialog(FilePickerSaveOptions options)
        {
            throw new NotSupportedException();
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
