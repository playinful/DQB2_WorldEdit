using EyeOfRubiss.Info.DQB2;
using Godot;
using System;
using System.Runtime.ExceptionServices;

namespace EyeOfRubiss
{
    /// <summary> VoxelStreamScript that streams a DQB2 StageData instance as voxel data. </summary>
    public partial class VoxelGeneratorDQB2(StageData stageData, bool showTerrain = true, bool showFluid = true, bool showPartsBlock = false) : VoxelGeneratorScript
    {
        const ulong BLOCK_SEAFLOOR = 8;

        public StageData _StageData { get; set; } = stageData;

        public bool ShowTerrain { get; set; } = showTerrain;
        public bool ShowFluid { get; set; } = showFluid;
        public bool ShowPartsBlock { get; set; } = showPartsBlock;

        const int CHANNEL = (int)VoxelBuffer.ChannelId.ChannelType;

        public override int _GetUsedChannelsMask() => 1 << CHANNEL;

        public override void _GenerateBlock(VoxelBuffer outBuffer, Vector3I originInVoxels, int lod)
        {
            if (_StageData is null || !_StageData.IsLoaded)
                return;

            if (originInVoxels.Y < 0 && !ShowPartsBlock)
            {
                outBuffer.Fill(BLOCK_SEAFLOOR);
                return;
            }
            Vector3I bufferSize = outBuffer.GetSize();

            //Step 1: check if inbounds
            if (!StageData.PositionIsInBounds(originInVoxels))
                return;

            // Step 2: check if chunk exists
            if (!_StageData.GetChunkAtPosition(originInVoxels).IsUsed())
                return;
            
            for (int x = 0; x < bufferSize.X; x++)
            {
                for (int y = 0; y < bufferSize.Y; y++)
                {
                    for (int z = 0; z < bufferSize.Z; z++)
                    {
                        Vector3I coords = originInVoxels + new Vector3I(x, y, z);
                        StageData.BlockInstance block = _StageData.GetBlockAtPosition(coords);
                        if (block is not null)
                        {
                            BlockInfo blockInfo = BlockInfo.Get(block.BlockID);
                            ulong voxelId = ShowPartsBlock ? (ulong)blockInfo.GetPartsType() : blockInfo.Voxel;
                            outBuffer.SetVoxel(voxelId, x, y, z, CHANNEL);
                        }
                    }
                }
            }
        }
    }
}