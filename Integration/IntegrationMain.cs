using EyeOfRubiss;
using Godot;
using System;

public partial class IntegrationMain : Control
{
	private IntegrationWorldEditor WorldEditor;

	public override void _Ready()
	{
		GD.Print($"{nameof(IntegrationMain)} has loaded...");
		WorldEditor = GetNode<IntegrationWorldEditor>("%WorldEditor");
		WorldEditor.LoadWorld(StageData.Instance);
	}
}
