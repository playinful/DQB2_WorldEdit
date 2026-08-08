using Godot;
using System;

namespace EyeOfRubiss
{
    public partial class VoxelGeneratorEyeOfRubissStructure(EyeOfRubissStructure structure, bool showTerrain = true, bool showFluid = true, bool showPartsBlock = false) : VoxelGeneratorScript
    {
        private EyeOfRubissStructure _Structure = structure;

        public bool ShowTerrain { get; set; } = showTerrain;
        public bool ShowFluid { get; set; } = showFluid;
        public bool ShowPartsBlock { get; set; } = showPartsBlock;

        const int CHANNEL = (int)VoxelBuffer.ChannelId.ChannelType;

        public override int _GetUsedChannelsMask() => 1 << CHANNEL;

        public override void _GenerateBlock(VoxelBuffer outBuffer, Vector3I originInVoxels, int lod)
        {
            if (_Structure is null)
                return;

            if (originInVoxels.Y < 0 && !ShowPartsBlock)
            {
                outBuffer.Fill(Constants.VOXEL_FLOOR_COLLISION);
                return;
            }
            Vector3I bufferSize = outBuffer.GetSize();

            for (int x = 0; x < bufferSize.X; x++)
            {
                for (int y = 0; y < bufferSize.Y; y++)
                {
                    for (int z = 0; z < bufferSize.Z; z++)
                    {
                        Vector3I coords = originInVoxels + new Vector3I(x, y, z);
                        outBuffer.SetVoxel(GetVoxelAtPosition(_Structure, coords, ShowTerrain, ShowFluid, ShowPartsBlock), x, y, z, CHANNEL);
                    }
                }
            }
        }
    
        public static ulong GetVoxelAtPosition(EyeOfRubissStructure structure, Vector3I position, bool showTerrain = true, bool showFluid = true, bool showPartsBlock = false)
        {
            if (structure.SourceGame == 1)
            {
                byte block = (byte)structure.GetBlock(position);

                if (position.Y < 0 && block == Constants.BLOCK_AIR)
                    return Constants.VOXEL_FLOOR_COLLISION;

                Info.DQB1.BlockInfo blockInfo = Info.DQB1.BlockInfo.Get(block);
                
                if (showPartsBlock)
                    return (ulong)((int)blockInfo.PartsType + 1);
                
                ulong voxelId = blockInfo.Voxel;
                if (voxelId != Constants.VOXEL_AIR)
                    return showTerrain ? voxelId : Constants.VOXEL_TERRAIN_COLLISION;

                PartsType partsType = blockInfo.PartsType;
                FluidType fluid = FluidType.Air; // TODO

                if (partsType == PartsType.None)
                {
                    return fluid switch
                    {
                        FluidType.Water    => showFluid ? 640 : Constants.VOXEL_FLUID_COLLISION,
                        FluidType.HotWater => showFluid ? 646 : Constants.VOXEL_FLUID_COLLISION,
                        FluidType.Poison   => showFluid ? 652 : Constants.VOXEL_FLUID_COLLISION,
                        FluidType.Lava     => showFluid ? 658 : Constants.VOXEL_FLUID_COLLISION,
                        _ => Constants.VOXEL_AIR
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
                        _ => Constants.VOXEL_PARTSBLOCK
                    };
                }
            }
            else if (structure.SourceGame == 2)
            {
                ushort block = structure.GetBlock(position).GetBlockID();

                if (position.Y < 0 && block == Constants.BLOCK_AIR)
                    return Constants.VOXEL_FLOOR_COLLISION;
                
                Info.DQB2.BlockInfo blockInfo = Info.DQB2.BlockInfo.Get(block);

                if (showPartsBlock)
                    return (ulong)((int)blockInfo.GetPartsType() + 1);
                
                ulong voxelId = blockInfo.Voxel;
                if (voxelId != Constants.VOXEL_AIR)
                    return showTerrain ? voxelId : Constants.VOXEL_TERRAIN_COLLISION;
                
                PartsType partsType = blockInfo.GetPartsType();
                FluidType fluidType = blockInfo.FluidType;
                FluidLevel fluidLevel = blockInfo.FluidLevel;

                if (partsType == PartsType.None)
                {
                    return fluidType switch
                    {
                        FluidType.Water      => showFluid ? 640 : Constants.VOXEL_FLUID_COLLISION,
                        FluidType.HotWater   => showFluid ? 646 : Constants.VOXEL_FLUID_COLLISION,
                        FluidType.Poison     => showFluid ? 652 : Constants.VOXEL_FLUID_COLLISION,
                        FluidType.Lava       => showFluid ? 658 : Constants.VOXEL_FLUID_COLLISION,
                        FluidType.SwampWater => showFluid ? 664 : Constants.VOXEL_FLUID_COLLISION,
                        FluidType.MuddyWater => showFluid ? 670 : Constants.VOXEL_FLUID_COLLISION,
                        FluidType.Seawater   => showFluid ? 676 : Constants.VOXEL_FLUID_COLLISION,
                        FluidType.Plasma     => showFluid ? 682 : Constants.VOXEL_FLUID_COLLISION,
                        _ => Constants.VOXEL_AIR
                    };
                }
                else
                {
                    return fluidType switch
                    {
                        FluidType.Water      => showFluid ? 643 : Constants.VOXEL_FLUID_PARTSBLOCK_COLLISION,
                        FluidType.HotWater   => showFluid ? 649 : Constants.VOXEL_FLUID_PARTSBLOCK_COLLISION,
                        FluidType.Poison     => showFluid ? 655 : Constants.VOXEL_FLUID_PARTSBLOCK_COLLISION,
                        FluidType.Lava       => showFluid ? 661 : Constants.VOXEL_FLUID_PARTSBLOCK_COLLISION,
                        FluidType.SwampWater => showFluid ? 667 : Constants.VOXEL_FLUID_PARTSBLOCK_COLLISION,
                        FluidType.MuddyWater => showFluid ? 673 : Constants.VOXEL_FLUID_PARTSBLOCK_COLLISION,
                        FluidType.Seawater   => showFluid ? 679 : Constants.VOXEL_FLUID_PARTSBLOCK_COLLISION,
                        FluidType.Plasma     => showFluid ? 685 : Constants.VOXEL_FLUID_PARTSBLOCK_COLLISION,
                        _ => Constants.VOXEL_PARTSBLOCK
                    };
                }
            }

            return Constants.VOXEL_AIR;
        }
    }
}