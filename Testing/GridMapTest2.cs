/*
 * Not sure what this was doing, but my `dotnet publish` was failing so I
 * commented this file out and now it works...
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
*/