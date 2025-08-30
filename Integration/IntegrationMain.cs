using EyeOfRubiss;
using EyeOfRubiss.Integration;
using Godot;
using System;

public partial class IntegrationMain : Control
{
	private IntegrationWorldEditor WorldEditor;

	public static string SelectedFile { get; set; }

	public override void _Ready()
	{
		WorldEditor = GetNode<IntegrationWorldEditor>("%WorldEditor");
		IWorld world;

		if (!string.IsNullOrEmpty(SelectedFile))
		{
			GD.Print($"{nameof(IntegrationMain)} has loaded... Now looking at {SelectedFile}");
			var driver = EyeOfRubiss.Integration.FSWatcher.FileSystemDriver.Create(SelectedFile);
			world = driver.World;
			GD.Print("Driver created!");
		}
		else
		{
			world = StageData.Instance ?? throw new Exception("Missing StageData");
		}

		WorldEditor.LoadWorld(world);
	}
}
