using System;
using System.IO;
using System.Linq;
using Avalonia.Controls;

namespace UndertaleModToolAvalonia;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        BuildScriptsMenu();
    }

    private void BuildScriptsMenu()
    {
        NativeMenu? rootMenu = NativeMenu.GetMenu(this) ?? NativeDock.GetMenu(this);
        NativeMenuItem? rootScriptItem = rootMenu?.Items.OfType<NativeMenuItem>()
            .FirstOrDefault(i => i.Header?.ToString() == "_Scripts");
        if (rootScriptItem is null)
            return;

        NativeMenu menu = new();
        PopulateScriptsMenu(menu, Path.Combine(AppContext.BaseDirectory, "Scripts"), isRoot: true);
        rootScriptItem.Menu = menu;
    }

    private void PopulateScriptsMenu(NativeMenu menu, string folderDir, bool isRoot)
    {
        menu.Items.Clear();

        try
        {
            DirectoryInfo directory = new(folderDir);

            if (!directory.Exists)
            {
                menu.Items.Add(new NativeMenuItem
                {
                    Header = $"(Path {folderDir} does not exist, cannot search for files!)",
                    IsEnabled = false,
                });
            }
            else
            {
                foreach (FileInfo file in directory.EnumerateFiles("*.csx").OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase))
                {
                    string scriptPath = file.FullName;
                    NativeMenuItem item = new()
                    {
                        Header = EscapeMenuHeader(file.Name),
                    };
                    item.Click += (_, _) => RunBuiltinScript(scriptPath);
                    menu.Items.Add(item);
                }

                foreach (DirectoryInfo subDirectory in directory.EnumerateDirectories().OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase))
                {
                    if (!subDirectory.EnumerateFiles("*.csx").Any())
                        continue;

                    NativeMenu subMenu = new();
                    PopulateScriptsMenu(subMenu, subDirectory.FullName, isRoot: false);
                    menu.Items.Add(new NativeMenuItem
                    {
                        Header = EscapeMenuHeader(subDirectory.Name),
                        Menu = subMenu,
                    });
                }

                if (menu.Items.Count == 0)
                {
                    menu.Items.Add(new NativeMenuItem
                    {
                        Header = "(No scripts found!)",
                        IsEnabled = false,
                    });
                }
            }
        }
        catch (Exception e)
        {
            menu.Items.Add(new NativeMenuItem
            {
                Header = e.Message,
                IsEnabled = false,
            });
        }

        if (isRoot)
        {
            menu.Items.Add(new NativeMenuItemSeparator());

            NativeMenuItem otherScripts = new()
            {
                Header = "Run _other script...",
            };
            otherScripts.Click += (_, _) =>
            {
                if (DataContext is MainViewModel vm)
                    vm.ScriptsRunOtherScript();
            };
            menu.Items.Add(otherScripts);
        }
    }

    private static string EscapeMenuHeader(string header)
    {
        return header.Replace("_", "__");
    }

    private async void RunBuiltinScript(string path)
    {
        if (DataContext is MainViewModel vm)
            await vm.RunScriptFileAsync(path);
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (!e.IsProgrammatic)
        {
            if (DataContext is MainViewModel vm && vm.Data is not null)
            {
                e.Cancel = true;

                async void AskSaveBeforeClose()
                {
                    if (await vm.AskProjectSave("There are assets marked to be exported in the current project. Save project before quitting?")
                        && await vm.AskFileSave("Save data file before quitting?"))
                        Close();
                }

                AskSaveBeforeClose();
            }
        }

        base.OnClosing(e);
    }
}
