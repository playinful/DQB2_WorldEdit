using EyeOfRubiss.Info;
using Godot;
using System;
using System.Runtime.ExceptionServices;

namespace EyeOfRubiss
{
    /// <summary> VoxelStreamScript that streams a DQB2 StageData instance as voxel data. </summary>
    public partial class VoxelStreamDQB2(StageData stageData, bool propsOnly = false) : VoxelStreamScript
    {
        const ulong BLOCK_SEAFLOOR = 8;

        public StageData DQB2StageData { get; set; } = stageData;
        public bool PropsOnly { get; set; } = propsOnly;

        const int CHANNEL = (int)VoxelBuffer.ChannelId.ChannelType;

        public override int _GetUsedChannelsMask() => 1 << CHANNEL;

        public override int _LoadVoxelBlock(VoxelBuffer outBuffer, Vector3I positionInBlocks, int lod)
        {
            if (DQB2StageData is null || !DQB2StageData.IsLoaded)
                    return (int)ResultCode.BlockNotFound;

            if (positionInBlocks.Y < 0 && !PropsOnly)
            {
                outBuffer.Fill(BLOCK_SEAFLOOR);
                return (int)ResultCode.BlockFound;
            }
            Vector3I bufferSize = outBuffer.GetSize();

            //Step 1: check if inbounds
            if (!StageData.PositionIsInBounds(positionInBlocks * bufferSize))
                return (int)ResultCode.BlockNotFound;

            // Step 2: check if chunk exists
            if (!DQB2StageData.GetChunkAtPosition(positionInBlocks * bufferSize).IsUsed())
                return (int)ResultCode.BlockNotFound;
            

            for (int x = 0; x < bufferSize.X; x++)
            {
                for (int y = 0; y < bufferSize.Y; y++)
                {
                    for (int z = 0; z < bufferSize.Z; z++)
                    {
                        Vector3I coords = (positionInBlocks * bufferSize) + new Vector3I(x, y, z);
                        //int tile = coords.X % 32 + (coords.Z % 32 * 32);
                        //StageData.BlockInstance block = chunk.GetBlock(coords.Y, tile);
                        StageData.BlockInstance block = StageData.Instance.GetBlockAtPosition(coords);
                        if (block is not null)
                        {
                            BlockInfo blockInfo = BlockInfo.Get(block.BlockID);
                            ulong voxelId = PropsOnly ? (ulong)blockInfo.PropShell : blockInfo.VoxelID;
                            //ulong voxelId = blockInfo.VoxelID;
                            outBuffer.SetVoxel(voxelId, x, y, z, CHANNEL);
                        }
                    }
                }
            }

            return (int)ResultCode.BlockFound;
        }
        public override void _SaveVoxelBlock(VoxelBuffer buffer, Vector3I positionInBlocks, int lod)
        {
            // This method intentionally left blank.
            // Saving voxel data is not handled in this class.
            return;
        }
    }
}