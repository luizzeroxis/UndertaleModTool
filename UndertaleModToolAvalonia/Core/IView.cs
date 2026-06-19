using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace UndertaleModToolAvalonia;

public interface IView
{
    private Control View => (Control)this;

    public async Task<IReadOnlyList<IStorageFile>> OpenFileDialog(FilePickerOpenOptions options)
    {
        TopLevel topLevel = View.RequireTopLevel(nameof(OpenFileDialog));
        return await topLevel.StorageProvider.OpenFilePickerAsync(options);
    }

    public async Task<IStorageFile?> SaveFileDialog(FilePickerSaveOptions options)
    {
        TopLevel topLevel = View.RequireTopLevel(nameof(SaveFileDialog));
        return await topLevel.StorageProvider.SaveFilePickerAsync(options);
    }

    public async Task<IReadOnlyList<IStorageFolder>> OpenFolderDialog(FolderPickerOpenOptions options)
    {
        TopLevel topLevel = View.RequireTopLevel(nameof(OpenFolderDialog));
        return await topLevel.StorageProvider.OpenFolderPickerAsync(options);
    }

    public async Task<bool> LaunchUriAsync(Uri uri)
    {
        TopLevel topLevel = View.RequireTopLevel(nameof(LaunchUriAsync));
        return await topLevel.Launcher.LaunchUriAsync(uri);
    }

    public async Task<MessageWindow.Result> MessageDialog(string message, string? title = null, MessageWindow.Buttons buttons = MessageWindow.Buttons.OK)
    {
        Window window = View.RequireWindow(nameof(MessageDialog));
        return await new MessageWindow(message, title, buttons).ShowDialog<MessageWindow.Result>(window);
    }

    public async Task<string?> TextBoxDialog(string message, string text = "", string? title = null, bool isMultiline = false, bool isReadOnly = false)
    {
        Window window = View.RequireWindow(nameof(TextBoxDialog));
        return await new TextBoxWindow(message, text, title, isMultiline, isReadOnly).ShowDialog<string?>(window);
    }

    public ILoaderWindow LoaderOpen()
    {
        Window window = View.RequireWindow(nameof(LoaderOpen), true);
        LoaderWindow loaderWindow = new();
        loaderWindow.ShowDelayed(window);
        return loaderWindow;
    }

    public IInputElement? GetFocusedElement()
    {
        TopLevel topLevel = View.RequireTopLevel(nameof(GetFocusedElement));
        return topLevel.FocusManager?.GetFocusedElement();
    }
}

internal static class ViewGuardExtensions
{
    public static TopLevel RequireTopLevel(this Control view, string operation)
    {
        return TopLevel.GetTopLevel(view)
            ?? throw new InvalidOperationException($"{operation} requires the view to be attached to a top-level window.");
    }

    public static Window RequireWindow(this Control view, string operation, bool includeSelf = false)
    {
        return view.FindLogicalAncestorOfType<Window>(includeSelf)
            ?? throw new InvalidOperationException($"{operation} requires the view to be attached to a window.");
    }
}
