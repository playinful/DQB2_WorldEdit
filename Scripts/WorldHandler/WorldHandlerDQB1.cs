using EyeOfRubiss.Scenes;
using Godot;
using System;
using EyeOfRubiss.Info;

namespace EyeOfRubiss
{
    public class WorldHandlerDQB1(WorldEditorScene worldEditorScene) : WorldHandler(worldEditorScene)
    {
        public WorldData _WorldData;

        public override string GetDebugInfo(Vector3I position)
        {
            byte block = _WorldData.GetBlockAtPosition(position);
            Vector3I dataPosition = WorldData.PositionToDataPosition(position);

            return
      			$"Targeted block: {Info.DQB1.BlockInfo.Get(block).Name + $" [{block}]"}\n" +
      			$"X: {position.X}, Y: {position.Y}, Z: {position.Z}\n" +
                $"Chunk: {dataPosition.X}, Layer: {dataPosition.Y}, Tile: {dataPosition.Z}";
        }

        public void LoadWorldData(WorldData worldData)
        {
            _WorldData = worldData;
            _WorldEditorScene._VoxelTerrain.Generator = new VoxelGeneratorDQB1(worldData);
        }
        public void UnloadWorldData()
        {
            _WorldData = null;
            _WorldEditorScene._VoxelTerrain.Generator = null;
        }
    }
}