using EyeOfRubiss.Scenes;
using Godot;
using System;
using EyeOfRubiss.Info;

namespace EyeOfRubiss
{
    public class WorldHandlerBlueprintAssetDQB1(WorldEditorScene worldEditorScene) : WorldHandler(worldEditorScene)
    {
        BlueprintAssetDQB1 _Blueprint;

        public void Load(BlueprintAssetDQB1 blueprint)
        {
            blueprint.CreateObjectDictionary();
            _Blueprint = blueprint;
            _WorldEditorScene._VoxelTerrain.Generator = new VoxelGeneratorBlueprintAssetDQB1(blueprint);
        }
        public void Unload()
        {
            _Blueprint = null;
            _WorldEditorScene._VoxelTerrain.Generator = null;
        }
    }
}