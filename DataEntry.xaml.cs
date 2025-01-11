
namespace ExtendedDataEntry;

public partial class DataEntry : ContentView
{
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
		BindableProperty.Create(nameof(EntryState), typeof(EntryStates), typeof(Label), EntryStates.Locked);

	public EntryStates EntryState
	{
		get => (EntryStates)GetValue(EntryStateProperty);
		set => SetValue(EntryStateProperty, value);
	}
	
	
	
	
	public enum EntryStates { Locked, Highlight, Edit }

	

	public DataEntry()
	{
		InitializeComponent();
	}
	
	
	/*private const double CharWidth = 10; // Approximate width of one character (adjust as needed)

	private void UpdateCursorPosition()
	{
		if (EnteredText.Text.Length >= InitialMaxChar)
		{
			// Calculate the cursor's position
			double textWidth = EnteredText.Measure(double.PositiveInfinity, double.PositiveInfinity).Request.Width;
			TextCursor.TranslationX = textWidth - CharWidth;

			// Show the cursor
			TextCursor.IsVisible = true;
		}
		else
		{
			// Calculate the position for current text length
			double textWidth = EnteredText.Measure(double.PositiveInfinity, double.PositiveInfinity).Request.Width;
			TextCursor.TranslationX = textWidth;

			// Show the cursor
			TextCursor.IsVisible = true;
		}
	}*/

	
	
	
	private static CancellationTokenSource? _blinkingTokenSource;
	public static CancellationTokenSource? BlinkingTokenSource
	{
		get => _blinkingTokenSource;
		set => _blinkingTokenSource = value;
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
			Highlighter.IsVisible = false;
			TextCursor.IsVisible = true;
			await ToggleSpanVisibility(TextCursor, "CursorSpan");
		}

		if (sender is Button button && EntryState == EntryStates.Edit)
		{
			if (DraftTextSpan.Text.Length < MaxChar)
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

			case "ENTER":
				// Send the text proposal via binding to the ViewModel for verification and storing
				ProposedText = DraftTextSpan.Text;
				break;

			default:
				// Handle other commands if necessary
				break;
		}
	}


	// 	if (sender is Button clickedButton)
	// 	{
	// 		if (clickedButton.CommandParameter is not "BACKSPACE")
	// 		{
	// 			ButtonLock.SendClicked();
	// 			return;
	// 		}
	// 		
	// 	}
	// }

	
	
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
					EntryState = EntryStates.Highlight;
					ButtonHighlight.BackgroundColor = Colors.Yellow;
					break;

				case "Edit":
					Highlighter.IsVisible = false;
					TextCursor.IsVisible = true;
					EnteredText.IsVisible = false;
					DraftTextSpan.Text = EnteredText.Text;
					EntryState = EntryStates.Edit;
					ButtonEdit.BackgroundColor = Colors.Yellow;
					await ToggleSpanVisibility(TextCursor, "CursorSpan");
					break;

				case "Lock":
					Highlighter.IsVisible = false;
					TextCursor.IsVisible = false;
					EnteredText.IsVisible = true;
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


	
}

