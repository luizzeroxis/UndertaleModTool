using System;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.Tests;

public class ControlTest
{
    [AvaloniaFact]
    public void EditableDataGrid_AddThrowsActionableErrorWithoutItemFactory()
    {
        EditableDataGrid grid = new();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(grid.Add);

        Assert.Contains("ItemFactory", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Func<object>", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FlagEnumToStringConverter_ConvertBackWithoutViewReturnsDoNothing()
    {
        FlagEnumToStringConverter converter = new();

        object? result = converter.ConvertBack("Regular", typeof(TestFlags), null, System.Globalization.CultureInfo.InvariantCulture);

        Assert.Same(BindingOperations.DoNothing, result);
    }

    [AvaloniaFact]
    public void FlagsBoxView_CheckChangedWithoutValueDoesNotThrow()
    {
        FlagsBoxView view = new();
        CheckBox checkBox = new()
        {
            IsChecked = true,
            DataContext = new FlagsBoxView.Flag(TestFlags.Regular, nameof(TestFlags.Regular), false),
        };

        view.Checked_IsCheckChanged(checkBox, new RoutedEventArgs());

        Assert.Null(view.Value);
    }

    [AvaloniaFact]
    public void ResourceReferenceView_OpenWithoutMainViewDoesNotThrow()
    {
        UndertaleResourceReferenceView view = new()
        {
            ReferenceType = typeof(UndertaleCode),
        };

        view.Open();
        view.OpenInNewTab();
    }

    [AvaloniaFact]
    public void StringReferenceView_OpenWithoutMainViewDoesNotThrow()
    {
        UndertaleStringReferenceView view = new();

        view.Open();
        view.OpenInNewTab();
    }

    [AvaloniaFact]
    public void RoomEditor_NonRoomDataContextDoesNotThrow()
    {
        UndertaleRoomEditor editor = new()
        {
            DataContext = new object(),
        };

        editor.DataContext = null;
    }

    [Flags]
    private enum TestFlags
    {
        None = 0,
        Regular = 1,
    }
}
