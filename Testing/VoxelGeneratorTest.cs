using Godot;
using System;

public partial class VoxelGeneratorTest : VoxelGeneratorScript
{
    const int CHANNEL = (int)VoxelBuffer.ChannelId.ChannelType;

    public override int _GetUsedChannelsMask() => 1 << CHANNEL;

    public override void _GenerateBlock(VoxelBuffer outBuffer, Vector3I originInVoxels, int lod)
    {
        if (originInVoxels.Y < 0)
        {
            outBuffer.Fill(1);
        }
    }
}