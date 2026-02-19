using Godot;
using System;

public partial class SelectionBox : Node3D
{
	[Export] public Mesh CornerMesh;
	[Export] public Mesh SideMesh;
	[Export] public BaseMaterial3D MaterialOverride;

	private MeshInstance3D[][][] _Corners;
	private MeshInstance3D[] _Sides;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_CreateParts();
		SetSize(Vector3.One);
	}
	private void _CreateParts()
	{
		if (CornerMesh is not null)
			_CreateCorners();
		if (SideMesh is not null)
			_CreateSides();
	}
	private void _CreateCorners()
	{
		MeshInstance3D x0y0z0 = new MeshInstance3D();
		x0y0z0.Mesh = CornerMesh;
		x0y0z0.MaterialOverride = MaterialOverride;
		x0y0z0.Rotation = new Vector3(0, -1, 1) * (Mathf.Pi / 2);
		AddChild(x0y0z0);

		MeshInstance3D x1y0z0 = new MeshInstance3D();
		x1y0z0.Mesh = CornerMesh;
		x1y0z0.MaterialOverride = MaterialOverride;
		x1y0z0.Rotation = new Vector3(0, -2, 1) * (Mathf.Pi / 2);
		AddChild(x1y0z0);

		MeshInstance3D x0y1z0 = new MeshInstance3D();
		x0y1z0.Mesh = CornerMesh;
		x0y1z0.MaterialOverride = MaterialOverride;
		x0y1z0.Rotation = new Vector3(-1, -1, 1) * (Mathf.Pi / 2);
		AddChild(x0y1z0);

		MeshInstance3D x1y1z0 = new MeshInstance3D();
		x1y1z0.Mesh = CornerMesh;
		x1y1z0.MaterialOverride = MaterialOverride;
		x1y1z0.Rotation = new Vector3(-1, -2, 1) * (Mathf.Pi / 2);
		AddChild(x1y1z0);

		MeshInstance3D x0y0z1 = new MeshInstance3D();
		x0y0z1.Mesh = CornerMesh;
		x0y0z1.MaterialOverride = MaterialOverride;
		x0y0z1.Rotation = new Vector3(0, 0, 1) * (Mathf.Pi / 2);
		AddChild(x0y0z1);

		MeshInstance3D x1y0z1 = new MeshInstance3D();
		x1y0z1.Mesh = CornerMesh;
		x1y0z1.MaterialOverride = MaterialOverride;
		x1y0z1.Rotation = new Vector3(0, 1, 1) * (Mathf.Pi / 2);
		AddChild(x1y0z1);

		MeshInstance3D x0y1z1 = new MeshInstance3D();
		x0y1z1.Mesh = CornerMesh;
		x0y1z1.MaterialOverride = MaterialOverride;
		x0y1z1.Rotation = new Vector3(-1, 0, 1) * (Mathf.Pi / 2);
		AddChild(x0y1z1);

		MeshInstance3D x1y1z1 = new MeshInstance3D();
		x1y1z1.Mesh = CornerMesh;
		x1y1z1.MaterialOverride = MaterialOverride;
		x1y1z1.Rotation = new Vector3(-1, 1, 1) * (Mathf.Pi / 2);
		AddChild(x1y1z1);

		_Corners = [
			[
				[x0y0z0, x0y0z1], [x0y1z0, x0y1z1]
			],
			[
				[x1y0z0, x1y0z1], [x1y1z0, x1y1z1]
			]
		];
	}
	private void _CreateSides()
	{
		_Sides = new MeshInstance3D[12];

		MeshInstance3D x0y0z0_x1y0z0 = new MeshInstance3D();
		x0y0z0_x1y0z0.Mesh = SideMesh;
		x0y0z0_x1y0z0.MaterialOverride = MaterialOverride;
		x0y0z0_x1y0z0.Rotation = new Vector3(0, 0, 0) * (Mathf.Pi / 2);
		AddChild(x0y0z0_x1y0z0);
		_Sides[0] = x0y0z0_x1y0z0;

		MeshInstance3D x0y1z0_x1y1z0 = new MeshInstance3D();
		x0y1z0_x1y1z0.Mesh = SideMesh;
		x0y1z0_x1y1z0.MaterialOverride = MaterialOverride;
		x0y1z0_x1y1z0.Rotation = new Vector3(0, 0, 0) * (Mathf.Pi / 2);
		AddChild(x0y1z0_x1y1z0);
		_Sides[1] = x0y1z0_x1y1z0;

		MeshInstance3D x0y0z1_x1y0z1 = new MeshInstance3D();
		x0y0z1_x1y0z1.Mesh = SideMesh;
		x0y0z1_x1y0z1.MaterialOverride = MaterialOverride;
		x0y0z1_x1y0z1.Rotation = new Vector3(0, 0, 0) * (Mathf.Pi / 2);
		AddChild(x0y0z1_x1y0z1);
		_Sides[2] = x0y0z1_x1y0z1;

		MeshInstance3D x0y1z1_x1y1z1 = new MeshInstance3D();
		x0y1z1_x1y1z1.Mesh = SideMesh;
		x0y1z1_x1y1z1.MaterialOverride = MaterialOverride;
		x0y1z1_x1y1z1.Rotation = new Vector3(0, 0, 0) * (Mathf.Pi / 2);
		AddChild(x0y1z1_x1y1z1);
		_Sides[3] = x0y1z1_x1y1z1;

		MeshInstance3D x0y0z0_x0y1z0 = new MeshInstance3D();
		x0y0z0_x0y1z0.Mesh = SideMesh;
		x0y0z0_x0y1z0.MaterialOverride = MaterialOverride;
		x0y0z0_x0y1z0.Rotation = new Vector3(0, 0, 1) * (Mathf.Pi / 2);
		AddChild(x0y0z0_x0y1z0);
		_Sides[4] = x0y0z0_x0y1z0;

		MeshInstance3D x1y0z0_x1y1z0 = new MeshInstance3D();
		x1y0z0_x1y1z0.Mesh = SideMesh;
		x1y0z0_x1y1z0.MaterialOverride = MaterialOverride;
		x1y0z0_x1y1z0.Rotation = new Vector3(0, 0, 1) * (Mathf.Pi / 2);
		AddChild(x1y0z0_x1y1z0);
		_Sides[5] = x1y0z0_x1y1z0;

		MeshInstance3D x0y0z1_x0y1z1 = new MeshInstance3D();
		x0y0z1_x0y1z1.Mesh = SideMesh;
		x0y0z1_x0y1z1.MaterialOverride = MaterialOverride;
		x0y0z1_x0y1z1.Rotation = new Vector3(0, 0, 1) * (Mathf.Pi / 2);
		AddChild(x0y0z1_x0y1z1);
		_Sides[6] = x0y0z1_x0y1z1;

		MeshInstance3D x1y0z1_x1y1z1 = new MeshInstance3D();
		x1y0z1_x1y1z1.Mesh = SideMesh;
		x1y0z1_x1y1z1.MaterialOverride = MaterialOverride;
		x1y0z1_x1y1z1.Rotation = new Vector3(0, 0, 1) * (Mathf.Pi / 2);
		AddChild(x1y0z1_x1y1z1);
		_Sides[7] = x1y0z1_x1y1z1;

		MeshInstance3D x0y0z0_x0y0z1 = new MeshInstance3D();
		x0y0z0_x0y0z1.Mesh = SideMesh;
		x0y0z0_x0y0z1.MaterialOverride = MaterialOverride;
		x0y0z0_x0y0z1.Rotation = new Vector3(0, -1, 0) * (Mathf.Pi / 2);
		AddChild(x0y0z0_x0y0z1);
		_Sides[8] = x0y0z0_x0y0z1;

		MeshInstance3D x1y0z0_x1y0z1 = new MeshInstance3D();
		x1y0z0_x1y0z1.Mesh = SideMesh;
		x1y0z0_x1y0z1.MaterialOverride = MaterialOverride;
		x1y0z0_x1y0z1.Rotation = new Vector3(0, -1, 0) * (Mathf.Pi / 2);
		AddChild(x1y0z0_x1y0z1);
		_Sides[9] = x1y0z0_x1y0z1;

		MeshInstance3D x0y1z0_x0y1z1 = new MeshInstance3D();
		x0y1z0_x0y1z1.Mesh = SideMesh;
		x0y1z0_x0y1z1.MaterialOverride = MaterialOverride;
		x0y1z0_x0y1z1.Rotation = new Vector3(0, -1, 0) * (Mathf.Pi / 2);
		AddChild(x0y1z0_x0y1z1);
		_Sides[10] = x0y1z0_x0y1z1;

		MeshInstance3D x1y1z0_x1y1z1 = new MeshInstance3D();
		x1y1z0_x1y1z1.Mesh = SideMesh;
		x1y1z0_x1y1z1.MaterialOverride = MaterialOverride;
		x1y1z0_x1y1z1.Rotation = new Vector3(0, -1, 0) * (Mathf.Pi / 2);
		AddChild(x1y1z0_x1y1z1);
		_Sides[11] = x1y1z0_x1y1z1;
	}

	public void SetSize(Vector3 size)
	{
		// Corners
		if (_Corners is not null)
		{
			for (int x = 0; x < 2; x++)
			{
				for (int y = 0; y < 2; y++)
				{
					for (int z = 0; z < 2; z++)
					{
						_Corners[x][y][z].Position = new Vector3(x, y, z) * size;
					}
				}
			}
		}
		
		// Sides
		if (_Sides is not null)
		{
			// x0y0z0 - x1y0z0
			_Sides[0].Position = new Vector3(0.5f, 0, 0);
			_Sides[0].Scale = new Vector3(size.X - 1, 1, 1);
			
			// x0y1z0 - x1y1z0
			_Sides[1].Position = new Vector3(0.5f, size.Y, 0);
			_Sides[1].Scale = new Vector3(size.X - 1, 1, 1);
			
			// x0y0z1 - x1y0z1
			_Sides[2].Position = new Vector3(0.5f, 0, size.Z);
			_Sides[2].Scale = new Vector3(size.X - 1, 1, 1);
			
			// x0y1z1 - x1y1z1
			_Sides[3].Position = new Vector3(0.5f, size.Y, size.Z);
			_Sides[3].Scale = new Vector3(size.X - 1, 1, 1);
			
			// x0y0z0 - x0y1z0
			_Sides[4].Position = new Vector3(0, 0.5f, 0);
			_Sides[4].Scale = new Vector3(size.Y - 1, 1, 1);
			
			// x1y0z0 - x1y1z0
			_Sides[5].Position = new Vector3(size.X, 0.5f, 0);
			_Sides[5].Scale = new Vector3(size.Y - 1, 1, 1);
			
			// x0y0z1 - x0y1z1
			_Sides[6].Position = new Vector3(0, 0.5f, size.Z);
			_Sides[6].Scale = new Vector3(size.Y - 1, 1, 1);
			
			// x1y0z1 - x1y1z1
			_Sides[7].Position = new Vector3(size.X, 0.5f, size.Z);
			_Sides[7].Scale = new Vector3(size.Y - 1, 1, 1);
			
			// x0y0z0 - x0y0z1
			_Sides[8].Position = new Vector3(0, 0, 0.5f);
			_Sides[8].Scale = new Vector3(size.Z - 1, 1, 1);
			
			// x1y0z0 - x1y0z1
			_Sides[9].Position = new Vector3(size.X, 0, 0.5f);
			_Sides[9].Scale = new Vector3(size.Z - 1, 1, 1);
			
			// x0y1z0 - x0y1z1
			_Sides[10].Position = new Vector3(0, size.Y, 0.5f);
			_Sides[10].Scale = new Vector3(size.Z - 1, 1, 1);
			
			// x1y1z0 - x1y1z1
			_Sides[11].Position = new Vector3(size.X, size.Y, 0.5f);
			_Sides[11].Scale = new Vector3(size.Z - 1, 1, 1);
		}
	}
}
