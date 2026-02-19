using Godot;
using System;

namespace EyeOfRubiss
{
    public partial class VoxelGeneratorBlueprintDQB2(Blueprint blueprint) : VoxelGeneratorScript
    {
        private Blueprint _Blueprint = blueprint;

        const int CHANNEL = (int)VoxelBuffer.ChannelId.ChannelType;

        public override int _GetUsedChannelsMask() => 1 << CHANNEL;

        public override void _GenerateBlock(VoxelBuffer outBuffer, Vector3I originInVoxels, int lod)
        {
            if (_Blueprint is null)
                return;

            Vector3I bufferSize = outBuffer.GetSize();

            if (originInVoxels.X < 0 || originInVoxels.Y < 0 || originInVoxels.Z < 0 ||
                originInVoxels.X >= _Blueprint.SizeX || originInVoxels.Y >= _Blueprint.SizeY || originInVoxels.Z >= _Blueprint.SizeZ)
                return;

            for (int x = 0; x < bufferSize.X && originInVoxels.X + x < _Blueprint.SizeX; x++)
            {
                for (int y = 0; y < bufferSize.Y && originInVoxels.Y + y < _Blueprint.SizeY; y++)
                {
                    for (int z = 0; z < bufferSize.Z && originInVoxels.Z + z < _Blueprint.SizeZ; z++)
                    {
                        Vector3I coords = originInVoxels + new Vector3I(x, y, z);
                        if (_Blueprint.GetBlock(coords) is Blueprint.BlueprintBlockInstance block)
                        {
                            Info.DQB2.BlockInfo blockInfo = Info.DQB2.BlockInfo.Get(block.BlockID);
                            ulong voxelId = blockInfo.Voxel;
                            outBuffer.SetVoxel(voxelId, x, y, z, CHANNEL);    
                        }
                    }
                }
            }
        }
    }
}