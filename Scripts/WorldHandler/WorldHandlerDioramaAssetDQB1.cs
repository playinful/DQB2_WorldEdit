using EyeOfRubiss.Scenes;
using Godot;
using System;
using EyeOfRubiss.Info;

namespace EyeOfRubiss
{
    public class WorldHandlerDioramaAssetDQB1(WorldEditorScene worldEditorScene) : WorldHandler(worldEditorScene)
    {
        public DioramaAssetDQB1 Diorama = new();

        public void LoadHeader(DioramaHeaderAssetDQB1 header)
        {
            Diorama.Header = header;
            Reload();
        }
        public void UnloadHeader()
        {
            Diorama.Header = null;
            Reload();
        }

        public void LoadData(DioramaDataAssetDQB1 data)
        {
            Diorama.Data = data;
            Reload();
        }
        public void UnloadData()
        {
            Diorama.Data = null;
            Reload();
        }

        public override void Reload()
        {
            if (Diorama is not null && Diorama.Header is not null && Diorama.Data is not null)
            {
                Diorama.CreateBlockList();
                _WorldEditorScene._VoxelTerrain.Generator = new VoxelGeneratorDioramaAssetDQB1(Diorama);
            }
            else
            {
                _WorldEditorScene._VoxelTerrain.Generator = null;
            }
        }
    }
}