using EyeOfRubiss.Info;
using Godot;
using System;
using System.Formats.Tar;
using System.Reflection.Metadata;
using System.Runtime.ExceptionServices;
using System.Transactions;

namespace EyeOfRubiss
{
    public partial class VoxelGeneratorDQB1(WorldData worldData, bool showTerrain = true, bool showFluid = true, bool showPartsBlock = false) : VoxelGeneratorScript
    {
        private WorldData _WorldData { get; set; } = worldData;

        public bool ShowTerrain { get; set; } = showTerrain;
        public bool ShowFluid { get; set; } = showFluid;
        public bool ShowPartsBlock { get; set; } = showPartsBlock;

        const int CHANNEL = (int)VoxelBuffer.ChannelId.ChannelType;

        public override int _GetUsedChannelsMask() => 1 << CHANNEL;

        public override void _GenerateBlock(VoxelBuffer outBuffer, Vector3I originInVoxels, int lod)
        {
            if (_WorldData is null)
                    return;

            if (originInVoxels.Y < 0 && !ShowPartsBlock)
            {
                outBuffer.Fill(Constants.VOXEL_FLOOR_COLLISION);
                return;
            }
            Vector3I bufferSize = outBuffer.GetSize();

            //Step 1: check if inbounds
            if (!WorldData.PositionIsInBounds(originInVoxels))
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
                        outBuffer.SetVoxel(GetVoxelAtPosition(_WorldData, coords, ShowTerrain, ShowFluid, ShowPartsBlock), x, y, z, CHANNEL);
                    }
                }
            }
        }

        public static ulong GetVoxelAtPosition(WorldData worldData, Vector3I position, bool showTerrain = true, bool showFluid = true, bool showPartsBlock = false)
        {
            if (position.Y < 0 && !showPartsBlock)
                return Constants.VOXEL_FLOOR_COLLISION;

            byte block = worldData.GetBlockAtPosition(position);
            Info.DQB1.BlockInfo blockInfo = Info.DQB1.BlockInfo.Get(block);

            if (showPartsBlock)
                    return (ulong)((int)blockInfo.PartsType + 1);
                
            ulong voxelId = blockInfo.Voxel;

            if (voxelId != Constants.VOXEL_AIR)
                return showTerrain ? voxelId : Constants.VOXEL_TERRAIN_COLLISION;

            PartsType partsType = blockInfo.PartsType;
            FluidType fluid = worldData.GetFluidAtPosition(position);

            if (partsType == PartsType.None)
            {
                return fluid switch
                {
                    FluidType.Water    => showFluid ? 640 : Constants.VOXEL_FLUID_COLLISION,
                    FluidType.HotWater => showFluid ? 646 : Constants.VOXEL_FLUID_COLLISION,
                    FluidType.Poison   => showFluid ? 652 : Constants.VOXEL_FLUID_COLLISION,
                    FluidType.Lava     => showFluid ? 658 : Constants.VOXEL_FLUID_COLLISION,
                    _ => Constants.VOXEL_AIR,
                };
            }
            else
            {
                return fluid switch
                {
                    FluidType.Water    => showFluid ? 643 : Constants.VOXEL_FLUID_PARTSBLOCK_COLLISION,
                    FluidType.HotWater => showFluid ? 649 : Constants.VOXEL_FLUID_PARTSBLOCK_COLLISION,
                    FluidType.Poison   => showFluid ? 655 : Constants.VOXEL_FLUID_PARTSBLOCK_COLLISION,
                    FluidType.Lava     => showFluid ? 661 : Constants.VOXEL_FLUID_PARTSBLOCK_COLLISION,
                    _ => Constants.VOXEL_PARTSBLOCK,
                };
            }
        }
    }
}