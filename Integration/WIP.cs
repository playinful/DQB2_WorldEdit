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

record struct Box(Vector3I Start, Vector3I Size)
{
	public Vector3I End => new Vector3I(Start.X + Size.X, Start.Y + Size.Y, Start.Z + Size.Z);
}

interface IWorld
{
	bool HasData(Box box);

	StageData.BlockInstance GetBlockAtPosition(Vector3I position);
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
					var block = world.GetBlockAtPosition(pos);
					int blockId = block?.BlockID ?? 0;
					ulong voxelId = voxelIdLookup[blockId % blockIdCount];
					outBuffer.SetVoxel(voxelId, x, y, z, Channel);
				}
			}
		}
	}
}
