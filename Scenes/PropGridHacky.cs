using Godot;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Linq.Expressions;
using System.Runtime.InteropServices;

public partial class PropGridHacky : GridMap
{
    private static BGPartsModel[] _ModelParams;

    public void SetupLibraries()
    {
        MeshLibrary = new MeshLibrary();
    }

    public void SetCellItemDelegated(Vector3I position, int item, int orientation = 0)
    {
        if (MeshLibrary is null)
            SetupLibraries();

        SetModel(item);
        SetCellItem(position, item, orientation);
    }
    public void ClearCellItem(Vector3I position)
    {
        SetCellItem(position, -1);
    }
    public void ClearSubGrid()
    {
        
    }

    public void SetModel(int item)
    {
        if (!MeshLibrary.GetItemList().Contains(item) && GetModelParam(item) is BGPartsModel param)
        {
            MeshLibrary.CreateItem(item);

            if (string.IsNullOrEmpty(param.Mesh))
                return;

            Mesh mesh = ResourceLoader.Load<Mesh>(param.Mesh, cacheMode: ResourceLoader.CacheMode.Ignore);
            MeshLibrary.SetItemMesh(item, mesh);

            for (int i = 0; i < mesh.GetSurfaceCount() && i < param.Materials.Length; i++)
            {
                StandardMaterial3D material = new();
                BGPartsModel.BGPartsModelMaterial matParam = param.Materials[i];

                material.AlbedoTexture = ResourceLoader.Load<Texture2D>(matParam.Texture);
                
                material.Transparency = matParam.Transparent ? BaseMaterial3D.TransparencyEnum.AlphaScissor : BaseMaterial3D.TransparencyEnum.Disabled;
                material.CullMode = matParam.BackfaceCulling ? BaseMaterial3D.CullModeEnum.Back : BaseMaterial3D.CullModeEnum.Disabled;

                if (string.IsNullOrEmpty(matParam.Mask))
                {
                    material.AlbedoColor = Color.FromHtml(matParam.Color);
                }
                else
                {
                    // TODO
                }

                mesh.SurfaceSetMaterial(i, material);   
            }

            MeshLibrary.SetItemMeshTransform(item, new Transform3D{Basis=Basis.Identity, Origin=param.GetMeshOffset()});
        }
    }

    public void LoadModelParam(bool forceReload = false)
    {
        if (forceReload || _ModelParams is null)
        {
            _ModelParams = JsonSerializer.Deserialize<BGPartsModel[]>(Godot.FileAccess.GetFileAsString("res://Info/BGPartsModel.json"));
        }
    }
    public BGPartsModel GetModelParam(int id)
    {
        LoadModelParam();

        return _ModelParams.FirstOrDefault(model => model.ID == id);
    }

    public struct BGPartsModel
    {
        public int ID { get; set; }
        public string Mesh { get; set; }
        public BGPartsModelMaterial[] Materials { get; set; }
        public float MeshOffsetX { get; set; }
        public float MeshOffsetY { get; set; }
        public float MeshOffsetZ { get; set; }
        public Vector3 GetMeshOffset() => new(MeshOffsetX, MeshOffsetY, MeshOffsetZ);
        
        public struct BGPartsModelMaterial
        {
            public string Texture { get; set; }
            public bool Transparent { get; set; }
            public bool BackfaceCulling { get; set; }
            public string Mask { get; set; }
            public string Color { get; set; }
        }
    }
}
