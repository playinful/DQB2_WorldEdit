using EyeOfRubiss.Info;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EyeOfRubiss.Integration;

partial class IWorldVoxelGenerator : VoxelGeneratorScript
{
	private readonly IWorld world;
	private readonly ulong[] voxelIdLookup;
	const int blockIdCount = 2048;

	public IWorldVoxelGenerator(IWorld world)
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