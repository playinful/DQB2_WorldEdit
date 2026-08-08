using Godot;
using System;
using System.Net.Http.Headers;

public partial class OptionButtonWithWriteIn : OptionButton
{
	private LineEdit _LineEdit;

    public override void _Ready()
    {
		_LineEdit = new LineEdit();
		_LineEdit.Size = Size - new Vector2(20, 0);
		_LineEdit.Visible = false;
		AddChild(_LineEdit);
		AddItem("");
    }

	public new void Select(int selection)
	{
		GD.Print($"new select {selection}");
		base.Select(selection);
	}

	public void _On_ItemSelected()
	{
		
	}
}
