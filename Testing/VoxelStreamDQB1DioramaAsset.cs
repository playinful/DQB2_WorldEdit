using EyeOfRubiss.Info;
using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Text.Json.Serialization;

namespace EyeOfRubiss
{
    /// <summary> VoxelStreamScript that streams a DQB2 StageData instance as voxel data. </summary>
    public partial class VoxelStreamDQB1DioramaAsset : VoxelStreamScript
    {
        public int SizeX { get; set; }
        public int SizeY { get; set; }
        public int SizeZ { get; set; }

        public List<byte> Blocks { get; set; }

        const int CHANNEL = (int)VoxelBuffer.ChannelId.ChannelType;
        public override int _GetUsedChannelsMask() => 1 << CHANNEL;

        public VoxelStreamDQB1DioramaAsset(int sizeX, int sizeY, int sizeZ, byte[] blocks)
        {
            SizeX = sizeX;
            SizeY = sizeY;
            SizeZ = sizeZ;

            Blocks = [];
            for (int i = 0; i + 1 < blocks.Length; i += 2)
            {
                if (blocks[i] == 0 && blocks[i + 1] == 0)
                {
                    Blocks = [];
                    continue;
                }
                for (int j = 1; j <= blocks[i]; j++)
                {
                    Blocks.Add(blocks[i + 1]);
                }
            }
            GD.Print(string.Join(',', Array.ConvertAll(Blocks.ToArray(), block => block.ToString())));
        }

        public override int _LoadVoxelBlock(VoxelBuffer outBuffer, Vector3I positionInBlocks, int lod)
        {
            if (Blocks is null)
                return (int)ResultCode.BlockNotFound;

            Vector3I bufferSize = outBuffer.GetSize();
            Vector3I origin = bufferSize * positionInBlocks;

            if (positionInBlocks.Y == -1 && positionInBlocks.X >= 0 && positionInBlocks.Z >= 0)
            {
                for (int x = 0; x < bufferSize.X && origin.X + x < SizeX; x++)
                {
                    for (int z = 0; z < bufferSize.Z && origin.Z + z < SizeZ; z++)
                    {
                        outBuffer.SetVoxel(1, x, bufferSize.Y - 1, z);
                    }
                }
            }
            if (positionInBlocks.X >= 0 && positionInBlocks.Y >= 0 && positionInBlocks.Z >= 0)
            {
                for (int x = 0; x < bufferSize.X && origin.X + x < SizeX; x++)
                {
                    for (int y = 0; y < bufferSize.Y && origin.Y + y < SizeY; y++)
                    {
                        for (int z = 0; z < bufferSize.Z && origin.Z + z < SizeZ; z++)
                        {
                            Vector3I coords = new Vector3I(x, y, z) + origin;
                            int index = coords.Z * SizeX * SizeY + coords.X * SizeY + coords.Y;
                            Info.DQB1.BlockInfo blockInfo = Info.DQB1.BlockInfo.Get(Blocks[index]);
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