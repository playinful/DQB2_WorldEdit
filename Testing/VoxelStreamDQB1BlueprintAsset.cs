using EyeOfRubiss.Info;
using Godot;
using System;
using System.Runtime.ExceptionServices;

namespace EyeOfRubiss
{
    /// <summary> VoxelStreamScript that streams a DQB2 StageData instance as voxel data. </summary>
    public partial class VoxelStreamDQB1BlueprintAsset(DQB1BlueprintAsset blueprint) : VoxelStreamScript
    {
        public DQB1BlueprintAsset Blueprint { get; set; } = blueprint;

        const int CHANNEL = (int)VoxelBuffer.ChannelId.ChannelType;
        public override int _GetUsedChannelsMask() => 1 << CHANNEL;

        public override int _LoadVoxelBlock(VoxelBuffer outBuffer, Vector3I positionInBlocks, int lod)
        {
            if (Blueprint is null)
                    return (int)ResultCode.BlockNotFound;

            Vector3I bufferSize = outBuffer.GetSize();
            
            if (Blueprint.BlockDictionary is null)
                Blueprint.CreateBlockDictionary();
            
            for (int x = 0; x < bufferSize.X; x++)
            {
                for (int y = 0; y < bufferSize.Y; y++)
                {
                    for (int z = 0; z < bufferSize.Z; z++)
                    {
                        Vector3I coords = (positionInBlocks * bufferSize) + new Vector3I(x, y, z);
                        if (Blueprint.BlockDictionary.TryGetValue(coords, out byte block))
                        {
                            Info.DQB1.BlockInfo blockInfo = Info.DQB1.BlockInfo.Get(block);
                            outBuffer.SetVoxel(blockInfo.VoxelID, x, y, z);
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