using Godot;
using System;

[Tool]
public partial class GridMapTest2 : EditorScript
{
	public override void _Run()
	{
		GD.Print("running running and running running");

		foreach (Node node in GetScene().GetChildren())
		{
			GD.Print(node.Name);
		}
	}
}
