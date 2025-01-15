
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ExtendedDataEntry;

public partial class DataEntry : ContentView
{
    private const char CursorLabelText = '▊';//'▍'//'▌'//'█';
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
		BindableProperty.Create(nameof(EntryState), typeof(EntryStates), typeof(Label), EntryStates.Locked,BindingMode.TwoWay);

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
            dataEntry.UpdateEntryState();
        }
    }

    // private static BindableProperty.ValidateValueDelegate Test4(BindableObject test, object result)
    // {
    // 	return result;
    // }


    protected void UpdateEntryState()
    {
        Debug.WriteLine("Updating EntryState changed");
        // Add your logic to handle the state update
    }





    public enum EntryStates { Locked, Highlight, Edit }



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
                DraftTextAfterSpan.Text = button.Text + DraftTextAfterSpan.Text.Substring(1, DraftTextAfterSpan.Text.Length - 1);
 
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
                    if (string.IsNullOrEmpty(totalText))
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

        if (totalText.Length < MaxChar || (!string.IsNullOrEmpty(DraftTextSpan.Text) && !string.IsNullOrEmpty(DraftTextAfterSpan.Text)))
        {
            CursorLabel.TranslationX = tmpLabel.Measure(double.MaxValue, double.MaxValue).Width;
            if(!string.IsNullOrEmpty(DraftTextAfterSpan.Text))
                CursorLabel.Text = cursorInsertText.ToString();
        }

        if (totalText.Length == MaxChar || string.IsNullOrEmpty(DraftTextAfterSpan.Text))
            CursorLabel.Text = CursorLabelText.ToString();
    }
}

