using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.DependencyInjection;
using Underanalyzer.Decompiler;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using UndertaleModLib.Project;
using UndertaleModLib.Scripting;
using UndertaleModLib.Util;

namespace UndertaleModToolAvalonia;

public class Scripting
{
    public readonly MainViewModel MainVM;

    public bool ScriptExecutionSuccess { get; private set; } = true;
    public string ScriptErrorMessage { get; private set; } = "";
    public string ScriptErrorType { get; private set; } = "";
    public bool FinishedMessageEnabled { get; private set; } = true;

    public Scripting(IServiceProvider serviceProvider)
    {
        MainVM = serviceProvider.GetRequiredService<MainViewModel>();
    }

    public async Task<object?> RunScript(string text, string? filePath = null)
    {
        try
        {
            SetScriptSuccess();
            FinishedMessageEnabled = true;
            MainVM.IsEnabled = false;

            Script<object?> script = CreateScript(text, filePath);

            ImmutableArray<Diagnostic> diagnostics = await Task.Run(() => script.Compile());

            IEnumerable<Diagnostic> errors = diagnostics.Where((Diagnostic diagnostic) => diagnostic.Severity == DiagnosticSeverity.Error);
            if (errors.Any())
            {
                string message = String.Join("\n", errors);
                SetScriptError("CompilationErrorException", message);
                await ShowScriptDialog(message, "Script compilation error");

                return null;
            }

            ScriptGlobals scripting = new(this, filePath);

            try
            {
                ScriptState<object?> state = await script.RunAsync(scripting);
                return state.ReturnValue;
            }
            catch (ScriptException e)
            {
                string message = e.Message;
                SetScriptError(e.GetType().Name, message);
                await ShowScriptDialog(message, "Error from script");
            }
            catch (Exception e)
            {
                string message = ScriptingUtil.PrettifyException(in e);
                SetScriptError(e.GetType().Name, message);
                await ShowScriptDialog(message, "Script execution error");
            }
            finally
            {
                scripting.Dispose();
            }
        }
        finally
        {
            MainVM.IsEnabled = true;
        }

        return null;
    }

    internal Script<object?> CreateScript(string text, string? filePath = null)
    {
        return CSharpScript.Create(text, ScriptingUtil.CreateDefaultScriptOptions()
            .AddImports(
                "System.Linq",
                "System.Text",
                "System.Threading.Tasks")
            .WithFilePath(filePath)
            .WithFileEncoding(filePath is null ? Encoding.Default : Encoding.UTF8)
            .WithEmitDebugInformation(true),
            typeof(IScriptInterface));
    }

    internal bool LintScript(string text, string? filePath, out string message)
    {
        ImmutableArray<Diagnostic> diagnostics = CreateScript(text, filePath).Compile();
        Diagnostic[] errors = diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
        if (errors.Length == 0)
        {
            SetScriptSuccess();
            message = "";
            return true;
        }

        message = String.Join("\n", errors.Select(error => error.ToString()));
        SetScriptError("CompilationErrorException", message);
        return false;
    }

    internal void SetScriptSuccess()
    {
        ScriptExecutionSuccess = true;
        ScriptErrorMessage = "";
        ScriptErrorType = "";
    }

    internal void SetScriptError(string type, string message)
    {
        ScriptExecutionSuccess = false;
        ScriptErrorMessage = message;
        ScriptErrorType = type;
    }

    internal void SetFinishedMessage(bool isEnabled)
    {
        FinishedMessageEnabled = isEnabled;
    }

    public bool ConsumeFinishedMessageEnabled()
    {
        bool result = FinishedMessageEnabled;
        FinishedMessageEnabled = true;
        return result;
    }

    private async Task ShowScriptDialog(string message, string title)
    {
        if (MainVM.View is not null)
            await MainVM.View.MessageDialog(message, title: title);
    }

    internal static IReadOnlyList<FilePickerFileType> CreateFilePickerTypes(string? filter, IReadOnlyList<FilePickerFileType>? fallback = null)
    {
        if (String.IsNullOrWhiteSpace(filter))
            return fallback ?? FilePickerFileTypes.All;

        string[] parts = filter.Split('|', StringSplitOptions.TrimEntries);
        if (parts.Length < 2)
            return fallback ?? FilePickerFileTypes.All;

        List<FilePickerFileType> fileTypes = [];
        for (int i = 0; i + 1 < parts.Length; i += 2)
        {
            string name = String.IsNullOrWhiteSpace(parts[i]) ? "Files" : parts[i];
            List<string> patterns = parts[i + 1]
                .Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(NormalizeFilterPattern)
                .Where(pattern => !String.IsNullOrWhiteSpace(pattern))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (patterns.Count == 0)
                continue;

            fileTypes.Add(new FilePickerFileType(name)
            {
                Patterns = patterns,
            });
        }

        return fileTypes.Count > 0 ? fileTypes : fallback ?? FilePickerFileTypes.All;
    }

    private static string NormalizeFilterPattern(string pattern)
    {
        if (pattern == "*")
            return "*.*";

        if (pattern.StartsWith("*.") || pattern.StartsWith('*'))
            return pattern;

        string trimmed = pattern.TrimStart('.');
        return $"*.{trimmed}";
    }
}

public class ScriptGlobals : IScriptInterface, IDisposable
{
    private readonly Scripting scripting;
    private readonly MainViewModel mainVM;
    private readonly string? scriptPath;

    private ILoaderWindow? loaderWindow;
    private int loaderValue;
    private CancellationTokenSource? progressUpdaterCts;
    private Task? progressUpdater;

    public ScriptGlobals(Scripting scripting, string? scriptPath)
    {
        this.scripting = scripting;
        mainVM = scripting.MainVM;
        this.scriptPath = scriptPath;
    }

    public void Dispose()
    {
        progressUpdaterCts?.Cancel();
        progressUpdaterCts?.Dispose();
        progressUpdaterCts = null;
        progressUpdater = null;
        loaderWindow?.Close();
        loaderWindow = null;
    }

    public UndertaleData? Data => mainVM.Data;

    public ProjectContext? Project => mainVM.Project;

    public string? FilePath => mainVM.DataPath;

    public string? ScriptPath => scriptPath;

    public object? Highlighted => Selected;

    public object? Selected => mainVM.TabSelected?.Content is IUndertaleResourceViewModel resourceViewModel
        ? resourceViewModel.Resource
        : mainVM.TabSelected?.Content;

    public bool CanSave => mainVM.Data is not null;

    public bool ScriptExecutionSuccess => scripting.ScriptExecutionSuccess;

    public string ScriptErrorMessage => scripting.ScriptErrorMessage;

    public string? ExePath => Path.GetDirectoryName(Environment.ProcessPath);

    public string ScriptErrorType => scripting.ScriptErrorType;

    public bool IsAppClosed => false;

    public Action<Action> MainThreadAction => Dispatcher.UIThread.Invoke;

    public void AddProgress(int amount)
    {
        loaderValue += amount;

        Dispatcher.UIThread.Post(() =>
        {
            loaderWindow?.SetValue(loaderValue);
        });
    }

    public void AddProgressParallel(int amount)
    {
        Interlocked.Add(ref loaderValue, amount);

        if (progressUpdaterCts is null)
        {
            Dispatcher.UIThread.Post(() =>
            {
                loaderWindow?.SetValue(loaderValue);
            }, DispatcherPriority.Background);
        }
    }

    public void ChangeSelection(object newSelection, bool inNewTab = false)
    {
        Dispatcher.UIThread.Invoke(() => mainVM.TabOpen(newSelection, inNewTab));
    }

    public Task ClickableSearchOutput(string title, string query, int resultsCount, IOrderedEnumerable<KeyValuePair<string, List<(int lineNum, string codeLine)>>> resultsDict, bool showInDecompiledView, IOrderedEnumerable<string>? failedList = null)
    {
        return ShowSearchOutput(title, query, resultsCount, resultsDict, failedList);
    }

    public Task ClickableSearchOutput(string title, string query, int resultsCount, IDictionary<string, List<(int lineNum, string codeLine)>> resultsDict, bool showInDecompiledView, IEnumerable<string>? failedList = null)
    {
        return ShowSearchOutput(title, query, resultsCount, resultsDict, failedList);
    }

    public void EnableUI()
    {
        mainVM.IsEnabled = true;
    }

    public string GetDecompiledText(string codeName, GlobalDecompileContext? context = null, IDecompileSettings? settings = null)
    {
        return GetDecompiledText(mainVM.Data!.Code.ByName(codeName), context, settings);
    }

    public string GetDecompiledText(UndertaleCode code, GlobalDecompileContext? context = null, IDecompileSettings? settings = null)
    {
        context ??= new(mainVM.Data);
        settings ??= mainVM.Data!.ToolInfo.DecompilerSettings;

        return new DecompileContext(context, code, settings).DecompileToString();
    }

    public string GetDisassemblyText(string codeName)
    {
        return GetDisassemblyText(mainVM.Data!.Code.ByName(codeName));
    }

    public string GetDisassemblyText(UndertaleCode code)
    {
        return code.Disassemble(mainVM.Data!.Variables, mainVM.Data!.CodeLocals?.For(code));
    }

    public int GetProgress()
    {
        return loaderValue;
    }

    public void HideProgressBar()
    {
        loaderWindow?.Close();
        loaderWindow = null;
    }

    public void IncrementProgress()
    {
        loaderValue++;

        Dispatcher.UIThread.Post(() =>
        {
            loaderWindow?.SetValue(loaderValue);
        });
    }

    public void IncrementProgressParallel()
    {
        Interlocked.Increment(ref loaderValue);

        if (progressUpdaterCts is null)
        {
            Dispatcher.UIThread.Post(() =>
            {
                loaderWindow?.SetValue(loaderValue);
            }, DispatcherPriority.Background);
        }
    }

    public void InitializeScriptDialog()
    {
        SetProgressBar();
    }

    public bool LintUMTScript(string path)
    {
        if (!File.Exists(path))
        {
            string message = $"{path} does not exist!";
            scripting.SetScriptError("FileNotFoundException", message);
            ScriptError(message);
            return false;
        }

        string scriptText = File.ReadAllText(path, Encoding.UTF8);
        if (scripting.LintScript(scriptText, path, out string errorMessage))
            return true;

        ScriptError(errorMessage, "Script compile error");
        return false;
    }

    public bool MakeNewDataFile()
    {
        return RunOnMainThread(() => mainVM.NewData());
    }

    public string? PromptChooseDirectory()
    {
        if (mainVM.View is not { } view)
            return null;

        IReadOnlyList<IStorageFolder> folders = RunOnMainThread(() => view.OpenFolderDialog(new()
        {
            Title = "Select directory",
        }));

        if (folders.Count != 1)
            return null;

        string? path = folders[0].TryGetLocalPath();
        if (path is null)
            return null;

        return Path.EndsInDirectorySeparator(path) ? path : path + Path.DirectorySeparatorChar;
    }

    public string? PromptLoadFile(string? defaultExt, string? filter)
    {
        if (mainVM.View is not { } view)
            return null;

        var files = RunOnMainThread(() => view.OpenFileDialog(new FilePickerOpenOptions()
        {
            Title = "Load file",
            FileTypeFilter = Scripting.CreateFilePickerTypes(filter, FilePickerFileTypes.Data),
        }));

        if (files.Count != 1)
            return null;

        return files[0].TryGetLocalPath();
    }

    public string? PromptSaveFile(string defaultExt, string filter)
    {
        if (mainVM.View is not { } view)
            return null;

        var file = RunOnMainThread(() => view.SaveFileDialog(new FilePickerSaveOptions()
        {
            Title = "Save file",
            FileTypeChoices = Scripting.CreateFilePickerTypes(filter, FilePickerFileTypes.Data),
            DefaultExtension = defaultExt,
        }));

        if (file is null)
            return null;

        return file.TryGetLocalPath();
    }

    public bool RunUMTScript(string path)
    {
        if (!File.Exists(path))
        {
            string message = $"{path} does not exist!";
            scripting.SetScriptError("FileNotFoundException", message);
            ScriptError(message);
            return false;
        }

        string scriptText = $"#line 1 \"{path}\"\n" + File.ReadAllText(path, Encoding.UTF8);
        RunOnMainThread(() => scripting.RunScript(scriptText, path));
        return scripting.ScriptExecutionSuccess;
    }

    public void ScriptError(string error, string title = "Error", bool SetConsoleText = true)
    {
        ShowMessageDialogIfPossible(error, title);

        if (SetConsoleText)
        {
            SetUMTConsoleText(error);
            SetFinishedMessage(false);
        }
    }

    public string? ScriptInputDialog(string title, string label, string defaultInput, string cancelText, string submitText, bool isMultiline, bool preventClose)
    {
        // TODO: cancelText, submitText, preventClose
        if (mainVM.View is not { } view)
            return null;

        return RunOnMainThread(() => view.TextBoxDialog(label, defaultInput, title: title, isMultiline: isMultiline));
    }

    public void ScriptMessage(string message)
    {
        ShowMessageDialogIfPossible(message, "Script message");
    }

    public void ScriptOpenURL(string url)
    {
        if (mainVM.View is not { } view)
            return;

        RunOnMainThread(() => view.LaunchUriAsync(new(url)));
    }

    public bool ScriptQuestion(string message)
    {
        if (mainVM.View is not { } view)
            return false;

        return RunOnMainThread(() => view.MessageDialog(message, "Script question", MessageWindow.Buttons.YesNo)) == MessageWindow.Result.Yes;
    }

    public void ScriptWarning(string message)
    {
        ShowMessageDialogIfPossible(message, "Script warning");
    }

    public void SetFinishedMessage(bool isFinishedMessageEnabled)
    {
        scripting.SetFinishedMessage(isFinishedMessageEnabled);
    }

    public void SetProgress(int value)
    {
        loaderValue = value;

        Dispatcher.UIThread.Post(() =>
        {
            loaderWindow?.SetValue(loaderValue);
        });
    }

    public void SetProgressBar(string message, string status, double progressValue, double maxValue)
    {
        if (mainVM.View is not { } view)
            return;

        loaderValue = (int)progressValue;

        Dispatcher.UIThread.Invoke(() =>
        {
            loaderWindow ??= view.LoaderOpen();
            loaderWindow.EnsureShown();
            loaderWindow.SetMessage(message);
            loaderWindow.SetStatus(status);
            loaderWindow.SetValue(loaderValue);
            loaderWindow.SetMaximum((int)maxValue);
        });
    }

    public void SetProgressBar()
    {
        if (mainVM.View is not { } view)
            return;

        Dispatcher.UIThread.Invoke(() =>
        {
            loaderWindow ??= view.LoaderOpen();
            loaderWindow.EnsureShown();
        });
    }

    public void SetUMTConsoleText(string message)
    {
        RunOnMainThread(() => mainVM.CommandTextBoxText = message);
    }

    public string? SimpleTextInput(string title, string label, string defaultValue, bool allowMultiline, bool showDialog = true)
    {
        // TODO: showDialog
        if (mainVM.View is not { } view)
            return null;

        return RunOnMainThread(() => view.TextBoxDialog(label, defaultValue, title: title, isMultiline: allowMultiline));
    }

    public void SimpleTextOutput(string title, string label, string message, bool allowMultiline)
    {
        if (mainVM.View is not { } view)
            return;

        RunOnMainThread(() => view.TextBoxDialog(label, message, title: title, isMultiline: allowMultiline, isReadOnly: true));
    }

    public void StartProgressBarUpdater()
    {
        if (progressUpdaterCts is not null)
        {
            ScriptWarning("Warning - there is another progress bar updater task running in the background.");
            return;
        }

        progressUpdaterCts = new CancellationTokenSource();
        progressUpdater = Task.Run(() => ProgressUpdater(progressUpdaterCts.Token));
    }

    public async Task StopProgressBarUpdater()
    {
        if (progressUpdaterCts is null || progressUpdater is null)
            return;

        CancellationTokenSource cts = progressUpdaterCts;
        Task updater = progressUpdater;

        cts.Cancel();

        Task completed = await Task.WhenAny(updater, Task.Delay(2000));
        if (completed != updater)
        {
            ScriptError("Stopping the progress bar updater task failed. Restarting the application is recommended.", "Script error", SetConsoleText: false);
            return;
        }

        await updater;
        cts.Dispose();
        progressUpdaterCts = null;
        progressUpdater = null;
    }

    public void UpdateProgressBar(string message, string status, double progressValue, double maxValue)
    {
        SetProgressBar(message, status, progressValue, maxValue);
    }

    public void UpdateProgressStatus(string status)
    {
        Dispatcher.UIThread.Post(() =>
        {
            loaderWindow?.SetTextToMessageAndStatus(status: status);
        });
    }

    public void UpdateProgressValue(double progressValue)
    {
        loaderValue = (int)progressValue;

        Dispatcher.UIThread.Post(() =>
        {
            loaderWindow?.SetValue(loaderValue);
        });
    }

    private Task ShowSearchOutput(string title, string query, int resultsCount, IEnumerable<KeyValuePair<string, List<(int lineNum, string codeLine)>>> resultsDict, IEnumerable<string>? failedList)
    {
        StringBuilder output = new();
        output.AppendLine($"Query: {query}");
        output.AppendLine($"Results: {resultsCount}");
        output.AppendLine();

        foreach ((string codeName, List<(int lineNum, string codeLine)> results) in resultsDict)
        {
            output.AppendLine(codeName);
            foreach ((int lineNum, string codeLine) in results)
            {
                output.Append("  ");
                output.Append(lineNum);
                output.Append(": ");
                output.AppendLine(codeLine);
            }
            output.AppendLine();
        }

        if (failedList is not null)
        {
            string[] failures = failedList.ToArray();
            if (failures.Length > 0)
            {
                output.AppendLine("Failed:");
                foreach (string failed in failures)
                    output.AppendLine($"  {failed}");
            }
        }

        if (mainVM.View is not { } view)
        {
            SetUMTConsoleText(output.ToString());
            return Task.CompletedTask;
        }

        RunOnMainThread(() => view.TextBoxDialog("Search results", output.ToString(), title: title, isMultiline: true, isReadOnly: true));
        return Task.CompletedTask;
    }

    private void ShowMessageDialogIfPossible(string message, string title)
    {
        if (mainVM.View is null)
            return;

        RunOnMainThread(() => mainVM.View.MessageDialog(message, title));
    }

    private void ProgressUpdater(CancellationToken token)
    {
        Stopwatch frameTimer = new();
        Stopwatch? stopTimeout = null;
        int previousValue = Volatile.Read(ref loaderValue);

        while (true)
        {
            frameTimer.Restart();

            int currentValue = Volatile.Read(ref loaderValue);
            UpdateProgressValue(currentValue);

            if (token.IsCancellationRequested)
            {
                if (previousValue >= currentValue)
                    return;

                stopTimeout ??= Stopwatch.StartNew();
                if (stopTimeout.ElapsedMilliseconds >= 500)
                    return;
            }
            else if (currentValue != previousValue)
            {
                stopTimeout = null;
            }

            previousValue = currentValue;

            int sleep = (int)Math.Max(0, 33 - frameTimer.ElapsedMilliseconds);
            if (sleep > 0)
                Thread.Sleep(sleep);
        }
    }

    private T RunOnMainThread<T>(Func<T> action)
    {
        if (Dispatcher.UIThread.CheckAccess())
            return action();

        return Dispatcher.UIThread.Invoke(action);
    }

    private T RunOnMainThread<T>(Func<Task<T>> action)
    {
        if (Dispatcher.UIThread.CheckAccess())
            return action().WaitOnDispatcherFrame();

        return Dispatcher.UIThread.Invoke(() => action().WaitOnDispatcherFrame());
    }

}
