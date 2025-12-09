using EyeOfRubiss;
using Godot;
using System;

public partial class SignalConnectTest : Node2D
{
    public override void _Ready()
    {
        Button button = GetNode<Button>("Button");
        Label label1 = GetNode<Label>("Label");
        Label label2 = GetNode<Label>("Label2");

        button.Pressed += DummyFunction1;
        button.Pressed += DummyFunction2;
        button.Pressed += () => DummyFunction3("Argument");
        button.Pressed += () => GD.Print("Lambda expression");
    }

    public void DummyFunction1()
    {
        GD.Print("Dummy function 1");
    }
    public void DummyFunction2()
    {
        GD.Print("Dummy function 2");
    }
    public void DummyFunction3(string text)
    {
        GD.Print("Dummy function 3 " + text);
    }

    public void TryDisconnectAll()
    {
        GetNode<Button>("Button").DisconnectAll(BaseButton.SignalName.Pressed);
    }
}
