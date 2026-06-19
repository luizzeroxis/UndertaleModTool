using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using UndertaleModLib;
using UndertaleModLib.Project;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.Tests;

public class MainViewModelTest
{
    [Fact]
    public async Task FileNewTask_CreatesNewDataWhenNoSavePromptIsNeeded()
    {
        MainViewModel vm = CreateViewModel();

        bool result = await vm.FileNewTask();

        Assert.True(result);
        Assert.NotNull(vm.Data);
        Assert.Null(vm.DataPath);
    }

    [Fact]
    public async Task FileCloseTask_ClearsDataWhenNoSavePromptIsNeeded()
    {
        MainViewModel vm = CreateViewModel();
        vm.View = new DialogAnswerView(MessageWindow.Result.No);
        await vm.NewData();

        bool result = await vm.FileCloseTask();

        Assert.True(result);
        Assert.Null(vm.Data);
        Assert.Null(vm.DataPath);
    }

    [Fact]
    public async Task FileOpenTask_ReturnsFalseWithoutView()
    {
        MainViewModel vm = CreateViewModel();

        bool result = await vm.FileOpenTask();

        Assert.False(result);
    }

    [Fact]
    public async Task FileOpenTask_ReturnsFalseWhenPickerIsCanceled()
    {
        MainViewModel vm = CreateViewModel();
        DialogAnswerView view = new(MessageWindow.Result.No);
        vm.View = view;

        bool result = await vm.FileOpenTask();

        Assert.False(result);
        Assert.NotNull(view.LastOpenFileOptions);
        Assert.Equal("Open data file", view.LastOpenFileOptions.Title);
    }

    [Fact]
    public async Task FileSaveTask_ReturnsFalseWithoutView()
    {
        MainViewModel vm = CreateViewModel();
        await vm.NewData();

        bool result = await vm.FileSaveTask();

        Assert.False(result);
    }

    [Fact]
    public async Task FileSaveTask_ReturnsFalseWhenPickerIsCanceled()
    {
        MainViewModel vm = CreateViewModel();
        DialogAnswerView view = new(MessageWindow.Result.No);
        vm.View = view;
        await vm.NewData();

        bool result = await vm.FileSaveTask();

        Assert.False(result);
        Assert.NotNull(view.LastSaveFileOptions);
        Assert.Equal("Save data file", view.LastSaveFileOptions.Title);
    }

    [Fact]
    public async Task FileRunTask_ReturnsFalseWithoutData()
    {
        MainViewModel vm = CreateViewModel();

        bool result = await vm.FileRunTask();

        Assert.False(result);
    }

    [Fact]
    public async Task FileRunTask_ReturnsFalseWithoutView()
    {
        MainViewModel vm = CreateViewModel();
        await vm.NewData();

        bool result = await vm.FileRunTask();

        Assert.False(result);
    }

    [Fact]
    public async Task FileRunTask_ReturnsFalseWhenRunnerPickerIsCanceled()
    {
        MainViewModel vm = CreateViewModel();
        DialogAnswerView view = new(MessageWindow.Result.No);
        vm.View = view;
        await vm.NewData();

        string dataPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.win");
        try
        {
            File.WriteAllBytes(dataPath, []);
            vm.DataPath = dataPath;

            bool result = await vm.FileRunTask();

            Assert.False(result);
            Assert.NotNull(view.LastOpenFileOptions);
            Assert.Equal("Open runner", view.LastOpenFileOptions.Title);
        }
        finally
        {
            File.Delete(dataPath);
        }
    }

    [Fact]
    public async Task OpenDroppedFilesTask_ReturnsFalseForNullFiles()
    {
        MainViewModel vm = CreateViewModel();

        bool result = await vm.OpenDroppedFilesTask(null);

        Assert.False(result);
    }

    [Fact]
    public async Task OpenDroppedFilesTask_ReturnsFalseForEmptyFiles()
    {
        MainViewModel vm = CreateViewModel();

        bool result = await vm.OpenDroppedFilesTask([]);

        Assert.False(result);
    }

    [Fact]
    public async Task ScriptsRunOtherScriptTask_ReturnsFalseWithoutView()
    {
        MainViewModel vm = CreateViewModel();

        bool result = await vm.ScriptsRunOtherScriptTask();

        Assert.False(result);
    }

    [Fact]
    public async Task ScriptsRunOtherScriptTask_ReturnsFalseWhenPickerIsCanceled()
    {
        MainViewModel vm = CreateViewModel();
        DialogAnswerView view = new(MessageWindow.Result.No);
        vm.View = view;

        bool result = await vm.ScriptsRunOtherScriptTask();

        Assert.False(result);
        Assert.NotNull(view.LastOpenFileOptions);
        Assert.Equal("Run script", view.LastOpenFileOptions.Title);
    }

    [Fact]
    public async Task RunScriptFileAsync_ReturnsFalseWithoutViewWhenFileIsMissing()
    {
        MainViewModel vm = CreateViewModel();
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.csx");

        bool result = await vm.RunScriptFileAsync(path);

        Assert.False(result);
    }

    [Fact]
    public async Task RunScriptFileAsync_ShowsMessageWhenFileIsMissing()
    {
        MainViewModel vm = CreateViewModel();
        DialogAnswerView view = new(MessageWindow.Result.OK);
        vm.View = view;
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.csx");

        bool result = await vm.RunScriptFileAsync(path);

        Assert.False(result);
        Assert.Equal("The script file doesn't exist.", view.LastMessage);
    }

    [Fact]
    public async Task OnLoadedTask_ReturnsFalseWithoutView()
    {
        MainViewModel vm = CreateViewModel();
        vm.LazyErrorMessages.Add("queued startup error");

        bool result = await vm.OnLoadedTask();

        Assert.False(result);
        Assert.Single(vm.LazyErrorMessages);
    }

    [Fact]
    public async Task OnLoadedTask_ShowsAndClearsLazyErrors()
    {
        MainViewModel vm = CreateViewModel();
        DialogAnswerView view = new(MessageWindow.Result.OK);
        vm.View = view;
        vm.LazyErrorMessages.Add("first startup error");
        vm.LazyErrorMessages.Add("second startup error");

        bool result = await vm.OnLoadedTask();

        Assert.True(result);
        Assert.Empty(vm.LazyErrorMessages);
        Assert.Equal(["first startup error", "second startup error"], view.Messages);
    }

    [Fact]
    public async Task HelpGitHubTask_ReturnsFalseWithoutView()
    {
        MainViewModel vm = CreateViewModel();

        bool result = await vm.HelpGitHubTask();

        Assert.False(result);
    }

    [Fact]
    public async Task HelpGitHubTask_LaunchesRepositoryUri()
    {
        MainViewModel vm = CreateViewModel();
        DialogAnswerView view = new(MessageWindow.Result.OK);
        vm.View = view;

        bool result = await vm.HelpGitHubTask();

        Assert.True(result);
        Assert.Equal(new Uri("https://github.com/UnderminersTeam/UndertaleModTool"), view.LaunchedUri);
    }

    [Fact]
    public async Task HelpAboutTask_ReturnsFalseWithoutView()
    {
        MainViewModel vm = CreateViewModel();

        bool result = await vm.HelpAboutTask();

        Assert.False(result);
    }

    [Fact]
    public async Task HelpAboutTask_ShowsAboutDialog()
    {
        MainViewModel vm = CreateViewModel();
        DialogAnswerView view = new(MessageWindow.Result.OK);
        vm.View = view;

        bool result = await vm.HelpAboutTask();

        Assert.True(result);
        Assert.Equal("About", view.LastMessageTitle);
        Assert.StartsWith("UndertaleModTool v", view.LastMessage);
        Assert.Contains("Licensed under the GNU General Public License Version 3.", view.LastMessage);
    }

    [Fact]
    public async Task AskFileSave_ReturnsTrueWithoutData()
    {
        MainViewModel vm = CreateViewModel();

        bool result = await vm.AskFileSave("save?");

        Assert.True(result);
    }

    [Fact]
    public async Task AskFileSave_ReturnsFalseWithoutViewWhenDataIsLoaded()
    {
        MainViewModel vm = CreateViewModel();
        await vm.NewData();

        bool result = await vm.AskFileSave("save?");

        Assert.False(result);
    }

    [Fact]
    public async Task AskFileSave_ReturnsTrueWhenUserDeclinesSave()
    {
        MainViewModel vm = CreateViewModel();
        vm.View = new DialogAnswerView(MessageWindow.Result.No);
        await vm.NewData();

        bool result = await vm.AskFileSave("save?");

        Assert.True(result);
    }

    [Fact]
    public async Task AskFileSave_ReturnsFalseWhenUserCancels()
    {
        MainViewModel vm = CreateViewModel();
        vm.View = new DialogAnswerView(MessageWindow.Result.Cancel);
        await vm.NewData();

        bool result = await vm.AskFileSave("save?");

        Assert.False(result);
    }

    [Fact]
    public async Task AskProjectSave_ReturnsTrueWithoutProject()
    {
        MainViewModel vm = CreateViewModel();

        bool result = await vm.AskProjectSave("save project?");

        Assert.True(result);
    }

    [Fact]
    public async Task AskProjectSave_ReturnsFalseWithoutViewWhenProjectHasUnexportedAssets()
    {
        MainViewModel vm = CreateViewModel();
        await vm.NewData();
        UndertalePath path = AddPath(vm, "path_dirty_project");
        string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        ProjectContext project = ProjectContext.CreateWithDirectories(directory, directory, Path.Combine(directory, "project.json"));
        project.MarkAssetForExport(path);
        vm.Project = project;

        bool result = await vm.AskProjectSave("save project?");

        Assert.False(result);
    }

    [Fact]
    public async Task LoadData_ReturnsFalseWithoutView()
    {
        MainViewModel vm = CreateViewModel();
        using MemoryStream stream = new([]);

        bool result = await vm.LoadData(stream);

        Assert.False(result);
        Assert.True(vm.IsEnabled);
    }

    [Fact]
    public async Task SaveData_ReturnsFalseWithoutData()
    {
        MainViewModel vm = CreateViewModel();
        vm.View = new DialogAnswerView(MessageWindow.Result.OK);
        using MemoryStream stream = new();

        bool result = await vm.SaveData(stream);

        Assert.False(result);
        Assert.True(vm.IsEnabled);
    }

    [Fact]
    public async Task SaveData_ReturnsFalseWithoutView()
    {
        MainViewModel vm = CreateViewModel();
        await vm.NewData();
        using MemoryStream stream = new();

        bool result = await vm.SaveData(stream);

        Assert.False(result);
        Assert.True(vm.IsEnabled);
    }

    [Fact]
    public async Task ProjectCloseTask_ClearsProjectWhenNoSavePromptIsNeeded()
    {
        MainViewModel vm = CreateViewModel();
        string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        ProjectContext project = ProjectContext.CreateWithDirectories(directory, directory, Path.Combine(directory, "project.json"));
        vm.Project = project;

        bool result = await vm.ProjectCloseTask();

        Assert.True(result);
        Assert.Null(vm.Project);
    }

    [Fact]
    public async Task ProjectNewTask_ReturnsFalseWithoutData()
    {
        MainViewModel vm = CreateViewModel();
        vm.View = new DialogAnswerView(MessageWindow.Result.OK);

        bool result = await vm.ProjectNewTask();

        Assert.False(result);
    }

    [Fact]
    public async Task ProjectNewTask_ReturnsFalseWithoutView()
    {
        MainViewModel vm = CreateViewModel();
        await vm.NewData();
        vm.DataPath = "source-data.win";

        bool result = await vm.ProjectNewTask();

        Assert.False(result);
    }

    [Fact]
    public async Task ProjectNewTask_ReturnsFalseWhenNamePromptIsCanceled()
    {
        MainViewModel vm = CreateViewModel();
        DialogAnswerView view = new(MessageWindow.Result.OK);
        vm.View = view;
        await vm.NewData();
        vm.DataPath = "source-data.win";

        bool result = await vm.ProjectNewTask();

        Assert.False(result);
        Assert.Equal("Project name:", view.LastTextBoxMessage);
    }

    [Fact]
    public async Task ProjectNewTask_KeepsCurrentProjectWhenFolderPickerIsCanceled()
    {
        MainViewModel vm = CreateViewModel();
        DialogAnswerView view = new(MessageWindow.Result.OK)
        {
            TextBoxDialogResult = "Test Mod",
        };
        vm.View = view;
        await vm.NewData();
        vm.DataPath = "source-data.win";
        ProjectContext project = ProjectContext.CreateWithDirectories(
            Path.GetTempPath(),
            Path.GetTempPath(),
            Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json"));
        vm.Project = project;

        bool result = await vm.ProjectNewTask();

        Assert.False(result);
        Assert.Same(project, vm.Project);
        Assert.NotNull(view.LastOpenFolderOptions);
        Assert.Equal("Select project folder", view.LastOpenFolderOptions.Title);
    }

    [Fact]
    public async Task ProjectOpenTask_ReturnsFalseWithoutView()
    {
        MainViewModel vm = CreateViewModel();
        await vm.NewData();
        vm.DataPath = "source-data.win";

        bool result = await vm.ProjectOpenTask();

        Assert.False(result);
    }

    [Fact]
    public async Task ProjectOpenTask_ReturnsFalseWhenProjectPickerIsCanceled()
    {
        MainViewModel vm = CreateViewModel();
        DialogAnswerView view = new(MessageWindow.Result.OK);
        vm.View = view;
        await vm.NewData();
        vm.DataPath = "source-data.win";

        bool result = await vm.ProjectOpenTask();

        Assert.False(result);
        Assert.NotNull(view.LastOpenFileOptions);
        Assert.Equal("Select project.json file", view.LastOpenFileOptions.Title);
    }

    [Fact]
    public async Task ProjectOpenTask_KeepsCurrentProjectWhenProjectPickerIsCanceled()
    {
        MainViewModel vm = CreateViewModel();
        DialogAnswerView view = new(MessageWindow.Result.OK);
        vm.View = view;
        await vm.NewData();
        vm.DataPath = "source-data.win";
        ProjectContext project = ProjectContext.CreateWithDirectories(
            Path.GetTempPath(),
            Path.GetTempPath(),
            Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json"));
        vm.Project = project;

        bool result = await vm.ProjectOpenTask();

        Assert.False(result);
        Assert.Same(project, vm.Project);
        Assert.NotNull(view.LastOpenFileOptions);
        Assert.Equal("Select project.json file", view.LastOpenFileOptions.Title);
    }

    [Fact]
    public async Task DataItemAddTask_ReturnsFalseWithoutViewWhenNamePromptIsNeeded()
    {
        MainViewModel vm = CreateViewModel();
        await vm.NewData();
        int initialCount = vm.Data!.Sprites.Count;

        bool result = await vm.DataItemAddTask(vm.Data["Sprites"]);

        Assert.False(result);
        Assert.Equal(initialCount, vm.Data.Sprites.Count);
    }

    [Fact]
    public async Task DataItemAddTask_ReturnsFalseWhenNamePromptIsCanceled()
    {
        MainViewModel vm = CreateViewModel();
        vm.View = new DialogAnswerView(MessageWindow.Result.OK);
        await vm.NewData();
        int initialCount = vm.Data!.Sprites.Count;

        bool result = await vm.DataItemAddTask(vm.Data["Sprites"]);

        Assert.False(result);
        Assert.Equal(initialCount, vm.Data.Sprites.Count);
    }

    [Fact]
    public async Task DataItemAddTask_RejectsInvalidAssetName()
    {
        MainViewModel vm = CreateViewModel();
        DialogAnswerView view = new(MessageWindow.Result.OK)
        {
            TextBoxDialogResult = "123 bad"
        };
        vm.View = view;
        await vm.NewData();
        int initialCount = vm.Data!.Sprites.Count;

        bool result = await vm.DataItemAddTask(vm.Data["Sprites"]);

        Assert.False(result);
        Assert.Equal(initialCount, vm.Data.Sprites.Count);
        Assert.Contains("is not a valid identifier", view.LastMessage);
    }

    [Fact]
    public async Task DataItemAddTask_AddsNamedResource()
    {
        MainViewModel vm = CreateViewModel();
        vm.View = new DialogAnswerView(MessageWindow.Result.OK)
        {
            TextBoxDialogResult = "spr_added"
        };
        await vm.NewData();

        bool result = await vm.DataItemAddTask(vm.Data!["Sprites"]);

        Assert.True(result);
        Assert.Single(vm.Data.Sprites);
        Assert.Equal("spr_added", vm.Data.Sprites[0].Name.Content);
    }

    [Fact]
    public async Task DataItemRemoveTask_ReturnsFalseWithoutView()
    {
        MainViewModel vm = CreateViewModel();
        await vm.NewData();
        UndertaleSprite sprite = AddSprite(vm, "spr_remove");

        bool result = await vm.DataItemRemoveTask(sprite);

        Assert.False(result);
        Assert.Contains(sprite, vm.Data!.Sprites);
    }

    [Fact]
    public async Task DataItemRemoveTask_ReturnsFalseWhenDeleteIsCanceled()
    {
        MainViewModel vm = CreateViewModel();
        vm.View = new DialogAnswerView(MessageWindow.Result.No);
        await vm.NewData();
        UndertaleSprite sprite = AddSprite(vm, "spr_remove");

        bool result = await vm.DataItemRemoveTask(sprite);

        Assert.False(result);
        Assert.Contains(sprite, vm.Data!.Sprites);
    }

    [Fact]
    public async Task DataItemRemoveTask_RemovesResourceWhenConfirmed()
    {
        MainViewModel vm = CreateViewModel();
        vm.View = new DialogAnswerView(MessageWindow.Result.Yes);
        await vm.NewData();
        UndertaleSprite sprite = AddSprite(vm, "spr_remove");

        bool result = await vm.DataItemRemoveTask(sprite);

        Assert.True(result);
        Assert.DoesNotContain(sprite, vm.Data!.Sprites);
    }

    private static MainViewModel CreateViewModel()
    {
        ServiceCollection services = new();
        services.AddSingleton<MainViewModel>();
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        MainViewModel vm = serviceProvider.GetRequiredService<MainViewModel>();
        vm.Initialize();
        return vm;
    }

    private static UndertaleSprite AddSprite(MainViewModel vm, string name)
    {
        UndertaleSprite sprite = new()
        {
            Name = vm.Data!.Strings.MakeString(name, createNew: true)
        };
        vm.Data.Sprites.Add(sprite);
        return sprite;
    }

    private static UndertalePath AddPath(MainViewModel vm, string name)
    {
        UndertalePath path = new()
        {
            Name = vm.Data!.Strings.MakeString(name, createNew: true)
        };
        vm.Data.Paths.Add(path);
        return path;
    }

    private sealed class DialogAnswerView(MessageWindow.Result result) : IView
    {
        public Uri? LaunchedUri { get; private set; }
        public string? LastMessage { get; private set; }
        public string? LastMessageTitle { get; private set; }
        public string? LastTextBoxMessage { get; private set; }
        public string? LastTextBoxText { get; private set; }
        public List<string> Messages { get; } = [];
        public FilePickerOpenOptions? LastOpenFileOptions { get; private set; }
        public FilePickerSaveOptions? LastSaveFileOptions { get; private set; }
        public FolderPickerOpenOptions? LastOpenFolderOptions { get; private set; }
        public IReadOnlyList<IStorageFile> OpenFileDialogResult { get; init; } = [];
        public IStorageFile? SaveFileDialogResult { get; init; }
        public IReadOnlyList<IStorageFolder> OpenFolderDialogResult { get; init; } = [];
        public string? TextBoxDialogResult { get; init; }

        public Task<MessageWindow.Result> MessageDialog(
            string message,
            string? title = null,
            MessageWindow.Buttons buttons = MessageWindow.Buttons.OK)
        {
            LastMessage = message;
            LastMessageTitle = title;
            Messages.Add(message);
            return Task.FromResult(result);
        }

        public Task<IReadOnlyList<IStorageFile>> OpenFileDialog(FilePickerOpenOptions options)
        {
            LastOpenFileOptions = options;
            return Task.FromResult(OpenFileDialogResult);
        }

        public Task<IStorageFile?> SaveFileDialog(FilePickerSaveOptions options)
        {
            LastSaveFileOptions = options;
            return Task.FromResult(SaveFileDialogResult);
        }

        public Task<IReadOnlyList<IStorageFolder>> OpenFolderDialog(FolderPickerOpenOptions options)
        {
            LastOpenFolderOptions = options;
            return Task.FromResult(OpenFolderDialogResult);
        }

        public Task<bool> LaunchUriAsync(Uri uri)
        {
            LaunchedUri = uri;
            return Task.FromResult(true);
        }

        public Task<string?> TextBoxDialog(string message, string text = "", string? title = null, bool isMultiline = false, bool isReadOnly = false)
        {
            LastTextBoxMessage = message;
            LastTextBoxText = text;
            return Task.FromResult(TextBoxDialogResult);
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
