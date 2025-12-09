using Godot;
using System;

public partial class DrawTest : Node3D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
    {
        DrawBox();
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void DrawBox()
    {
        Aabb aabb = new(new(), Vector3.One);

        SurfaceTool st = new();
		st.Begin(Mesh.PrimitiveType.Lines);

		st.AddVertex(new Vector3(1,0,0));
		st.AddVertex(new Vector3(0,0,0));

		st.AddVertex(new Vector3(1,0,1));
		st.AddVertex(new Vector3(1,0,0));

		st.AddVertex(new Vector3(0,0,1));
		st.AddVertex(new Vector3(1,0,1));

		st.AddVertex(new Vector3(0,0,0));
		st.AddVertex(new Vector3(0,0,1));
		
		st.AddVertex(new Vector3(0,1,0));
		st.AddVertex(new Vector3(1,1,0));
		
		st.AddVertex(new Vector3(1,1,0));
		st.AddVertex(new Vector3(1,1,1));
		
		st.AddVertex(new Vector3(1,1,1));
		st.AddVertex(new Vector3(0,1,1));
		
		st.AddVertex(new Vector3(0,1,1));
		st.AddVertex(new Vector3(0,1,0));
		
		st.AddVertex(new Vector3(0,0,1));
		st.AddVertex(new Vector3(0,1,1));
		
		st.AddVertex(new Vector3(0,0,0));
		st.AddVertex(new Vector3(0,1,0));
		
		st.AddVertex(new Vector3(1,0,0));
		st.AddVertex(new Vector3(1,1,0));
		
		st.AddVertex(new Vector3(1,0,1));
		st.AddVertex(new Vector3(1,1,1));

		var x = st.Commit();
		MeshInstance3D meshInstance3D = new MeshInstance3D();
		meshInstance3D.Mesh = x;
		AddChild(meshInstance3D);

		meshInstance3D.Scale = new Vector3(1, 2, 3);
    }
}
