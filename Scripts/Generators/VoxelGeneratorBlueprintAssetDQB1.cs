using EyeOfRubiss.Info;
using Godot;
using System;
using System.Runtime.ExceptionServices;
using EyeOfRubiss.Info.DQB1;

namespace EyeOfRubiss
{
    public partial class VoxelGeneratorBlueprintAssetDQB1(BlueprintAssetDQB1 blueprint) : VoxelGeneratorScript
    {
        public BlueprintAssetDQB1 _Blueprint = blueprint;

        const int CHANNEL = (int)VoxelBuffer.ChannelId.ChannelType;

        public override int _GetUsedChannelsMask() => 1 << CHANNEL;

        public override void _GenerateBlock(VoxelBuffer outBuffer, Vector3I originInVoxels, int lod)
        {
            if (_Blueprint is null)
                return;

            Vector3I bufferSize = outBuffer.GetSize();

            for (int x = 0; x < bufferSize.X; x++)
            {
                for (int y = 0; y < bufferSize.Y; y++)
                {
                    for (int z = 0; z < bufferSize.Z; z++)
                    {
                        if (_Blueprint.GetObjectAtPosition(originInVoxels + new Vector3I(x, y, z)) is BlueprintAssetDQB1.ObjectStruct obj)
                        {
                            outBuffer.SetVoxel(BlockInfo.Get(obj.Data.Block).Voxel, x, y, z);
                        }
                    }
                }
            }
        }
    }
}