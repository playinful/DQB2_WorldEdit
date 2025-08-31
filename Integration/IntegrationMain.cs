using EyeOfRubiss;
using EyeOfRubiss.Integration;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class IntegrationMain : Control
{
	private IntegrationWorldEditor WorldEditor;
	private IWorld world;
	private IDriver driver;

	private static string DriverFile { get; set; }

	/// <summary>
	/// Returns true if the command line args contain something like `--driverFile C:\Temp\Blah\driver.json`
	/// telling us to run in integration mode.
	/// </summary>
	public static bool IsRunningIntegrationMode(IReadOnlyList<string> cmdLineArgs)
	{
		var args = cmdLineArgs.Select(x => x?.ToLowerInvariant()).ToList();
		var idx = args.IndexOf("--driverfile");
		if (idx < 0)
		{
			return false;
		}
		if (idx == args.Count - 1)
		{
			GD.PrintErr("The `--driverFile` argument must be followed by the full path to the driver file");
			return false;
		}
		DriverFile = cmdLineArgs[idx + 1];
		return true;
	}

	public override void _Ready()
	{
		WorldEditor = GetNode<IntegrationWorldEditor>("%WorldEditor");

		if (string.IsNullOrEmpty(DriverFile))
		{
			throw new Exception($"Assert fail: DriverFile should have been set by {nameof(IsRunningIntegrationMode)}");
		}

		GD.Print($"{nameof(IntegrationMain)} has loaded... Now looking at {DriverFile}");
		driver = EyeOfRubiss.Integration.FSWatcher.FileSystemDriver.Create(DriverFile);
		driver.WorldUpdated += Driver_WorldUpdated;
		world = driver.World;
		GD.Print("Driver created!");

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
