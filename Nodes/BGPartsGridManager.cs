using Godot;
using System;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using EyeOfRubiss;

public partial class BGPartsGridManager : Node3D
{
	[Export] public int GridMapsMaximum;

	private List<GridMap> _GridMaps = [];
	private Dictionary<Vector3I, List<CellItem>> _CellItems = [];

    private static BGPartsModel[] _ModelParams;

	public MeshLibrary MeshLibrary { get; private set; } = new MeshLibrary();

	public void AddCellItem(Vector3I position, int item, int orientation = 0)
	{
		if (_CellItems.ContainsKey(position))
		{
			_CellItems[position].Add(new CellItem(item, orientation));
		}
		else
		{
			_CellItems.Add(position, [new CellItem(item, orientation)]);
		}

		SetModel(item);
		UpdateCell(position);
	}
	public void ClearCellItem(Vector3I position)
	{
		if (_CellItems.ContainsKey(position))
		{
			_CellItems.Remove(position);
			UpdateCell(position);
		}
	}
	public void Clear()
	{
		Vector3I[] keys = [.. _CellItems.Keys];
		foreach (Vector3I key in keys)
		{
			_CellItems.Remove(key);
			UpdateCell(key);
		}
	}

	private void UpdateCell(Vector3I position)
	{
		if (!_CellItems.ContainsKey(position))
		{
			foreach (GridMap gridMap in _GridMaps)
			{
				gridMap.SetCellItem(position, -1);
			}
			return;
		}

		for (int i = 0; i < GridMapsMaximum; i++)
		{
			if (i < _CellItems[position].Count)
			{
				CellItem item = _CellItems[position][i];

				if (i >= _GridMaps.Count)
				{
					AddGridMap();
				}

				GridMap gridMap = _GridMaps[i];

				gridMap.SetCellItem(position, item.ID, item.Direction);
			}
			else
			{
				if (i < _GridMaps.Count)
				{
					GridMap gridMap = _GridMaps[i];
					gridMap.SetCellItem(position, -1);
				}
				else
				{
					break;
				}
			}
		}
	}

	private void AddGridMap()
	{
        GridMap newGridMap = new GridMap
        {
            CellSize = Vector3.One,
            CollisionLayer = 0,
            CollisionMask = 0,
			MeshLibrary = MeshLibrary
        };

		AddChild(newGridMap);
		_GridMaps.Add(newGridMap);
    }

    public void SetModel(int item)
    {
        if (item >= 0 && !MeshLibrary.GetItemList().Contains(item) && GetModelParam(item) is BGPartsModel param)
        {
            MeshLibrary.CreateItem(item);

            if (string.IsNullOrEmpty(param.Mesh))
                return;

            Mesh mesh = ResourceLoader.Load<Mesh>(param.Mesh, cacheMode: ResourceLoader.CacheMode.Ignore);
            MeshLibrary.SetItemMesh(item, mesh);

            for (int i = 0; i < mesh.GetSurfaceCount() && i < param.Materials.Length; i++)
            {
                ShaderMaterial material = new();
                BGPartsModel.BGPartsModelMaterial matParam = param.Materials[i];
                
                if (matParam.BackfaceCulling)
                {
                    material.Shader = ResourceLoader.Load<Shader>("res://Resources/BGParts.gdshader");
                }
                else
                {
                    material.Shader = ResourceLoader.Load<Shader>("res://Resources/BGParts_culldisabled.gdshader");
                }

				if (!string.IsNullOrEmpty(matParam.Texture))
                	material.SetShaderParameter("albedo_texture", ResourceLoader.Load<Texture2D>(matParam.Texture));

                material.SetShaderParameter("color", Color.FromHtml(matParam.Color));

                if (!string.IsNullOrEmpty(matParam.Mask))
                {
                    material.SetShaderParameter("color_mask", ResourceLoader.Load<Texture2D>(matParam.Mask));
                }

                material.SetShaderParameter("alpha_scissor_threshold", matParam.Transparent ? 0.5f : 0.0f);

                mesh.SurfaceSetMaterial(i, material);
            }

			Basis basis = new(param.MeshSizeX, 0, 0, 0, param.MeshSizeY, 0, 0, 0, param.MeshSizeZ);
            MeshLibrary.SetItemMeshTransform(item, new Transform3D{Basis=basis, Origin=param.GetMeshOffset() + new Vector3(0, -0.5f, 0)});
        }
    }

	private struct CellItem(int id, int direction)
	{
		public int ID = id;
		public int Direction = direction;
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

    public class BGPartsModel
    {
        public int ID { get; set; }
		public string Name { get; set; }
        public string Mesh { get; set; }
        public BGPartsModelMaterial[] Materials { get; set; }
        public float MeshOffsetX { get; set; }
        public float MeshOffsetY { get; set; }
        public float MeshOffsetZ { get; set; }
        public Vector3 GetMeshOffset() => new(MeshOffsetX, MeshOffsetY, MeshOffsetZ);
		public float MeshSizeX { get; set; } = 1;
		public float MeshSizeY { get; set; } = 1;
		public float MeshSizeZ { get; set; } = 1;
        
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
