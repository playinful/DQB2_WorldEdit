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
	private IDriver driver; // needed to make sure the driver doesn't get GC'd which would cause the FileSystemWatcher to stop firing

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		driver?.Dispose();
		driver = null;
	}

	private static string DriverFile { get; set; }

	/// <summary>
	/// Returns true if the command line args contain something like `--driverFile C:\Temp\Blah\driver.json`
	/// telling us to run in integration mode.
	/// </summary>
	public static bool ShouldSwitchToIntegrationMode(IReadOnlyList<string> cmdLineArgs)
	{
		var args = cmdLineArgs.Select(x => x?.ToLowerInvariant()).ToList();
		var idx = args.IndexOf("--driverfile");
		if (idx < 0)
		{
			return false;
		}
		if (idx == args.Count - 1)
		{
			GD.PushError("The `--driverFile` argument must be followed by the full path to the driver file");
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
			throw new Exception($"Assert fail: DriverFile should have been set by {nameof(ShouldSwitchToIntegrationMode)}");
		}

		GD.Print($"{nameof(IntegrationMain)} has loaded... Now looking at {DriverFile}");

		IDriver driver;
		string json = System.IO.File.ReadAllText(DriverFile);
		var peek = System.Text.Json.JsonSerializer.Deserialize<DriverFileContentBase>(json);
		if (peek.IntegrationType == IntegrationTypeConstants.FSWatcher)
		{
			driver = EyeOfRubiss.Integration.FSWatcher.FileSystemDriver.Create(DriverFile);
		}
		else
		{
			throw new BadIntegrationException($"unrecognized IntegrationType given: {peek.IntegrationType}");
		}

		this.driver = driver;
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
