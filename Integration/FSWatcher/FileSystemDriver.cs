using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EyeOfRubiss.Integration.FSWatcher;

/// <summary>
/// The "driver file" points to chunk files.
/// Each chunk file contains the blockdata for a single chunk.
/// When the driver file changes, this class will reload any stale chunks.
/// </summary>
sealed class FileSystemDriver : IDriver
{
	public event EventHandler<WorldUpdatedEventArgs> WorldUpdated;
	public IWorld World => world;
	private readonly FileInfo driverFile;
	private readonly FileSystemWatcher watcher;
	private readonly object reloadLockObject = new();
	private World world;

	private FileSystemDriver(FileInfo driverFile)
	{
		this.driverFile = driverFile;
		this.watcher = new FileSystemWatcher(driverFile.Directory.FullName);
		watcher.Filter = driverFile.Name;
		watcher.Changed += Watcher_Changed;
		watcher.Created += Watcher_Changed;
		watcher.Renamed += Watcher_Changed;
		watcher.EnableRaisingEvents = true;
		world = Reload(FSWatcher.World.Empty(), driverFile);
	}

	public void Dispose()
	{
		watcher?.Dispose();
	}

	public static IDriver Create(string path)
	{
		var file = new FileInfo(path);
		if (!file.Exists)
		{
			throw new BadIntegrationException($"File does not exist: {path}");
		}
		if (file.Extension.ToLowerInvariant() != ".json")
		{
			throw new BadIntegrationException($"Expected a json file, got {path}");
		}

		return new FileSystemDriver(file);
	}

	private void Watcher_Changed(object sender, FileSystemEventArgs e)
	{
		lock (reloadLockObject)
		{
			int retries = 0;
			bool giveUp = false;

			WorldUpdatedEventArgs args;
			while (!TryReload(out args) && !giveUp)
			{
				retries++;
				if (retries > 3)
				{
					giveUp = true;
				}
				else
				{
					const int milliseconds = 500;
					System.Threading.Thread.Sleep(milliseconds);
				}
			}

			if (giveUp)
			{
				GD.PrintErr("File still locked, giving up!");
			}
			else
			{
				WorldUpdated?.Invoke(this, args);
			}
		}
	}

	private bool TryReload(out WorldUpdatedEventArgs args)
	{
		try
		{
			var newWorld = Reload(world, driverFile);
			this.world = newWorld;
			args = new WorldUpdatedEventArgs()
			{
				World = newWorld,
			};
			return true;
		}
		catch (IOException ex)
		{
			// writer still has file locked probably
			GD.Print($"File still locked: {ex.ToString()}");
			args = null;
			return false;
		}
	}

	private static World Reload(World currentWorld, FileInfo driverFile)
	{
		using var stream = File.OpenRead(driverFile.FullName);
		var content = JsonSerializer.Deserialize<DriverFileContent>(stream);
		stream.Close();
		stream.Dispose();

		GD.Print($"Reloaded {driverFile.FullName} with {content.ChunkInfos.Count} chunks");

		return currentWorld.Reload(content, driverFile);
	}
}
