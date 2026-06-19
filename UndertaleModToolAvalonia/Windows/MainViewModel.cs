using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using PropertyChanged.SourceGenerator;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModLib.Project;

namespace UndertaleModToolAvalonia;

public partial class MainViewModel
{
    // Set this when testing.
    public IView? View;

    // Services
    public readonly IServiceProvider ServiceProvider;

    /// <summary>Error messages to be displayed after the view has been loaded.</summary>
    public List<string> LazyErrorMessages = [];

    // Settings
    public SettingsFile? Settings { get; set; }

    // Scripting
    public Scripting Scripting = null!;

    // Window
    public string Title => $"UndertaleModToolAvalonia - v" +
        (Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "?.?.?.?") +
        $"{(Project?.Name is not null ? " - " + Project.Name : "")}" +
        $"{(Data?.GeneralInfo is not null ? " - " + Data.GeneralInfo.ToString() : "")}" +
        $"{(DataPath is not null ? " [" + DataPath + "]" : "")}";

    [Notify]
    private WindowState _WindowState = WindowState.Maximized;

    [Notify]
    private bool _IsEnabled = true;

    // Data
    [Notify]
    private UndertaleData? _Data;
    [Notify]
    private string? _DataPath;
    [Notify]
    private (uint Major, uint Minor, uint Release, uint Build) _DataVersion;

    // Project
    [Notify]
    private ProjectContext? _Project;

    // Tree data grid
    public partial class TreeDataGridItem
    {
        [Notify]
        private string _Text = "<unset text!>";
        public object? Value { get; set; }
        public object? Tag { get; set; }
        [Notify]
        private IList<TreeDataGridItem>? _Children;
    }

    [Notify]
    private ObservableCollection<TreeDataGridItem> _TreeDataGridData = [];

    [Notify]
    private string _FilterText = "";

    public event Action<string>? FilterTextChanged;

    // Tabs
    public ObservableCollection<TabItemViewModel> Tabs { get; set; }

    [Notify]
    private TabItemViewModel? _TabSelected;
    [Notify]
    private int _TabSelectedIndex;
    [Notify]
    private bool _TabIsMarkedForExport = false;
    [Notify]
    private bool _TabCanMarkedForExport = false;
    [Notify]
    private string _TabSelectedResourceIdString = "None";

    // Command text box
    [Notify]
    private string _CommandTextBoxText = "";

    public async void RunCommandText()
    {
        await RunCommandTextAsync();
    }

    public async Task RunCommandTextAsync()
    {
        string text = CommandTextBoxText.Trim('\r', '\n');
        if (String.IsNullOrWhiteSpace(text))
            return;

        object? result = await Scripting.RunScript(text);
        if (!Scripting.ConsumeFinishedMessageEnabled())
            return;

        CommandTextBoxText = Scripting.ScriptExecutionSuccess
            ? result?.ToString() ?? ""
            : Scripting.ScriptErrorMessage;
    }

    // Image cache
    public ImageCache ImageCache = new();

    // Internal clipboard
    public object? InternalClipboard = null;

    public MainViewModel(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;

        AudioPlayer.Configure(f => Dispatcher.UIThread.Post(f));

        Tabs = [
            new TabItemViewModel(new DescriptionViewModel(
                "Welcome to UndertaleModTool!",
                "Open a data.win file to get started, then double click on the items on the left to view them."),
                isSelected: true),
        ];
    }

    public void Initialize()
    {
        Settings = SettingsFile.Load(ServiceProvider);
        Scripting = new(ServiceProvider);

        WindowState = Settings.StartMaximized ? WindowState.Maximized : WindowState.Normal;
    }

    public async void OnLoaded()
    {
        await OnLoadedTask();
    }

    public async Task<bool> OnLoadedTask()
    {
        if (View is null)
            return false;

        foreach (string message in LazyErrorMessages)
        {
            await View.MessageDialog(message);
        }
        LazyErrorMessages.Clear();

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.Args?.Length >= 1)
            {
                try
                {
                    using FileStream stream = File.OpenRead(desktop.Args[0]);
                    if (await LoadData(stream))
                    {
                        DataPath = stream.Name;
                    }
                }
                catch (SystemException e)
                {
                    await View.MessageDialog($"Error opening data file from argument: {e.Message}");
                }
            }
        }

        return true;
    }

    public async void OpenDroppedFiles(IEnumerable<IStorageItem>? files)
    {
        await OpenDroppedFilesTask(files);
    }

    public async Task<bool> OpenDroppedFilesTask(IEnumerable<IStorageItem>? files)
    {
        if (files is null)
            return false;

        var list = files.ToList();
        if (list.Count != 1)
            return false;

        if (list[0] is not IStorageFile file)
            return false;

        if (!await AskFileSave("Save data file before opening a new one?"))
            return false;

        CloseData();

        using Stream stream = await file.OpenReadAsync();

        if (await LoadData(stream))
        {
            DataPath = file.TryGetLocalPath();
            return true;
        }

        return false;
    }

    // Called by [Notify]
    public void OnDataChanged()
    {
        if (Data is not null)
        {
            if (Data.GeneralInfo is not null)
                Data.GeneralInfo.PropertyChanged += DataGeneralInfoChangedHandler;

            Data.ToolInfo.InstanceIdPrefix = () => Settings?.InstanceIdPrefix;
            Data.ToolInfo.DecompilerSettings = Settings?.DecompileSettings;
        }

        UpdateVersion();

        TreeDataGridData.Clear();

        if (FilterTextChanged is not null)
            foreach (Delegate item in FilterTextChanged.GetInvocationList())
            {
                FilterTextChanged -= (Action<string>)item;
            }

        if (Data is not null)
        {
            IList<TreeDataGridItem>? MakeChildren<T>(IList<T>? list) where T : notnull
            {
                if (list is not null)
                {
                    ObservableCollectionView<T, TreeDataGridItem> view = new(list,
                        transform: x => new TreeDataGridItem() { Text = "", Value = x });

                    FilterTextChanged += filterText =>
                    {
                        view.SetFilter(item => AssetNameContainsText(item, filterText));
                    };

                    view.SetFilter(item => AssetNameContainsText(item, FilterText));

                    return view.Output;
                }
                return null;
            }

            var dataItem = new TreeDataGridItem()
            {
                Value = Data,
                Text = "Data",
                Children = [],
            };

            if (Data.GeneralInfo is not null)
                dataItem.Children.Add(new() { Value = "GeneralInfo", Text = "General info" });
            if (Data.GlobalInitScripts is not null)
                dataItem.Children.Add(new() { Value = "GlobalInitScripts", Text = "Global init scripts" });
            if (Data.GameEndScripts is not null)
                dataItem.Children.Add(new() { Value = "GameEndScripts", Text = "Game End scripts" });

            if (Data.AudioGroups is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "AudioGroups", Text = "Audio groups",
                Children = MakeChildren(Data.AudioGroups)});
            if (Data.Sounds is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "Sounds", Text = "Sounds",
                Children = MakeChildren(Data.Sounds)});
            if (Data.Sprites is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "Sprites", Text = "Sprites",
                Children = MakeChildren(Data.Sprites)});
            if (Data.Backgrounds is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "Backgrounds", Text = "Backgrounds & Tile sets",
                Children = MakeChildren(Data.Backgrounds)});
            if (Data.Paths is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "Paths", Text = "Paths",
                Children = MakeChildren(Data.Paths)});
            if (Data.Scripts is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "Scripts", Text = "Scripts",
                Children = MakeChildren(Data.Scripts)});
            if (Data.Shaders is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "Shaders", Text = "Shaders",
                Children = MakeChildren(Data.Shaders)});
            if (Data.Fonts is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "Fonts", Text = "Fonts",
                Children = MakeChildren(Data.Fonts)});
            if (Data.Timelines is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "Timelines", Text = "Timelines",
                Children = MakeChildren(Data.Timelines)});
            if (Data.GameObjects is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "GameObjects", Text = "Game objects",
                Children = MakeChildren(Data.GameObjects)});
            if (Data.Rooms is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "Rooms", Text = "Rooms",
                Children = MakeChildren(Data.Rooms)});
            if (Data.Extensions is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "Extensions", Text = "Extensions",
                Children = MakeChildren(Data.Extensions)});
            if (Data.TexturePageItems is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "TexturePageItems", Text = "Texture page items",
                Children = MakeChildren(Data.TexturePageItems)});
            if (Data.Code is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "Code", Text = "Code",
                Children = MakeChildren(Data.Code)});
            if (Data.Variables is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "Variables", Text = "Variables",
                Children = MakeChildren(Data.Variables)});
            if (Data.Functions is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "Functions", Text = "Functions",
                Children = MakeChildren(Data.Functions)});
            if (Data.CodeLocals is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "CodeLocals", Text = "Code locals",
                Children = MakeChildren(Data.CodeLocals)});
            if (Data.Strings is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "Strings", Text = "Strings",
                Children = MakeChildren(Data.Strings)});
            if (Data.EmbeddedTextures is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "EmbeddedTextures", Text = "Embedded textures",
                Children = MakeChildren(Data.EmbeddedTextures)});
            if (Data.EmbeddedAudio is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "EmbeddedAudio", Text = "Embedded audio",
                Children = MakeChildren(Data.EmbeddedAudio)});
            if (Data.TextureGroupInfo is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "TextureGroupInformation", Text = "Texture group information",
                Children = MakeChildren(Data.TextureGroupInfo)});
            if (Data.EmbeddedImages is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "EmbeddedImages", Text = "Embedded images",
                Children = MakeChildren(Data.EmbeddedImages)});
            if (Data.AnimationCurves is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "AnimationCurves", Text = "Animation curves",
                Children = MakeChildren(Data.AnimationCurves)});
            if (Data.ParticleSystems is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "ParticleSystems", Text = "Particle systems",
                Children = MakeChildren(Data.ParticleSystems)});
            if (Data.ParticleSystemEmitters is not null)
                dataItem.Children.Add(new() {Tag = "list", Value = "ParticleSystemEmitters", Text = "Particle system emitters",
                Children = MakeChildren(Data.ParticleSystemEmitters)});

            TreeDataGridData.Add(dataItem);

            if (View is MainView mainView)
                mainView.ExpandItemOnTree(dataItem);
        }
    }

    private bool AssetNameContainsText<T>(T asset, string text)
    {
        string? name = asset switch
        {
            UndertaleNamedResource namedResource => namedResource.Name.Content,
            UndertaleString _string => _string.Content,
            _ => null,
        };

        if (name is null)
            return true;

        return name.Contains(text, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Ask if user wants to save the current file before continuing.
    /// Returns true if either it saved successfully, or if the user didn't want to save, or if there is no file loaded.</summary>
    public async Task<bool> AskFileSave(string message)
    {
        if (Data is null)
            return true;
        if (View is null)
            return false;

        var result = await View.MessageDialog(message, buttons: MessageWindow.Buttons.YesNoCancel);
        if (result == MessageWindow.Result.Yes)
        {
            if (await FileSaveTask())
            {
                return true;
            }
        }
        else if (result == MessageWindow.Result.No)
        {
            return true;
        }

        return false;
    }

    /// <summary>Ask if user wants to save the current project before continuing.
    /// Returns true if either it saved successfully, or if the user didn't want to save, or if there is no project loaded, or if the project has no unexported assets.</summary>
    public async Task<bool> AskProjectSave(string message)
    {
        if (Project is null || !Project.HasUnexportedAssets)
            return true;
        if (View is null)
            return false;

        var result = await View.MessageDialog(message, buttons: MessageWindow.Buttons.YesNoCancel);
        if (result == MessageWindow.Result.Yes)
        {
            if (await ProjectSaveTask())
            {
                return true;
            }
        }
        else if (result == MessageWindow.Result.No)
        {
            return true;
        }

        return false;
    }

    public Task<bool> NewData()
    {
        CloseData();

        Data = UndertaleData.CreateNew();
        DataPath = null;

        return Task.FromResult(true);
    }

    public async Task<bool> LoadData(Stream stream)
    {
        if (View is not { } view)
            return false;

        IsEnabled = false;

        ILoaderWindow w = view.LoaderOpen();
        w.SetText("Opening data file...");

        try
        {
            List<string> warnings = [];
            bool hadImportantWarnings = false;

            UndertaleData data = await Task.Run(() => UndertaleIO.Read(stream,
                (string warning, bool isImportant) =>
                {
                    warnings.Add(warning);
                    if (isImportant)
                    {
                        hadImportantWarnings = true;
                    }
                },
                (string message) =>
                {
                    Dispatcher.UIThread.Post(() => w.SetText($"Opening data file... {message}"));
                })
            );

            if (warnings.Count > 0)
            {
                w.EnsureShown();
                await view.MessageDialog($"Warnings occurred when loading the data file:\n\n" +
                    $"{(hadImportantWarnings ? "Data loss will likely occur when trying to save.\n" : "")}" +
                    $"{String.Join("\n", warnings)}");
            }

            // TODO: Add other checks for possible stuff.

            Data = data;

            return true;
        }
        catch (Exception e)
        {
            w.EnsureShown();
            await view.MessageDialog($"Error opening data file: {e.Message}");

            return false;
        }
        finally
        {
            IsEnabled = true;
            w.Close();
        }
    }

    public async Task<bool> SaveData(Stream stream)
    {
        if (Data is null)
            return false;
        if (View is not { } view)
            return false;

        IsEnabled = false;

        ILoaderWindow w = view.LoaderOpen();
        w.SetText("Saving data file...");

        try
        {
            // TODO: RecompileAllCodeSourcesOnProjectSave setting
            if (Project is not null)
            {
                Project.RecompileAllCodeSources();
            }

            await Task.Run(() => UndertaleIO.Write(stream, Data, message =>
            {
                Dispatcher.UIThread.Post(() => w.SetText($"Saving data file... {message}"));
            }));

            return true;
        }
        catch (ProjectException e)
        {
            w.EnsureShown();
            await view.MessageDialog($"Recompile error:\n{e.Message}");
        }
        catch (Exception e)
        {
            w.EnsureShown();
            await view.MessageDialog($"Error saving data file:\n{e.Message}");
        }
        finally
        {
            IsEnabled = true;
            w.Close();
        }

        return false;
    }

    public void CloseData()
    {
        Data = null;
        DataPath = null;

        TabCloseAll();

        ClearProject();

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            foreach (var window in desktop.Windows.ToList())
            {
                if (window is SearchInCodeWindow)
                {
                    window.Close();
                }
            }
        }
    }

    public void UpdateVersion()
    {
        DataVersion = Data is not null && Data.GeneralInfo is not null ? (Data.GeneralInfo.Major, Data.GeneralInfo.Minor, Data.GeneralInfo.Release, Data.GeneralInfo.Build) : default;
    }

    private void DataGeneralInfoChangedHandler(object? sender, PropertyChangedEventArgs e)
    {
        if (Data is not null && e.PropertyName is
            nameof(UndertaleGeneralInfo.Major) or nameof(UndertaleGeneralInfo.Minor) or
            nameof(UndertaleGeneralInfo.Release) or nameof(UndertaleGeneralInfo.Build))
        {
            UpdateVersion();
        }
    }

    // Menus
    public async void FileNew()
    {
        await FileNewTask();
    }

    public async Task<bool> FileNewTask()
    {
        if (await AskProjectSave("There are assets marked to be exported in the current project. Save project before closing it?")
            && await AskFileSave("Save data file before creating a new one?"))
        {
            await NewData();
            return true;
        }

        return false;
    }

    public async void FileOpen()
    {
        await FileOpenTask();
    }

    public async Task<bool> FileOpenTask()
    {
        if (!await AskProjectSave("There are assets marked to be exported in the current project. Save project before closing it?"))
            return false;
        if (!await AskFileSave("Save data file before opening a new one?"))
            return false;
        if (View is null)
            return false;

        var files = await View.OpenFileDialog(new FilePickerOpenOptions()
        {
            Title = "Open data file",
            FileTypeFilter = FilePickerFileTypes.Data,
        });

        if (files.Count != 1)
            return false;

        CloseData();

        using Stream stream = await files[0].OpenReadAsync();

        if (await LoadData(stream))
        {
            DataPath = files[0].TryGetLocalPath();
            return true;
        }

        return false;
    }

    public async void FileSave()
    {
        await FileSaveTask();
    }

    public async Task<bool> FileSaveTask()
    {
        if (Data is null)
            return false;
        if (View is null)
            return false;

        if (Project is not null)
        {
            var result = await View.MessageDialog("Save to the project's designated data file for saving?", buttons: MessageWindow.Buttons.YesNoCancel);
            if (result == MessageWindow.Result.Yes)
            {
                using FileStream fileStream = File.Open(Project.SaveDataPath, FileMode.Create);
                if (await SaveData(fileStream))
                {
                    return true;
                }
                return false;
            }
            else if (result != MessageWindow.Result.No)
            {
                return false;
            }
            // If pressed No, continue saving as if there's no project.
        }

        IStorageFile? file = await View.SaveFileDialog(new FilePickerSaveOptions()
        {
            Title = "Save data file",
            FileTypeChoices = FilePickerFileTypes.Data,
            DefaultExtension = ".win",
        });

        if (file is null)
            return false;

        using Stream stream = await file.OpenWriteAsync();

        if (await SaveData(stream))
        {
            DataPath = file.TryGetLocalPath();
            return true;
        }

        return false;
    }

    public async void FileClose()
    {
        await FileCloseTask();
    }

    public async Task<bool> FileCloseTask()
    {
        if (!await AskProjectSave("There are assets marked to be exported in the current project. Save project before closing it?"))
            return false;
        if (!await AskFileSave("Save data file before closing?"))
            return false;

        CloseData();
        return true;
    }

    public async void FileRun()
    {
        await FileRunTask();
    }

    public async Task<bool> FileRunTask()
    {
        // NOTE: The project system would make this a lot simpler!
        if (Data is null)
            return false;
        if (View is null)
            return false;

        string question = $"Save data file before running? {(DataPath is null
            ? " It must be saved before running."
            : $"If it's not saved, the data file at the last location will be used (\"{DataPath}\").")}";

        if (!await AskFileSave(question))
            return false;

        if (DataPath is null)
            return false;

        var files = await View.OpenFileDialog(new FilePickerOpenOptions()
        {
            Title = "Open runner",
            FileTypeFilter = FilePickerFileTypes.All,
        });

        if (files.Count != 1)
            return false;

        string runnerPath = files[0].TryGetLocalPath() ?? string.Empty;
        if (runnerPath == string.Empty)
            return false;

        if (!File.Exists(DataPath))
            return false;

        // "launcher" allows game_change data files to still access files above the data path.
        Process.Start(new ProcessStartInfo(runnerPath, $"-game \"{DataPath}\" launcher") { WorkingDirectory = Path.GetDirectoryName(DataPath) });
        return true;
    }

    public async void FileSettings()
    {
        if (View is MainView mainView)
            await mainView.OpenSettingsDialog(ServiceProvider);
    }

    public void FileExit()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    public void ToolsSearchInCode()
    {
        if (View is MainView mainView)
            mainView.OpenSearchInCode(ServiceProvider);
    }

    public async void ScriptsRunOtherScript()
    {
        await ScriptsRunOtherScriptTask();
    }

    public async Task<bool> ScriptsRunOtherScriptTask()
    {
        if (View is null)
            return false;

        var files = await View.OpenFileDialog(new FilePickerOpenOptions()
        {
            Title = "Run script",
            FileTypeFilter = FilePickerFileTypes.CS,
        });

        if (files.Count != 1)
            return false;

        string? filePath = files[0].TryGetLocalPath();
        if (filePath is not null && File.Exists(filePath))
        {
            return await RunScriptFileAsync(filePath);
        }

        using (Stream stream = await files[0].OpenReadAsync())
        {
            using StreamReader streamReader = new(stream);
            string text = await streamReader.ReadToEndAsync();
            await RunScriptTextAsync(text, filePath, files[0].Name);
        }

        return true;
    }

    public async Task<bool> RunScriptFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            if (View is null)
                return false;

            await View.MessageDialog("The script file doesn't exist.");
            return false;
        }

        string text = await File.ReadAllTextAsync(filePath);
        await RunScriptTextAsync(text, filePath, Path.GetFileName(filePath) ?? "Script");
        return true;
    }

    private async Task RunScriptTextAsync(string text, string? filePath, string displayName)
    {
        await Scripting.RunScript(text, filePath);

        if (!Scripting.ConsumeFinishedMessageEnabled())
            return;

        CommandTextBoxText = Scripting.ScriptExecutionSuccess
            ? $"{displayName} finished!"
            : Scripting.ScriptErrorMessage;
    }

    void ClearProject()
    {
        Project = null;

        if (View is MainView mainView)
            mainView.CloseProjectAssets();
    }

    void SetProject(ProjectContext projectContext)
    {
        Project = projectContext;
        Project.UnexportedAssetsChanged += (s, e) =>
        {
            UpdateSelectedTabProperties();
        };

        UpdateSelectedTabProperties();
    }

    async Task<string?> AskProjectDestinationDataFile()
    {
        if (View is null)
            return null;

        // Destination data file
        // TODO: Check if same as source and if empty directory
        IStorageFile? destinationDataFile = await View.SaveFileDialog(new()
        {
            Title = "Select destination data file location",
            FileTypeChoices = FilePickerFileTypes.Data,
        });
        string? destinationDataPath = destinationDataFile?.TryGetLocalPath();

        return destinationDataPath;
    }

    public async void ProjectNew()
    {
        await ProjectNewTask();
    }

    public async Task<bool> ProjectNewTask()
    {
        // TODO: Ask for source data file if nothing is opened
        if (Data is null || DataPath is null)
            return false;
        if (View is null)
            return false;

        if (!await AskProjectSave("There are assets marked to be exported in the current project. Save project before creating a new one?"))
            return false;

        // Project name
        string? projectName = await View.TextBoxDialog("Project name:", $"{Data.GeneralInfo?.DisplayName?.Content ?? "New"} Mod");
        if (projectName is null)
            return false;

        // Project folder
        IReadOnlyList<IStorageFolder> projectFolderList = await View.OpenFolderDialog(new() { Title = "Select project folder" });
        string? projectFolderPath = projectFolderList.ElementAtOrDefault(0)?.TryGetLocalPath();

        if (projectFolderPath is null)
            return false;

        string projectFilePath = Path.Join(projectFolderPath, "project.json");

        // Destination data file
        string? destinationDataPath = await AskProjectDestinationDataFile();
        if (destinationDataPath is null)
            return false;

        ProjectContext projectContext;
        try
        {
            projectContext = new(Data, DataPath, destinationDataPath, projectFilePath, projectName.Trim(), Dispatcher.UIThread.Invoke);
        }
        catch (ProjectException e)
        {
            await View.MessageDialog($"Failed to create new project:\n{e.Message}");
            return false;
        }
        catch (Exception e)
        {
            await View.MessageDialog($"Error occurred when creating new project:\n{e}");
            return false;
        }

        DataPath = destinationDataPath;
        ClearProject();
        SetProject(projectContext);
        return true;
    }

    public async void ProjectOpen()
    {
        await ProjectOpenTask();
    }

    public async Task<bool> ProjectOpenTask()
    {
        // TODO: Ask for source data file if nothing is opened
        if (Data is null || DataPath is null)
            return false;
        if (View is null)
            return false;

        if (!await AskProjectSave("There are assets marked to be exported in the current project. Save project before opening a new one?"))
            return false;

        // Project file
        IReadOnlyList<IStorageFile> projectFileList = await View.OpenFileDialog(new()
        {
            Title = "Select project.json file",
            FileTypeFilter = FilePickerFileTypes.JSON,
        });
        string? projectFilePath = projectFileList.ElementAtOrDefault(0)?.TryGetLocalPath();
        if (projectFilePath is null)
            return false;

        // Destination data file
        string? destinationDataPath = await AskProjectDestinationDataFile();
        if (destinationDataPath is null)
            return false;

        ProjectContext projectContext;
        try
        {
            projectContext = ProjectContext.CreateWithDataFilePaths(DataPath, destinationDataPath, projectFilePath);
            projectContext.Import(Data, null, Dispatcher.UIThread.Invoke);
        }
        catch (ProjectException e)
        {
            await View.MessageDialog($"Failed to load project:\n{e.Message}");
            return false;
        }
        catch (Exception e)
        {
            await View.MessageDialog($"Error occurred when loading project:\n{e}");
            return false;
        }

        DataPath = destinationDataPath;
        ClearProject();
        SetProject(projectContext);
        return true;
    }

    public async void ProjectSave()
    {
        await ProjectSaveTask();
    }

    public async Task<bool> ProjectSaveTask()
    {
        if (Project is null || Data is null || DataPath is null)
            return false;

        try
        {
            Project.Export(true);
            return true;
        }
        catch (ProjectException e)
        {
            string message = $"Failed to save project:\n{e.Message}";
            if (View is null)
                LazyErrorMessages.Add(message);
            else
                await View.MessageDialog(message);
        }
        catch (Exception e)
        {
            string message = $"Error occurred when saving project:\n{e}";
            if (View is null)
                LazyErrorMessages.Add(message);
            else
                await View.MessageDialog(message);
        }

        return false;
    }

    public void ProjectViewUnexportedAssets()
    {
        if (Project is null || Data is null || DataPath is null)
            return;

        if (View is MainView mainView)
            mainView.OpenProjectAssets(ServiceProvider);
    }

    public async void ProjectClose()
    {
        await ProjectCloseTask();
    }

    public async Task<bool> ProjectCloseTask()
    {
        if (!await AskProjectSave("There are assets marked to be exported in the current project. Save project before closing?"))
            return false;

        ClearProject();
        return true;
    }

    public async void HelpGitHub()
    {
        await HelpGitHubTask();
    }

    public async Task<bool> HelpGitHubTask()
    {
        if (View is null)
            return false;

        return await View.LaunchUriAsync(new Uri("https://github.com/UnderminersTeam/UndertaleModTool"));
    }

    public async void HelpAbout()
    {
        await HelpAboutTask();
    }

    public async Task<bool> HelpAboutTask()
    {
        if (View is null)
            return false;

        await View.MessageDialog($"UndertaleModTool v{Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "?.?.?.?"} " +
            $"by the Underminers team\nLicensed under the GNU General Public License Version 3.", title: "About");
        return true;
    }

    public void SetFilterText(string text)
    {
        FilterTextChanged?.Invoke(text);
    }

    public async void DataItemAdd(IList list)
    {
        await DataItemAddTask(list);
    }

    public async Task<bool> DataItemAddTask(IList? list)
    {
        if (Data is null || list is null)
            return false;

        UndertaleResource res = UndertaleData.CreateResource(list);

        string? name = UndertaleData.GetDefaultResourceName(list);
        if (name is not null)
        {
            if (View is null)
                return false;

            name = await View.TextBoxDialog("Name of new asset:", name);
            if (name is null)
                return false;

            static bool IsValidAssetIdentifier(string name)
            {
                if (string.IsNullOrEmpty(name))
                    return false;

                char firstChar = name[0];
                if (!char.IsAsciiLetter(firstChar) && firstChar != '_')
                    return false;

                foreach (char c in name.Skip(1))
                    if (!char.IsAsciiLetterOrDigit(c) && c != '_')
                        return false;

                return true;
            }

            if (!IsValidAssetIdentifier(name))
            {
                await View.MessageDialog($"Asset name \"{name}\" is not a valid identifier. Only letters, digits and underscore allowed, and it must not start with a digit.");
                return false;
            }
        }

        var newResources = Data.InitializeResource(res, list, name);

        if (res is UndertaleRoom room)
        {
            if (View is null)
                return false;

            if (await View.MessageDialog("Add the new room to the end of the room order list?", buttons: MessageWindow.Buttons.YesNo) == MessageWindow.Result.Yes)
                Data.GeneralInfo?.RoomOrder.Add(new(room));
        }

        list.Add(res);

        if (Project is not null && res is IProjectAsset { ProjectExportable: true } projectAsset)
        {
            Project.MarkAssetForExport(projectAsset);

            foreach (UndertaleResource newResource in newResources)
            {
                if (newResource is IProjectAsset { ProjectExportable: true } newProjectAsset)
                {
                    Project.MarkAssetForExport(newProjectAsset);
                }
            }
        }

        if (Settings!.OpenNewResourceAfterCreatingIt)
        {
            TabOpen(res, inNewTab: true);
        }

        return true;
    }

    public async void DataItemRemove(UndertaleResource resource)
    {
        await DataItemRemoveTask(resource);
    }

    public async Task<bool> DataItemRemoveTask(UndertaleResource resource)
    {
        if (Data is null)
            return false;

        if (View is null)
            return false;

        if (await View.MessageDialog($"Delete {resource}?\nNote that the code often references objects by ID, " +
                $"so this operation is likely to break stuff because other items will shift up!",
                buttons: MessageWindow.Buttons.YesNo) != MessageWindow.Result.Yes)
            return false;

        // TODO: Maybe do something about all references to this.
        Data[resource.GetType()].Remove(resource);

        if (Project is not null && resource is IProjectAsset projectAsset)
        {
            Project.UnmarkAssetForExport(projectAsset);
        }

        // TODO: Close tabs, remove histories

        return true;
    }

    public TabItemViewModel? TabOpen(object? item, bool inNewTab = false)
    {
        if (Data is null)
            return null;

        ITabContent? content = item switch
        {
            DescriptionViewModel vm => vm,
            "GeneralInfo" => new GeneralInfoViewModel(Data),
            "GlobalInitScripts" => new GlobalInitScriptsViewModel((Data.GlobalInitScripts as ObservableCollection<UndertaleGlobalInit>)!),
            "GameEndScripts" => new GameEndScriptsViewModel((Data.GameEndScripts as ObservableCollection<UndertaleGlobalInit>)!),
            UndertaleAudioGroup r => new UndertaleAudioGroupViewModel(r),
            UndertaleSound r => new UndertaleSoundViewModel(r, ServiceProvider),
            UndertaleSprite r => new UndertaleSpriteViewModel(r, ServiceProvider),
            UndertaleBackground r => new UndertaleBackgroundViewModel(r, ServiceProvider),
            UndertalePath r => new UndertalePathViewModel(r),
            UndertaleScript r => new UndertaleScriptViewModel(r),
            UndertaleShader r => new UndertaleShaderViewModel(r, ServiceProvider),
            UndertaleFont r => new UndertaleFontViewModel(r, ServiceProvider),
            UndertaleTimeline r => new UndertaleTimelineViewModel(r),
            UndertaleGameObject r => new UndertaleGameObjectViewModel(r, ServiceProvider),
            UndertaleRoom r => new UndertaleRoomViewModel(r, ServiceProvider),
            UndertaleExtension r => new UndertaleExtensionViewModel(r, ServiceProvider),
            UndertaleTexturePageItem r => new UndertaleTexturePageItemViewModel(r, ServiceProvider),
            UndertaleCode r => new UndertaleCodeViewModel(r, ServiceProvider),
            UndertaleVariable r => new UndertaleVariableViewModel(r),
            UndertaleFunction r => new UndertaleFunctionViewModel(r),
            UndertaleCodeLocals r => new UndertaleCodeLocalsViewModel(r),
            UndertaleString r => new UndertaleStringViewModel(r),
            UndertaleEmbeddedTexture r => new UndertaleEmbeddedTextureViewModel(r, ServiceProvider),
            UndertaleEmbeddedAudio r => new UndertaleEmbeddedAudioViewModel(r, ServiceProvider),
            UndertaleTextureGroupInfo r => new UndertaleTextureGroupInfoViewModel(r),
            UndertaleEmbeddedImage r => new UndertaleEmbeddedImageViewModel(r, ServiceProvider),
            UndertaleAnimationCurve r => new UndertaleAnimationCurveViewModel(r),
            UndertaleParticleSystem r => new UndertaleParticleSystemViewModel(r),
            UndertaleParticleSystemEmitter r => new UndertaleParticleSystemEmitterViewModel(r),
            _ => null,
        };

        if (content is not null)
        {
            if (!inNewTab && TabSelected is not null)
            {
                TabGoTo(content);
                return TabSelected;
            }
            else
            {
                TabItemViewModel tab = new(content);
                Tabs.Add(tab);
                TabSelected = tab;
                return tab;
            }
        }

        return null;
    }

    public void TabClose(TabItemViewModel tab)
    {
        var selected = TabSelected;
        var index = TabSelectedIndex;

        tab.OnClose();

        Tabs.Remove(tab);

        if (TabSelected != selected)
        {
            if (index >= Tabs.Count)
                index = Tabs.Count - 1;

            TabSelectedIndex = index;
        }
    }

    public void TabCloseSelected()
    {
        if (TabSelected is not null)
            TabClose(TabSelected);
    }

    public void TabCloseAll()
    {
        foreach (TabItemViewModel tab in Tabs.ToList())
        {
            TabClose(tab);
        }
    }

    public void TabSetToPrevious()
    {
        if (TabSelectedIndex > 0)
            TabSelectedIndex--;
        else
            TabSelectedIndex = Tabs.Count - 1;
    }

    public void TabSetToNext()
    {
        if (TabSelectedIndex < Tabs.Count - 1)
            TabSelectedIndex++;
        else
            TabSelectedIndex = 0;
    }

    public void TabGoTo(ITabContent content)
    {
        TabSelected?.GoTo(content);
        UpdateSelectedTabProperties();
    }

    public void TabGoBack()
    {
        TabSelected?.GoBack();
        UpdateSelectedTabProperties();
    }

    public void TabGoForward()
    {
        TabSelected?.GoForward();
        UpdateSelectedTabProperties();
    }

    private void OnTabSelectedChanged()
    {
        UpdateSelectedTabProperties();
    }

    // Bottom bar
    private void UpdateSelectedTabProperties()
    {
        if (Data is not null && TabSelected?.Content is IUndertaleResourceViewModel vm)
        {
            TabSelectedResourceIdString = Data.IndexOf(vm.Resource).ToString();

            if (Project is not null)
            {
                if (vm.Resource is IProjectAsset { ProjectExportable: true } projectAsset)
                {
                    TabIsMarkedForExport = Project.IsAssetMarkedForExport(projectAsset);
                    TabCanMarkedForExport = true;
                    return;
                }
            }
        }
        else
        {
            TabSelectedResourceIdString = "None";
        }

        TabIsMarkedForExport = false;
        TabCanMarkedForExport = false;
    }

    private void OnTabIsMarkedForExportChanged()
    {
        if (Project is not null
            && TabSelected?.Content is IUndertaleResourceViewModel vm
            && vm.Resource is IProjectAsset { ProjectExportable: true } projectAsset)
        {
            if (TabIsMarkedForExport)
            {
                Project.MarkAssetForExport(projectAsset);
            }
            else
            {
                Project.UnmarkAssetForExport(projectAsset);
            }
        }
    }
}
