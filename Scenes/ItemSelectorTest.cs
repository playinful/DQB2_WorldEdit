using Godot;
using System;

public partial class ItemSelectorTest : Window
{
    [Signal] public delegate void ItemSelectedEventHandler(int id);

    public void _On_Button_Pressed()
    {
        int id = (int)GetNode<SpinBox>("SpinBox").Value;

        GD.Print($"Item selected: {id}");

        EmitSignal(SignalName.ItemSelected, id);
    }
}
