using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ExtendedDataEntry;

public partial class DataEntry : ContentView
{
    private const char CursorLabelText = '█';
    private const char cursorInsertText = 'ꕯ';
    private string totalText;

    public static readonly BindableProperty MaxCharProperty =
        BindableProperty.Create(nameof(MaxChar), typeof(int), typeof(Label), 6);

    public int MaxChar
    {
        get => (int)GetValue(MaxCharProperty);
        set => SetValue(MaxCharProperty, value);
    }

    public static readonly BindableProperty SavedTextProperty =
        BindableProperty.Create(nameof(SavedText), typeof(string), typeof(Label), string.Empty);

    public string SavedText
    {
        get => (string)GetValue(SavedTextProperty);
        set => SetValue(SavedTextProperty, value);
    }

    public static readonly BindableProperty ProposedTextProperty =
        BindableProperty.Create(nameof(ProposedText), typeof(string), typeof(Label), string.Empty);

    public string ProposedText
    {
        get => (string)GetValue(ProposedTextProperty);
        set => SetValue(ProposedTextProperty, value);
    }

    public static readonly BindableProperty EntryStateProperty =
        BindableProperty.Create(nameof(EntryState), typeof(EntryStates), typeof(Label), EntryStates.Locked,
            BindingMode.TwoWay);

    public EntryStates EntryState
    {
        get => (EntryStates)GetValue(EntryStateProperty);
        set => SetValue(EntryStateProperty, value);
    }

    private static void OnEntryStateChanged(BindableObject bindable, object oldValue, object newValue)
    {
        Debug.WriteLine("test");
        if (bindable is DataEntry dataEntry)
        {
            //
        }
    }





    public enum EntryStates
    {
        Locked,
        Highlight,
        Edit
    }



    public DataEntry()
    {
        InitializeComponent();
    }



    private static CancellationTokenSource? _blinkingTokenSource;

    public static CancellationTokenSource? BlinkingTokenSource
    {
        get => _blinkingTokenSource;
        set => _blinkingTokenSource = value;
    }


    public async Task ToggleSpanVisibility(Label parentLabel, string spanName = "")
    {
        // Cancel any existing blinking task
        _blinkingTokenSource?.Cancel();
        _blinkingTokenSource = new CancellationTokenSource();
        CancellationToken token = _blinkingTokenSource.Token;

        try
        {
            while (!token.IsCancellationRequested)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    // Find the span with the specified name
                    if (parentLabel.FormattedText is FormattedString formattedString)
                    {
                        var targetSpan = formattedString.Spans.FirstOrDefault(s => s.StyleId == spanName);
                        if (targetSpan != null)
                        {
                            // Toggle visibility by changing the text color
                            targetSpan.TextColor = targetSpan.TextColor == Colors.Transparent
                                ? Colors.Black // Replace with your desired visible color
                                : Colors.Transparent;
                        }
                    }
                    //Label itself will be blinking
                    else
                    {
                        parentLabel.TextColor = parentLabel.TextColor == Colors.Transparent
                            ? Colors.Black
                            : Colors.Transparent;
                        
                        // parentLabel.FormattedText.Spans[0].TextColor = parentLabel.TextColor == Colors.Transparent
                        //     ? Colors.Black
                        //     : Colors.Transparent;
                        
                    }
                });

                // Wait for 700ms before toggling again
                await Task.Delay(700, token);
            }
        }
        catch (TaskCanceledException)
        {
            // Task was cancelled, which is expected behavior
        }
        finally
        {
            // Ensure the span is visible when the task ends
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (parentLabel.FormattedText is FormattedString formattedString)
                {
                    var targetSpan = formattedString.Spans.FirstOrDefault(s => s.StyleId == spanName);
                    if (targetSpan != null)
                    {
                        targetSpan.TextColor = Colors.Black; // Replace with the default visible color
                    }
                }
            });
        }
    }





    private void TapGestureRecognizer_OnTapped(object? sender, TappedEventArgs e)
    {

    }

    async private void Button_OnClicked(object? sender, EventArgs e)
    {
        if (EntryState == EntryStates.Locked) return;
        if (EntryState == EntryStates.Highlight)
        {
            EntryState = EntryStates.Edit;
            Highlighter.IsVisible = false;
            TextCursor.IsVisible = true;
            //await ToggleSpanVisibility(TextCursor, "CursorLabel");
        }

        if (sender is Button button && EntryState == EntryStates.Edit)
        {
            if (totalText.Length < MaxChar)
                DraftTextSpan.Text += button.Text;
            else
                DraftTextSpan.Text = DraftTextSpan.Text.Substring(0, DraftTextSpan.Text.Length - 1) + button.Text;

        }
    }

    private void Button_OnClickedSpecial(object? sender, EventArgs e)
    {
        if (EntryState != EntryStates.Edit)
            return;

        if (sender is not Button button || button.CommandParameter is not string command)
            return;

        switch (command)
        {
            case "BACKSPACE":
                // Remove the last character from DraftTextSpan.Text if it exists
                DraftTextSpan.Text = !string.IsNullOrEmpty(DraftTextSpan.Text)
                    ? DraftTextSpan.Text[..^1]
                    : string.Empty;
                break;

            case "STORE":
                _blinkingTokenSource?.Cancel();
                // Send the text proposal via binding to the ViewModel for verification and storing
                ProposedText = totalText;
                // now we have to wait for the answer of the viewmodel
                // if the value is accepted, the viewmodel will set the EntryState to Locked
                // TODO: this change is not detected by the view
                break;

            case "→":
                //Check that there's character after cursor
                if (!string.IsNullOrEmpty(DraftTextAfterSpan.Text))
                {
                    //Pass character before cursorr
                    char transChar = DraftTextAfterSpan.Text.First();
                    DraftTextAfterSpan.Text = DraftTextAfterSpan.Text.Remove(0, 1);
                    DraftTextSpan.Text += transChar;
                }

                break;

            case "←":
                //Check that there's character before cursor
                if (!string.IsNullOrEmpty(DraftTextSpan.Text))
                {
                    //Pass character after cursorr
                    char transChar = DraftTextSpan.Text.Last();
                    DraftTextSpan.Text = DraftTextSpan.Text.Remove(DraftTextSpan.Text.Length - 1, 1);
                    DraftTextAfterSpan.Text = transChar + DraftTextAfterSpan.Text;
                }

                break;
            default:
                // Handle other commands if necessary
                break;
        }
    }




    private async void Button_ChangeEditMode(object? sender, EventArgs e)
    {
        if (sender is Button button)
        {
            ResetButtonColors();

            switch (button.CommandParameter)
            {
                case "Highlight":
                    Highlighter.IsVisible = true;
                    TextCursor.IsVisible = false;
                    CursorLabel.IsVisible = false;
                    EntryState = EntryStates.Highlight;
                    ButtonHighlight.BackgroundColor = Colors.Yellow;
                    break;

                case "Edit":
                    Highlighter.IsVisible = false;
                    TextCursor.IsVisible = true;
                    // EnteredText.IsVisible = false;
                    DraftTextSpan.Text = EnteredText.Text;
                    EntryState = EntryStates.Edit;
                    ButtonEdit.BackgroundColor = Colors.Yellow;
                    CursorLabel.IsVisible = true;
                    await ToggleSpanVisibility(CursorLabel);
                    break;

                case "Lock":
                    Highlighter.IsVisible = false;
                    TextCursor.IsVisible = false;
                    CursorLabel.IsVisible = false;
                    // EnteredText.IsVisible = true;
                    _blinkingTokenSource?.Cancel();
                    _blinkingTokenSource = new CancellationTokenSource();
                    EntryState = EntryStates.Locked;
                    ButtonLock.BackgroundColor = Colors.Yellow;
                    break;
            }
        }
    }

    private void ResetButtonColors()
    {
        ButtonHighlight.BackgroundColor = Colors.MediumPurple;
        ButtonEdit.BackgroundColor = Colors.MediumPurple;
        ButtonLock.BackgroundColor = Colors.MediumPurple;
    }

    private void textSpan_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (DraftTextSpan is not null && DraftTextAfterSpan is not null)
        {
            totalText = DraftTextSpan.Text + DraftTextAfterSpan.Text;
            moveCursor();
        }
    }

    private void moveCursor()
    {
        tmpLabel.Text = DraftTextSpan.Text;
        CursorLabel.TranslationY = 4;

        if (totalText.Length < MaxChar)
        {
            CursorLabel.TranslationX = tmpLabel.Measure(151, 115).Width - 4;
            if (!string.IsNullOrEmpty(DraftTextAfterSpan.Text))
                CursorLabel.Text = cursorInsertText.ToString();
        }

        if (totalText.Length == MaxChar || string.IsNullOrEmpty(DraftTextAfterSpan.Text))
        {
            // Clear existing spans in the FormattedString
            CursorLabel.FormattedText = new FormattedString(); // Reset to avoid duplications
            // Create a new span with the text
            var span = new Span
            {
                Text = CursorLabelText.ToString()
            };
            // Add the span to the FormattedString
            CursorLabel.FormattedText.Spans.Add(span);

        }

        // CursorLabel.Text = CursorLabelText.ToString();
    }
}



/*public static readonly BindableProperty MaxCharProperty =
    BindableProperty.Create(nameof(MaxChar), typeof(int), typeof(Label), 6);

public int MaxChar
{
    get => (int)GetValue(MaxCharProperty);
    set => SetValue(MaxCharProperty, value);
}

public static readonly BindableProperty SavedTextProperty =
    BindableProperty.Create(nameof(SavedText), typeof(string), typeof(Label), string.Empty);

public string SavedText
{
    get => (string)GetValue(SavedTextProperty);
    set => SetValue(SavedTextProperty, value);
}

public static readonly BindableProperty ProposedTextProperty =
    BindableProperty.Create(nameof(ProposedText), typeof(string), typeof(Label), string.Empty,
        validateValue: IsValidValue);

public string ProposedText
{
    get => (string)GetValue(ProposedTextProperty);
    set => SetValue(ProposedTextProperty, value);
}

static bool IsValidValue(BindableObject view, object value)
{
    string result;
    Debug.WriteLine("validating....");
    if (value.ToString() == "ABCC")
        return true;
    return false;
}


public static readonly BindableProperty EntryStateProperty =
    BindableProperty.Create(nameof(EntryState), typeof(EntryStates), typeof(Label), EntryStates.Locked,
        BindingMode.TwoWay, propertyChanged: OnEntryStateChanged);


public EntryStates EntryState
{
    get => (EntryStates)GetValue(EntryStateProperty);
    set => SetValue(EntryStateProperty, value);
}

private static void OnEntryStateChanged(BindableObject bindable, object oldValue, object newValue)
{
    if (bindable is DataEntry dataentry)
    {
        Debug.WriteLine("yes it is!");

        if (newValue is EntryStates.Edit)
        {
            dataentry.DraftTextSpan.Text = "";
            dataentry.StartBlinkingCursor(1, true);
        }
        else dataentry.StopBlinkingCursor();
    }


    Debug.WriteLine("test");
    // if (bindable is DataEntry dataEntry)
    // {
    // 	dataEntry.UpdateEntryState();
    // }
}

// {
// 	return result;
// }


protected void UpdateEntryState()
{
    Debug.WriteLine("Updating EntryState changed");
    // Add your logic to handle the state update
}


private int Cursorposition { get; set; } = 0;
public bool CursorInInsertMode { get; set; } = false;

public enum EntryStates
{
    Locked,
    Highlight,
    Edit
}


public DataEntry()
{
    InitializeComponent();
}


private static CancellationTokenSource? _blinkingTokenSource;

public static CancellationTokenSource? BlinkingTokenSource
{
    get => _blinkingTokenSource;
    set => _blinkingTokenSource = value;
}

private FormattedString FormattedStringWithCursor(FormattedString formattedString, int cursorpos,
    bool overwritemode)
{
    var spanbefore = DraftTextSpan.Text.Substring(0, Cursorposition);
    if (DraftTextSpan.Text.Length < MaxChar && Cursorposition != DraftTextSpan.Text.Length)
    {
        Debug.WriteLine("Insert mode at pos" + Cursorposition);
        CursorInInsertMode = true;
    }

    return new FormattedString();
}*/

// 	// Validate the cursor position
//     if (cursorpos < 0 || cursorpos > formattedString.ToString().Length)
//     {
//         throw new ArgumentOutOfRangeException(nameof(cursorpos), "Cursor position is out of range.");
//     }
//
//     // Create a new FormattedString to hold the updated spans
//     var updatedFormattedString = new FormattedString();
//
//     int currentPos = 0;
//
//     foreach (var span in formattedString.Spans)
//     {
//         string spanText = span.Text;
//         int spanLength = spanText.Length;
//
//         if (cursorpos >= currentPos && cursorpos < currentPos + spanLength)
//         {
//             // Cursor is in this span
//             int localCursorPos = cursorpos - currentPos;
//
//             // Add the text before the cursor
//             if (localCursorPos > 0)
//             {
//                 updatedFormattedString.Spans.Add(new Span
//                 {
//                     Text = spanText.Substring(0, localCursorPos),
//                     TextColor = span.TextColor,
//                     BackgroundColor = span.BackgroundColor,
//                     FontAttributes = span.FontAttributes,
//                     FontSize = span.FontSize
//                 });
//             }
//
//             // Add the cursor character
//             updatedFormattedString.Spans.Add(new Span
//             {
//                 Text = spanText.Substring(localCursorPos, 1), // The character at the cursor
//                 TextColor = overwritemode ? Colors.Cyan : span.TextColor, // Cyan for overwritemode
//                 BackgroundColor = span.BackgroundColor,
//                 FontAttributes = span.FontAttributes | FontAttributes.Underline, // Add underline
//                 FontSize = span.FontSize
//             });
//
//             // Add the text after the cursor
//             if (localCursorPos < spanLength - 1)
//             {
//                 updatedFormattedString.Spans.Add(new Span
//                 {
//                     Text = spanText.Substring(localCursorPos + 1),
//                     TextColor = span.TextColor,
//                     BackgroundColor = span.BackgroundColor,
//                     FontAttributes = span.FontAttributes,
//                     FontSize = span.FontSize
//                 });
//             }
//         }
//         else
//         {
//             // Cursor is not in this span; add the entire span unchanged
//             updatedFormattedString.Spans.Add(new Span
//             {
//                 Text = spanText,
//                 TextColor = span.TextColor,
//                 BackgroundColor = span.BackgroundColor,
//                 FontAttributes = span.FontAttributes,
//                 FontSize = span.FontSize
//             });
//         }
//
//         currentPos += spanLength;
//     }
//
//     return updatedFormattedString;
// }

    /*private void Cursorlogic(string newkey, int cursorpos, bool overwritemode)
    {
        // Get the current text in the DraftTextSpan
        var currentText = DraftTextSpan.Text;

        // Validate cursor position to avoid out-of-range errors
        if (cursorpos < 0 || cursorpos > currentText.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(cursorpos), "Cursor position is out of range.");
        }

        // Perform insert or overwrite logic
        if (overwritemode)
        {
            // Overwrite the character at the cursor position
            if (cursorpos < currentText.Length)
            {
                currentText = currentText.Remove(cursorpos, 1).Insert(cursorpos, newkey);
            }
            else
            {
                // Append if the cursor is at the end of the string
                currentText += newkey;
            }
        }
        else
        {
            // Insert the new key at the cursor position
            currentText = currentText.Insert(cursorpos, newkey);
        }

        // Update the DraftTextSpan text
        DraftTextSpan.Text = currentText;

        // Update the cursor appearance
        if (overwritemode)
        {
            CursorSpanOverwrite.Text = "█"; // Blinking box in overwrite mode
        }
        else
        {
            CursorSpanOverwrite.Text = "|"; // Blinking line in insert mode
        }

        // Ensure the cursor is shown in the correct position
        UpdateCursorPosition(cursorpos, overwritemode);
    }

    private void UpdateCursorPosition(int cursorpos, bool overwritemode)
    {
        // Get the current text in the DraftTextSpan
        var currentText = DraftTextSpan.Text;

        // Validate cursor position
        if (cursorpos < 0 || cursorpos > currentText.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(cursorpos), "Cursor position is out of range.");
        }

        // Position the cursor visually in the label
        var textBeforeCursor = currentText.Substring(0, cursorpos);
        var textAfterCursor = currentText.Substring(cursorpos);

        // Update the spans to reflect the cursor position
        DraftTextSpan.Text = textBeforeCursor;
        CursorSpanOverwrite.Text = overwritemode ? "█" : "|";
        CursorSpanOverwrite.TextColor = Colors.Cyan; // Ensure the cursor color is visible
        BeforeCursorInvisibleDraftTextSpan.Text = textAfterCursor;

        // Optionally, start the blinking animation for the cursor
        StartBlinkingCursor();
    }

    private void StartBlinkingCursor()
    {
        // Example: Start a blinking animation for the cursor
        // You can implement this as described earlier, toggling CursorSpan.TextColor or Visibility
    }


    public async Task ToggleSpanVisibility(Label parentLabel, string spanName)
    {
        // Cancel any existing blinking task
        _blinkingTokenSource?.Cancel();
        _blinkingTokenSource = new CancellationTokenSource();
        CancellationToken token = _blinkingTokenSource.Token;

        try
        {
            while (!token.IsCancellationRequested)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    // Find the span with the specified name
                    if (parentLabel.FormattedText is FormattedString formattedString)
                    {
                        var targetSpan = formattedString.Spans.FirstOrDefault(s => s.StyleId == spanName);
                        if (targetSpan != null)
                        {
                            // Toggle visibility by changing the text color
                            targetSpan.TextColor = targetSpan.TextColor == Colors.Transparent
                                ? Colors.Black // Replace with your desired visible color
                                : Colors.Transparent;
                        }
                    }
                });

                // Wait for 700ms before toggling again
                await Task.Delay(700, token);
            }
        }
        catch (TaskCanceledException)
        {
            // Task was cancelled, which is expected behavior
        }
        finally
        {
            // Ensure the span is visible when the task ends
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (parentLabel.FormattedText is FormattedString formattedString)
                {
                    var targetSpan = formattedString.Spans.FirstOrDefault(s => s.StyleId == spanName);
                    if (targetSpan != null)
                    {
                        targetSpan.TextColor = Colors.Black; // Replace with the default visible color
                    }
                }
            });
        }
    }


    private void TapGestureRecognizer_OnTapped(object? sender, TappedEventArgs e)
    {
    }

    async private void Button_OnClicked(object? sender, EventArgs e)
    {
        if (EntryState == EntryStates.Locked) return;
        if (EntryState == EntryStates.Highlight)
        {
            EntryState = EntryStates.Edit;
            // await ToggleSpanVisibility(TextCursor, "CursorSpan");
        }

        if (sender is Button button && EntryState == EntryStates.Edit)
        {
            // InvisibleDraftTextSpan.Text = DraftTextSpan.Text;
            Cursorlogic(button.Text, 0, true);
            return;

            if (DraftTextSpan.Text.Length == MaxChar)
            {
                DraftTextSpan.Text = DraftTextSpan.Text.Substring(0, DraftTextSpan.Text.Length - 1) + button.Text;
                BeforeCursorInvisibleDraftTextSpan.Text =
                    DraftTextSpan.Text.Substring(0, DraftTextSpan.Text.Length - 1);
            }
            else
            {
                DraftTextSpan.Text += button.Text;
                BeforeCursorInvisibleDraftTextSpan.Text = DraftTextSpan.Text;
            }
        }
    }

    private void Button_OnClickedSpecial(object? sender, EventArgs e)
    {
        if (EntryState != EntryStates.Edit)
            return;

        if (sender is not Button button || button.CommandParameter is not string command)
            return;

        switch (command)
        {
            case "BACKSPACE":
                // Remove the last character from DraftTextSpan.Text if it exists
                DraftTextSpan.Text = !string.IsNullOrEmpty(DraftTextSpan.Text)
                    ? DraftTextSpan.Text[..^1]
                    : string.Empty;
                break;

            case "ENTER":
                // Send the text proposal via binding to the ViewModel for verification and storing
                ProposedText = DraftTextSpan.Text;
                // now we have to wait for the answer of the viewmodel
                // if the value is accepted, the viewmodel will set the EntryState to Locked
                // TODO: this change is not detected by the view
                break;

            default:
                // Handle other commands if necessary
                break;
        }
    }


    private async void Button_ChangeEditMode(object? sender, EventArgs e)
    {
        if (sender is Button button)
        {
            switch (button.CommandParameter)
            {
                case "Highlight":
                    EntryState = EntryStates.Highlight;
                    break;
                case "Edit":
                    DraftTextSpan.Text = EnteredText.Text;
                    EntryState = EntryStates.Edit;
                    // await ToggleSpanVisibility(TextCursor, "CursorSpan");
                    break;
                case "Lock":
                    // _blinkingTokenSource?.Cancel();
                    CursorInInsertMode = false;
                    _blinkingTokenSource = new CancellationTokenSource();
                    EntryState = EntryStates.Locked;
                    break;
            }
        }
    }


    private CancellationTokenSource _blinkingCursorCts;

    private async void StartBlinkingCursor(int cursorpos, bool mode)
    {
        // Cancel any existing animation
        _blinkingCursorCts?.Cancel();
        _blinkingCursorCts = new CancellationTokenSource();

        var token = _blinkingCursorCts.Token;

        try
        {
            while (!token.IsCancellationRequested)
            {
                // Toggle TextColor to create a blinking effect
                CursorSpanOverwrite.TextColor =
                    CursorSpanOverwrite.TextColor == Colors.Cyan ? Colors.Transparent : Colors.Cyan;

                // Wait for 500ms between toggles
                await Task.Delay(500, token);
            }
        }
        catch (TaskCanceledException)
        {
            // Animation canceled
            CursorSpanOverwrite.TextColor = Colors.Cyan; // Ensure it stays visible
        }
    }

    private void StopBlinkingCursor()
    {
        _blinkingCursorCts?.Cancel();
        CursorSpanOverwrite.TextColor = Colors.Cyan; // Ensure cursor is visible after stopping
    }
}*/