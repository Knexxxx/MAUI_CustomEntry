
namespace ExtendedDataEntry;

public partial class DataEntry : ContentView
{
	public static readonly BindableProperty InitialMaxCharProperty =
		BindableProperty.Create(nameof(InitialMaxChar), typeof(int), typeof(Label), 6);

	public int InitialMaxChar
	{
		get => (int)GetValue(InitialMaxCharProperty);
		set => SetValue(InitialMaxCharProperty, value);
	}
	
	public enum EntryState { Locked, Highlight, Edit }

	public EntryState EntryState2 { get; set; } = EntryState.Locked;
	

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
	
	
	public async Task ToggleLabelVisibility(Label parentLabel)
	{
		// Cancel any existing blinking task
		_blinkingTokenSource?.Cancel();
		_blinkingTokenSource = new CancellationTokenSource();
		CancellationToken token = _blinkingTokenSource.Token;

		try
		{
			while (!token.IsCancellationRequested)
			{
				// Toggle visibility
				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					parentLabel.IsVisible = !parentLabel.IsVisible;
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
			// Ensure the label is invisible when the task ends
			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				parentLabel.IsVisible = false; // Default to invisible
			});
		}
	}




	private void TapGestureRecognizer_OnTapped(object? sender, TappedEventArgs e)
	{
		
	}

	async private void Button_OnClicked(object? sender, EventArgs e)
	{
		if (EntryState2 == EntryState.Locked) return;
		if (EntryState2 == EntryState.Highlight)
		{
			EntryState2 = EntryState.Edit;
			Highlighter.IsVisible = false;
			TextCursor.IsVisible = true;
			await ToggleLabelVisibility(TextCursor);
		}

		if (sender is Button button && EntryState2 == EntryState.Edit)
		{
			if (EnteredText.Text.Length < InitialMaxChar)
				EnteredText.Text += button.Text;
			else
				EnteredText.Text = EnteredText.Text.Substring(0, EnteredText.Text.Length - 1) + button.Text;
			
		}
	}
	
	private void Button_OnClickedSpecial(object? sender, EventArgs e)
	{
		if (EntryState2 == EntryState.Edit)
		{
			// Handle BACKSPACE specifically for the edit state
			if (sender is Button buttonInEditMode && buttonInEditMode.CommandParameter is "BACKSPACE")
			{
				EnteredText.Text = EnteredText.Text.Length > 0
					? EnteredText.Text.Remove(EnteredText.Text.Length - 1)
					: string.Empty;
				return;
			}

			return;
		}

		if (sender is Button clickedButton)
		{
			if (clickedButton.CommandParameter is not "BACKSPACE")
			{
				ButtonLock.SendClicked();
				return;
			}
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
					EntryState2 = EntryState.Highlight;
					ButtonHighlight.BackgroundColor = Colors.Yellow;
					break;

				case "Edit":
					Highlighter.IsVisible = false;
					TextCursor.IsVisible = true;
					EntryState2 = EntryState.Edit;
					ButtonEdit.BackgroundColor = Colors.Yellow;
					await ToggleLabelVisibility(TextCursor);
					break;

				case "Lock":
					Highlighter.IsVisible = false;
					TextCursor.IsVisible = false;
					_blinkingTokenSource?.Cancel();
					_blinkingTokenSource = new CancellationTokenSource();
					EntryState2 = EntryState.Locked;
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

