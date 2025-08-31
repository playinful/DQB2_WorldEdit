using EyeOfRubiss;
using EyeOfRubiss.Integration;
using Godot;
using System;

public partial class IntegrationMain : Control
{
	private IntegrationWorldEditor WorldEditor;
	private IWorld world;
	private IDriver driver;

	public static string SelectedFile { get; set; }

	public override void _Ready()
	{
		WorldEditor = GetNode<IntegrationWorldEditor>("%WorldEditor");
		IWorld world;

		if (!string.IsNullOrEmpty(SelectedFile))
		{
			GD.Print($"{nameof(IntegrationMain)} has loaded... Now looking at {SelectedFile}");
			driver = EyeOfRubiss.Integration.FSWatcher.FileSystemDriver.Create(SelectedFile);
			driver.WorldUpdated += Driver_WorldUpdated;
			world = driver.World;
			GD.Print("Driver created!");
		}
		else
		{
			world = StageData.Instance ?? throw new Exception("Missing StageData");
		}

		WorldEditor.LoadWorld(world);
	}

	private void Driver_WorldUpdated(object sender, WorldUpdatedEventArgs e)
	{
		// Calling Refresh from this thread (the FileSystemWatcher thread) will cause
		// an error; using CallDeferred solves it.
		this.world = e.World;
		this.CallDeferred(nameof(Refresh));
	}

	private void Refresh()
	{
		var world = this.world;
		if (world == null)
		{
			return;
		}
		WorldEditor.LoadWorld(world);
	}
}
