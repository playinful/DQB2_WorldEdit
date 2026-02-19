using EyeOfRubiss.Info;
using Godot;
using System;
using System.Runtime.ExceptionServices;

namespace EyeOfRubiss
{
    public partial class VoxelGeneratorDQB1(WorldData worldData) : VoxelGeneratorScript
    {
        private WorldData _WorldData { get; set; } = worldData;

        const int CHANNEL = (int)VoxelBuffer.ChannelId.ChannelType;

        public override int _GetUsedChannelsMask() => 1 << CHANNEL;

        public override void _GenerateBlock(VoxelBuffer outBuffer, Vector3I originInVoxels, int lod)
        {
            if (_WorldData is null)
                    return;

            Vector3I bufferSize = outBuffer.GetSize();

            //Step 1: check if inbounds
            if (!StageData.PositionIsInBounds(originInVoxels))
                return ;

            // Step 2: check if chunk exists
            //if (!_WorldData.GetChunkAtPosition(originInVoxels).IsUsed())
            //    return;

            for (int x = 0; x < bufferSize.X; x++)
            {
                for (int y = 0; y < bufferSize.Y; y++)
                {
                    for (int z = 0; z < bufferSize.Z; z++)
                    {
                        Vector3I coords = originInVoxels + new Vector3I(x, y, z);
                        byte block = _WorldData.GetBlockAtPosition(coords);
                        Info.DQB1.BlockInfo blockInfo = Info.DQB1.BlockInfo.Get(block);
                        ulong voxelId = blockInfo.Voxel;
                        outBuffer.SetVoxel(voxelId, x, y, z, CHANNEL);
                    }
                }
            }
        }
    }
}