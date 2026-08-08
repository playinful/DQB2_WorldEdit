using EyeOfRubiss;
using EyeOfRubiss.Info.DQB2;
using Godot;
using System;

public partial class InstrumentEditor : Window
{
	[Export] private OptionButton _OptionButton;

	private StageData.Instrument _Instrument;

	public void Popup(StageData.Instrument instrument)
	{
		if (_OptionButton.ItemCount <= 0)
			_InitializeSongList();
		
		_Instrument = instrument;
		_OptionButton.Selected = _OptionButton.GetItemIndex(instrument.Song);
		Title = $"Instrument at {instrument.GetPosition()}";
		PopupCentered();
	}

	private void _InitializeSongList()
	{
		_OptionButton.Clear();
		string[] songs = SongName.GetAll();
		for (int i = 0; i < songs.Length; i++)
		{
			string song = songs[i];
			if (!string.IsNullOrEmpty(song))
				_OptionButton.AddItem(song, i);
		}
	}

	public void _On_OptionButton_ItemSelected(int index)
	{
		if (_Instrument is null)
			return;

		int songId = _OptionButton.GetItemId(index);
		_Instrument.Song = (byte)songId;
	}

	public void Close()
	{
		Hide();
		_Instrument = null;
		_OptionButton.Selected = 0;
	}
}
