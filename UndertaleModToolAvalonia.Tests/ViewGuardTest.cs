using System;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;

namespace UndertaleModToolAvalonia.Tests;

public class ViewGuardTest
{
    [AvaloniaFact]
    public void RequireTopLevel_ThrowsActionableErrorWhenViewIsDetached()
    {
        UserControl view = new();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => view.RequireTopLevel("TestOperation"));

        Assert.Contains("TestOperation", exception.Message, StringComparison.Ordinal);
        Assert.Contains("top-level window", exception.Message, StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public void RequireWindow_ThrowsActionableErrorWhenViewIsDetached()
    {
        UserControl view = new();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => view.RequireWindow("TestOperation"));

        Assert.Contains("TestOperation", exception.Message, StringComparison.Ordinal);
        Assert.Contains("window", exception.Message, StringComparison.Ordinal);
    }
}
