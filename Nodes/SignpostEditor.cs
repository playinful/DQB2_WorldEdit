using EyeOfRubiss;
using Godot;
using System;

public partial class SignpostEditor : Window
{
	[Export] private TextEdit _TextEdit;

	private ParamData.Signpost _SignpostDQB1;
	private StageData.Signpost _SignpostDQB2;

	public void Popup(ParamData.Signpost signpost)
	{
		_SignpostDQB1 = signpost;
		_SignpostDQB2 = null;
		_TextEdit.Text = signpost.Text
			.Replace("<br>", "\n");

		Title = $"Signpost at {signpost.GetPosition()}";
		_TextEdit.TooltipText =
			"<cap> - Capitalise next letter" +
			"\n" +
			"\n<icon(X)> - Display icon number X" + 
			"\n<button(X)> - Display button icon number X" +
			"\n" +
			"\n<key> - Wait for input" +
			"\n<color(0xFFFFFFFF)> - Change text color to hex code" +
			"\n</color> - Change text color to default" +
			"\n<off> - Makes text box invisible" +
			"\n<on> - Makes text box visible" +
			"\n<speaker(Name)> - Displays name tag above text box" +
			"\n<clear> - Clears text box" +
			"\n" +
			"\n<man> - Masculine voice SFX" +
			"\n<woman> - Feminine voice SFX" +
			"\n<unknown> - Ambiguous voice SFX" +
			"\n<nose> - No voice SFX";

		PopupCentered();
	}
	public void Popup(StageData.Signpost signpost)
	{
		_SignpostDQB1 = null;
		_SignpostDQB2 = signpost;
		_TextEdit.Text = signpost.Text
			// .Replace("<6>", "‘")
			// .Replace("<9>", "’")
			// .Replace("<66>", "“")
			// .Replace("<99>", "”")
			// .Replace("<-->", "–")
			// .Replace("<note>", "♪")
			.Replace("<br>", "\n");

		Title = $"Signpost at {signpost.GetPosition()}";
		_TextEdit.TooltipText =
			"<pname> - Player name" +
			"\n<morf(X,Y)> - Changes text based on player gender" + 
			"\n<cap> - Capitalise next letter" +
			"\n<allcap> - Capitalise all following text" +
			"\n</allcap> - Stop capitalising text" +
			"\n" +
			"\n<icon(X)> <$icon(X)> - Display icon number X" + 
			"\n<$iicon(X)> - Display icon of item number X" +
			"\n<$kicon(X)> - Display icon of room/set number X" +
			"\n<$ui(X)> - Display UI icon number X" +
			"\n<button(X)> - Display button icon number X" +
			"\n" +
			"\n<key> - Wait for input" +
			"\n<$cdef(X)> - Change text color to color number X" +
			"\n<color(0xFFFFFFFF)> - Change text color to hex code" +
			"\n</color> - Change text color to default" +
			"\n<off> - Makes text box invisible" +
			"\n" +
			"\n<man> - Masculine voice SFX" +
			"\n<woman> - Feminine voice SFX" +
			"\n<unknown> - Ambiguous voice SFX" +
			"\n<nose> - No voice SFX";
		
		PopupCentered();
	}

	public void _On_Button_Apply_Pressed()
	{
		if (_SignpostDQB1 is not null)
		{
			_SignpostDQB1.Text = _TextEdit.Text
				.Replace("\r\n", "<br>")
				.Replace("\n", "<br>")
				.Replace("\r", "<br>");
			_TextEdit.Text = _SignpostDQB1.Text
				.Replace("<br>", "\n");

			_SignpostDQB1.Written = !string.IsNullOrEmpty(_SignpostDQB1.Text);
			_SignpostDQB1.Language = 3;
		}
		else if (_SignpostDQB2 is not null)
		{
			_SignpostDQB2.Text = _TextEdit.Text
				// .Replace("‘", "<6>")
				// .Replace("’", "<9>")
				// .Replace("“", "<66>")
				// .Replace("”", "<99>")
				// .Replace("–", "<-->")
				// .Replace("♪", "<note>")
				.Replace("\r\n", "<br>")
				.Replace("\n", "<br>")
				.Replace("\r", "<br>");
			_TextEdit.Text = _SignpostDQB2.Text
				// .Replace("<6>", "‘")
				// .Replace("<9>", "’")
				// .Replace("<66>", "“")
				// .Replace("<99>", "”")
				// .Replace("<-->", "–")
				// .Replace("<note>", "♪")
				.Replace("<br>", "\n");

			_SignpostDQB2.Written = !string.IsNullOrEmpty(_SignpostDQB2.Text);
		}
	}

	public void Close()
	{
		Hide();
		_SignpostDQB1 = null;
		_SignpostDQB2 = null;
		_TextEdit.Text = string.Empty;
	}
}
