using Godot;
using System;

public partial class OptionButtonWithWriteInTest : Control
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		OptionButton optionButton = GetNode<OptionButton>("OptionButton");
		GD.Print("OptionButton");
		optionButton.Select(1);

		OptionButtonWithWriteIn optionButtonWithWriteIn = GetNode<OptionButtonWithWriteIn>("OptionButton");
		GD.Print("OptionButtonWithWriteIn");
		optionButtonWithWriteIn.Select(2);
	}
}
