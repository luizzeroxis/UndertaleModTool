using Avalonia.Controls;

namespace UndertaleModToolAvalonia;

/// <summary>
/// Asks what to do after the text of a string reference was edited, because other resources may share that string.
/// </summary>
public partial class StringUpdateWindow : Window
{
    public enum Result
    {
        // First, so closing the window with its X button aborts the change.
        Cancel = 0,
        // Point this reference to a string with the new text, leaving the old string as it is.
        ChangeOneValue,
        // Edit the string itself, which changes every resource that references it.
        ChangeReferencedValue,
    }

    public StringUpdateWindow()
    {
        InitializeComponent();
    }

    public void ChangeOneValueClick()
    {
        Close(Result.ChangeOneValue);
    }

    public void ChangeReferencedValueClick()
    {
        Close(Result.ChangeReferencedValue);
    }

    public void CancelClick()
    {
        Close(Result.Cancel);
    }
}
