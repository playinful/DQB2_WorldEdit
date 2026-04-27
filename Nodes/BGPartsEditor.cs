using EyeOfRubiss;
using Godot;
using System;

public partial class BGPartsEditor : Window
{
	[Export] private SpinBox _BGPartsID_SpinBox;
	[Export] private TextEdit _HexEditor_TextEdit;

	public StageData.BGParts Parts;

	public void _On_BGPartsID_Apply_Button_Pressed()
	{
		if (Parts is null)
			return;
		
		ushort partsId = (ushort)_BGPartsID_SpinBox.Value;
		Parts.BGPartsID = partsId;

		UpdateControls();
	}
	public void _On_HexEditor_Apply_Button_Pressed()
	{
		if (Parts is null)
			return;
		
		string hexstring = _HexEditor_TextEdit.Text.Replace(" ", "").Replace("\n", "").Replace("\r", "");

		byte[] stringBytes = Convert.FromHexString(hexstring);
		Span<byte> partsBytes = Parts.GetBytes();
		for (int i = 0; i < partsBytes.Length; i++)
		{
			if (i < stringBytes.Length)
			{
				partsBytes[i] = stringBytes[i];
			}
			else
			{
				partsBytes[i] = 0;
			}
		}

		UpdateControls();
	}

	public void SetBGParts(StageData.BGParts parts)
	{
		Parts = parts;

		UpdateControls();
	}
	private void UpdateControls()
	{
		if (Parts is null)
			return;
		
		_BGPartsID_SpinBox.Value = Parts.BGPartsID;
		_HexEditor_TextEdit.Text = Convert.ToHexString(Parts.GetBytes());
	}
}
