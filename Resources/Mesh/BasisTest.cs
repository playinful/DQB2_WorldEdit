using Godot;
using System;

public partial class BasisTest : Node
{
	public override void _Ready()
	{
		Basis basis = Basis.Identity;
		GD.Print(basis);
		Basis basis2 = basis.Rotated(Vector3.Up, 1);
		GD.Print(basis2);
		
	}
}
