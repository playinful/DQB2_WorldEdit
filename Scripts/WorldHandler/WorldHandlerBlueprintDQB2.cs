using EyeOfRubiss.Scenes;
using Godot;
using System;
using EyeOfRubiss.Info.DQB2;

namespace EyeOfRubiss
{
    public class WorldHandlerBlueprintDQB2(WorldEditorScene worldEditorScene) : WorldHandler(worldEditorScene)
    {
        private Blueprint _Blueprint;

        public override string GetDebugInfo(Vector3I position)
        {
            Blueprint.BlueprintBlockInstance block = _Blueprint.GetBlock(position);

            if (block is not null)
                return
	    			$"Targeted block: {BlockInfo.Get(block.BlockID).Name + $" [{block.BlockID}]"}\n" +
	    			$"X: {position.X}, Y: {position.Y}, Z: {position.Z}\n";
                    // TODO chisel
            else
                return
                    "Targeted block: Error\n" +
                    $"X: {position.X}, Y: {position.Y}, Z: {position.Z}";
        }

        public void Load(Blueprint blueprint)
        {
            _Blueprint = blueprint;

            _WorldEditorScene._VoxelTerrain.Generator = new VoxelGeneratorBlueprintDQB2(_Blueprint);
            CreateBGParts(_Blueprint);
        }
        public void Unload()
        {
            _Blueprint = null;

			_WorldEditorScene._VoxelTerrain.Generator = null;
			_WorldEditorScene._VoxelTerrain_PropShells.Generator = null;
            
			DestroyBGParts();
        }

        public void CreateBGParts(Blueprint blueprint)
        {
            for (int x = 0; x < blueprint.SizeX; x++)
            {
                for (int y = 0; y < blueprint.SizeY; y++)
                {
                    for (int z = 0; z < blueprint.SizeZ; z++)
                    {
                        Blueprint.BlueprintBlockInstance block = blueprint.GetBlock(new Vector3I(x, y, z));
                        if (BGPartsInfo.Get(block.PropID).Mesh is int meshId)
                        {
                            _WorldEditorScene._BGPartsGrid.SetCellItemDelegated(new Vector3I(x, y, z), meshId, Util.GridMapRotationFromDirection(block.Direction));
                        }
                    }
                }
            }
        }
        public void DestroyBGParts()
        {
            _WorldEditorScene._BGPartsGrid.Clear();
        }
    }
}