using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using UndertaleModLib.Scripting;

namespace UndertaleModToolAvalonia.Tests;

public class ScriptingTest
{
    [AvaloniaFact]
    public async Task RunCommandTextAsync_ReturnsResultAndTracksSuccess()
    {
        MainViewModel vm = CreateViewModel();

        vm.CommandTextBoxText = "1 + 2";
        await vm.RunCommandTextAsync();

        Assert.True(vm.Scripting.ScriptExecutionSuccess);
        Assert.Equal("", vm.Scripting.ScriptErrorMessage);
        Assert.Equal("", vm.Scripting.ScriptErrorType);
        Assert.Equal("3", vm.CommandTextBoxText);
    }

    [AvaloniaFact]
    public void RunUMTScript_RunsFileScriptAndTracksSuccess()
    {
        MainViewModel vm = CreateViewModel();
        string scriptPath = Path.GetTempFileName();
        File.WriteAllText(scriptPath, "SetUMTConsoleText(\"file script ran\");");

        try
        {
            ScriptGlobals globals = new(vm.Scripting, null);

            Assert.True(globals.RunUMTScript(scriptPath));
            Assert.True(vm.Scripting.ScriptExecutionSuccess);
            Assert.Equal("file script ran", vm.CommandTextBoxText);
        }
        finally
        {
            File.Delete(scriptPath);
        }
    }

    [AvaloniaFact]
    public void LintUMTScript_CompilesFileWithoutRunningIt()
    {
        MainViewModel vm = CreateViewModel();
        string scriptPath = Path.GetTempFileName();
        File.WriteAllText(scriptPath, "SetUMTConsoleText(\"should not run\");");

        try
        {
            ScriptGlobals globals = new(vm.Scripting, null);

            Assert.True(globals.LintUMTScript(scriptPath));
            Assert.True(vm.Scripting.ScriptExecutionSuccess);
            Assert.Equal("", vm.CommandTextBoxText);
        }
        finally
        {
            File.Delete(scriptPath);
        }
    }

    [AvaloniaFact]
    public void LintUMTScript_MissingFileReportsErrorWithoutView()
    {
        MainViewModel vm = CreateViewModel();
        ScriptGlobals globals = new(vm.Scripting, null);
        string scriptPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), "missing.csx");

        Assert.False(globals.LintUMTScript(scriptPath));
        Assert.False(vm.Scripting.ScriptExecutionSuccess);
        Assert.Equal("FileNotFoundException", vm.Scripting.ScriptErrorType);
        Assert.Contains(scriptPath, vm.CommandTextBoxText);
    }

    [AvaloniaFact]
    public void MakeNewDataFile_CreatesDataWithoutBlockingOnTaskResult()
    {
        MainViewModel vm = CreateViewModel();
        ScriptGlobals globals = new(vm.Scripting, null);

        Assert.Null(vm.Data);
        Assert.True(globals.MakeNewDataFile());
        Assert.NotNull(vm.Data);
        Assert.True(vm.Scripting.ScriptExecutionSuccess);
    }

    [AvaloniaFact]
    public void LintUMTScript_CompilesStockExportAllSoundsScript()
    {
        MainViewModel vm = CreateViewModel();
        ScriptGlobals globals = new(vm.Scripting, null);
        string scriptPath = FindRepositoryFile(Path.Combine(
            "UndertaleModTool",
            "Scripts",
            "Resource Exporters",
            "ExportAllSounds.csx"));

        Assert.True(globals.LintUMTScript(scriptPath), vm.Scripting.ScriptErrorMessage);
        Assert.True(vm.Scripting.ScriptExecutionSuccess);
    }

    [AvaloniaFact]
    public void LintScript_UsesDefaultUndertaleScriptReferences()
    {
        MainViewModel vm = CreateViewModel();
        string script = """
            using ImageMagick;
            using UndertaleModLib.Util;

            string result = typeof(TextureWorker).FullName + typeof(MagickImage).FullName;
            """;

        Assert.True(vm.Scripting.LintScript(script, null, out string message), message);
        Assert.True(vm.Scripting.ScriptExecutionSuccess);
    }

    [AvaloniaFact]
    public void MainWindow_BuildsStockScriptsMenu()
    {
        MainWindow window = new();
        NativeMenu? rootMenu = NativeMenu.GetMenu(window) ?? NativeDock.GetMenu(window);

        NativeMenuItem scriptsItem = Assert.Single(rootMenu!.Items.OfType<NativeMenuItem>(), item => item.Header?.ToString() == "_Scripts");
        NativeMenuItem exportersItem = Assert.Single(scriptsItem.Menu!.Items.OfType<NativeMenuItem>(), item => item.Header?.ToString() == "Resource Exporters");

        Assert.Contains(exportersItem.Menu!.Items.OfType<NativeMenuItem>(), item => item.Header?.ToString() == "ExportAllSounds.csx");
    }

    [AvaloniaFact]
    public async Task RunCommandTextAsync_RespectsFinishedMessageFlag()
    {
        MainViewModel vm = CreateViewModel();

        vm.CommandTextBoxText = "SetUMTConsoleText(\"kept\"); SetFinishedMessage(false); \"replace\"";
        await vm.RunCommandTextAsync();

        Assert.True(vm.Scripting.ScriptExecutionSuccess);
        Assert.Equal("kept", vm.CommandTextBoxText);
    }

    [AvaloniaFact]
    public async Task RunCommandTextAsync_ReportsCompilationErrorsWithoutView()
    {
        MainViewModel vm = CreateViewModel();

        vm.CommandTextBoxText = "int broken = ;";
        await vm.RunCommandTextAsync();

        Assert.False(vm.Scripting.ScriptExecutionSuccess);
        Assert.Equal("CompilationErrorException", vm.Scripting.ScriptErrorType);
        Assert.Contains("CS1525", vm.CommandTextBoxText);
    }

    [AvaloniaFact]
    public async Task RunCommandTextAsync_ReportsScriptExceptionsWithoutView()
    {
        MainViewModel vm = CreateViewModel();

        vm.CommandTextBoxText = "throw new ScriptException(\"deliberate failure\");";
        await vm.RunCommandTextAsync();

        Assert.False(vm.Scripting.ScriptExecutionSuccess);
        Assert.Equal(nameof(ScriptException), vm.Scripting.ScriptErrorType);
        Assert.Equal("deliberate failure", vm.CommandTextBoxText);
    }

    [AvaloniaFact]
    public async Task SetUMTConsoleText_CanRunFromBackgroundThread()
    {
        MainViewModel vm = CreateViewModel();
        ScriptGlobals globals = new(vm.Scripting, null);

        await Task.Run(() => globals.SetUMTConsoleText("from background"));

        Assert.Equal("from background", vm.CommandTextBoxText);
    }

    [AvaloniaFact]
    public void ViewBackedScriptHelpers_ReturnFallbacksWithoutView()
    {
        MainViewModel vm = CreateViewModel();
        ScriptGlobals globals = new(vm.Scripting, null);

        Assert.Null(globals.PromptChooseDirectory());
        Assert.Null(globals.PromptLoadFile(null, null));
        Assert.Null(globals.PromptSaveFile("txt", "Text files|*.txt"));
        Assert.Null(globals.ScriptInputDialog("Title", "Label", "default", "Cancel", "OK", false, false));
        Assert.Null(globals.SimpleTextInput("Title", "Label", "default", false));
        Assert.False(globals.ScriptQuestion("Question?"));

        globals.ScriptMessage("Message");
        globals.ScriptWarning("Warning");
        globals.ScriptOpenURL("https://example.com");
        globals.SetProgressBar();
        globals.SetProgressBar("Message", "Status", 1, 10);
        globals.SimpleTextOutput("Title", "Label", "Message", false);
    }

    [AvaloniaFact]
    public async Task ProgressUpdater_TracksParallelProgressAndStops()
    {
        MainViewModel vm = CreateViewModel();
        ScriptGlobals globals = new(vm.Scripting, null);

        globals.SetProgress(0);
        globals.StartProgressBarUpdater();
        globals.IncrementProgressParallel();
        globals.AddProgressParallel(4);
        await globals.StopProgressBarUpdater();

        Assert.Equal(5, globals.GetProgress());
        Assert.True(vm.Scripting.ScriptExecutionSuccess);
    }

    [Fact]
    public void CreateFilePickerTypes_ParsesWpfStyleScriptFilters()
    {
        IReadOnlyList<FilePickerFileType> types = Scripting.CreateFilePickerTypes(
            "Sound files (*.WAV;*.OGG)|*.WAV;*.OGG|All files|*");

        Assert.Equal(2, types.Count);
        Assert.Equal("Sound files (*.WAV;*.OGG)", types[0].Name);
        Assert.Equal(["*.WAV", "*.OGG"], types[0].Patterns);
        Assert.Equal("All files", types[1].Name);
        Assert.Equal(["*.*"], types[1].Patterns);
    }

    [Fact]
    public void CreateFilePickerTypes_FallsBackForNullScriptFilters()
    {
        IReadOnlyList<FilePickerFileType> types = Scripting.CreateFilePickerTypes(null, FilePickerFileTypes.Data);

        Assert.Same(FilePickerFileTypes.Data, types);
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

    private static string FindRepositoryFile(string relativePath)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string path = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(path))
                return path;

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find {relativePath}.");
    }
}
