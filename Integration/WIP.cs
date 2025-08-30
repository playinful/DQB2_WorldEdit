using EyeOfRubiss;
using EyeOfRubiss.Info;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Godot.VoxelStream;

namespace EyeOfRubiss.Integration;

sealed class WorldUpdatedEventArgs
{
	public IWorld World { get; init; }
}

interface IDriver : IDisposable
{
	public event EventHandler<WorldUpdatedEventArgs> WorldUpdated;
	public IWorld World { get; }
}

record struct Block(ushort Value)
{
	public ushort BlockId => (ushort)(Value & 0x7FF);
	public bool PlayerPlaced => (Value & 0x800) >> 11 != 0;
	public StageData.BlockInstance.ChiselType Chisel => (StageData.BlockInstance.ChiselType)((Value & 0xF000) >> 12);
}

record struct Box(Vector3I Start, Vector3I Size)
{
	public Vector3I End => new Vector3I(Start.X + Size.X, Start.Y + Size.Y, Start.Z + Size.Z);
}

interface IWorld
{
	bool HasData(Box box);

	Block GetBlockAtPosition(Vector3I position);

	/// <summary>
	/// When using an IWorld, the camera coordinates will match the Zylann coordinates.
	/// So this property allows the IWorld to declare where the camera should start.
	/// </summary>
	/// <remarks>
	/// Other code applies an XZ transform of -1024,-1024 to the VoxelTerrain node
	/// so that when the camera is at 0,0 it's looking at XZ=1024,1024.
	/// This works nicely for DQB2 StageData because that will always put you near
	/// the center of the island, but it would be slightly confusing for an
	/// integration to have to know this and adjust to put its build near 1024,1024.
	/// </remarks>
	Vector3I InitialCameraPosition { get; }
}

partial class WIPGenerator : VoxelGeneratorScript
{
	private readonly IWorld world;
	private readonly ulong[] voxelIdLookup;
	const int blockIdCount = 2048;

	public WIPGenerator(IWorld world)
	{
		this.world = world;
		this.voxelIdLookup = new ulong[blockIdCount];

		for (ushort blockId = 0; blockId < blockIdCount; blockId++)
		{
			var block = BlockInfo.Get(blockId);
			voxelIdLookup[blockId] = block.VoxelID;
		}
	}

	const ulong BLOCK_SEAFLOOR = 8;
	const int Channel = (int)VoxelBuffer.ChannelId.ChannelType;

	public override int _GetUsedChannelsMask()
	{
		return 1 << Channel;
	}

	public override void _GenerateBlock(VoxelBuffer outBuffer, Vector3I originInVoxels, int lod)
	{
		if (lod != 0)
		{
			GD.Print($"Ignoring lod: {lod}"); // seems to never happen, which is good I think
			return;
		}

		if (world == null)
		{
			return;
		}

		if (originInVoxels.Y < 0)
		{
			outBuffer.Fill(BLOCK_SEAFLOOR);
			return;
		}
		if (originInVoxels.Y >= 96 || originInVoxels.X < 0 || originInVoxels.Z < 0)
		{
			return;
		}

		Vector3I bufferSize = outBuffer.GetSize();
		if (!world.HasData(new Box(originInVoxels, bufferSize)))
		{
			return;
		}

		for (int x = 0; x < bufferSize.X; x++)
		{
			for (int z = 0; z < bufferSize.Z; z++)
			{
				for (int y = 0; y < bufferSize.Y; y++)
				{
					var pos = originInVoxels + new Vector3I(x, y, z);
					var blockId = world.GetBlockAtPosition(pos).BlockId;
					ulong voxelId = voxelIdLookup[blockId % blockIdCount];
					outBuffer.SetVoxel(voxelId, x, y, z, Channel);
				}
			}
		}
	}
}

record struct ChunkLocation(int X32, int Z32)
{
	public int X => X32 * 32;
	public int Z => Z32 * 32;

	public static ChunkLocation FromPosition(Vector3I position)
	{
		int x32 = position.X / 32;
		int z32 = position.Z / 32;
		return new ChunkLocation(x32, z32);
	}
}

/// <summary>
/// Used when an integration sends us a bad request or bad data.
/// </summary>
sealed class BadIntegrationException : Exception
{
	public BadIntegrationException(string message) : base(message) { }
}
