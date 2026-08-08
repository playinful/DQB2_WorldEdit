using EyeOfRubiss;
using EyeOfRubiss.Info.DQB2;
using EyeOfRubiss.Scenes;
using Godot;
using System;

public partial class SalutationStationEditor : Window
{
	[Export] OptionButton _OptionButton;
	[Export] TextEdit _TextEdit;

	private StageData.SalutationStation _SalutationStation;

	public void Popup(StageData.SalutationStation station)
	{
		_OptionButton.Clear();
		_OptionButton.AddItem("None");
		for (int i = 1; i < CommonData.Resident.MAXIMUM; i++)
		{
			string name = Main.GetResidentName(i);
			if (string.IsNullOrEmpty(name))
			{
				_OptionButton.AddItem($"[{i}]");
			}
			else
			{
				_OptionButton.AddItem(name);
			}
		}

		_SalutationStation = station;
		_TextEdit.Text = station.Text.Replace("<br>", "\n");
		_OptionButton.Selected = station.ResidentID;
		Title = $"Salutation Station at {station.GetPosition()}";
		PopupCentered();
	}

	public void _On_Button_Apply_Pressed()
	{
		_SalutationStation.Text = _TextEdit.Text.Replace("\r\n", "<br>").Replace("\n", "<br>").Replace("\r", "<br>");
		_TextEdit.Text = _SalutationStation.Text.Replace("<br>", "\n");

		_SalutationStation.ResidentID = (ushort)_OptionButton.Selected;
	}

	public void Close()
	{
		_SalutationStation = null;
		_OptionButton.Selected = -1;
		_TextEdit.Text = string.Empty;
		Hide();
	}
}
