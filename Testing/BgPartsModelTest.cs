using EyeOfRubiss;
using Godot;
using Microsoft.VisualBasic;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;

public partial class BgPartsModelTest : Node3D
{
	[Export] private BGPartsGridManager _BGPartsGridManager;
	[Export] private Label _Label;
	[Export] private SpinBox _SpinBox;

    private static BGPartsGridManager.BGPartsModel[] _ModelParams;

	public int CurrentModelID { get; set; } = 0;
	private int _Minimum { get; set; }
	private int _Maximum { get; set; }

    public override void _Ready()
    {
        _ModelParams = JsonSerializer.Deserialize<BGPartsGridManager.BGPartsModel[]>(FileAccess.GetFileAsString("res://Info/BGPartsModel.json"));

		BGPartsGridManager.BGPartsModel[] sorted = [.. _ModelParams.OrderBy(param => param.ID)];
		_Minimum = sorted[0].ID;
		_Maximum = sorted[^1].ID;
		CurrentModelID = _Minimum;

		UpdateModel();
    }

	public void _On_PreviousButton_Pressed()
	{
		CurrentModelID -= 1;
		if (CurrentModelID < _Minimum)
			CurrentModelID = _Maximum;
		UpdateModel();
	}
	public void _On_NextButton_Pressed()
	{
		CurrentModelID += 1;
		if (CurrentModelID > _Maximum)
			CurrentModelID = _Minimum;
		UpdateModel();
	}
	public void _On_SpinBoxGoButton_Pressed()
	{
		int value = (int)_SpinBox.Value;
		if (value > _Maximum)
			CurrentModelID = _Maximum;
		else if (value < _Minimum)
			CurrentModelID = _Minimum;
		else
			CurrentModelID = value;
		UpdateModel();
	}

	public void UpdateModel()
	{
		_BGPartsGridManager.ClearCellItem(Vector3I.Zero);
		_BGPartsGridManager.AddCellItem(Vector3I.Zero, CurrentModelID);

		BGPartsGridManager.BGPartsModel param = _ModelParams.FirstOrDefault(p => p.ID == CurrentModelID);
		
		_Label.Text = 
			$"ID: {CurrentModelID}\n" +
			$"Name: {param.Name}";
		_Label.Text += 
			$"\nUsed by (DQB1):";
		foreach (EyeOfRubiss.Info.DQB1.BGPartsInfo info in EyeOfRubiss.Info.DQB1.BGPartsInfo.GetAll().Where(i => i.Mesh == CurrentModelID))
		{
			_Label.Text += $"\n{info.Name} [{info.ID}]";
		}
		_Label.Text += 
			$"\nUsed by (DQB2):";
		foreach (EyeOfRubiss.Info.DQB2.BGPartsInfo info in EyeOfRubiss.Info.DQB2.BGPartsInfo.GetAll().Where(i => i.Mesh == CurrentModelID))
		{
			_Label.Text += $"\n{info.Name} [{info.ID}]";
		}
	}
}
