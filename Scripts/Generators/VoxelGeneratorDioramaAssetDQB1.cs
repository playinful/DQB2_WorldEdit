using EyeOfRubiss.Info;
using Godot;
using System;
using System.Runtime.ExceptionServices;
using EyeOfRubiss.Info.DQB1;

namespace EyeOfRubiss
{
    public partial class VoxelGeneratorDioramaAssetDQB1(DioramaAssetDQB1 diorama) : VoxelGeneratorScript
    {
        public DioramaAssetDQB1 _Diorama = diorama;

        const int CHANNEL = (int)VoxelBuffer.ChannelId.ChannelType;

        public override int _GetUsedChannelsMask() => 1 << CHANNEL;

        public override void _GenerateBlock(VoxelBuffer outBuffer, Vector3I originInVoxels, int lod)
        {
            if (_Diorama is null || _Diorama.Header is null || _Diorama.Data is null || _Diorama.Blocks is null)
                return;

            Vector3I bufferSize = outBuffer.GetSize();

            if (originInVoxels.X >= 0 && originInVoxels.Y >= 0 && originInVoxels.Z >= 0)
            {
                for (int x = 0; x < bufferSize.X && originInVoxels.X + x < _Diorama.Header.SizeX; x++)
                {
                    for (int y = 0; y < bufferSize.Y && originInVoxels.Y + y < _Diorama.Header.SizeY; y++)
                    {
                        for (int z = 0; z < bufferSize.Z && originInVoxels.Z + z < _Diorama.Header.SizeZ; z++)
                        {
                            Vector3I coords = new Vector3I(x, y, z) + originInVoxels;
                            byte block = _Diorama.GetBlock(coords);
                            Info.DQB1.BlockInfo blockInfo = Info.DQB1.BlockInfo.Get(block);
                            outBuffer.SetVoxel(blockInfo.Voxel, x, y, z);
                        }
                    }
                }
            }
        }
    }
}