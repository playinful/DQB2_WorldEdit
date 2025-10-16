using EyeOfRubiss;
using Gizmo3DPlugin;
using Godot;
using System;

public partial class Gizmo3dTest : Node3D
{
    [Export] private Node3D _GizmoTarget;
    [Export] private Gizmo3D _Gizmo;

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed(Constants.Controls.TEST1))
        {
            _Gizmo.ClearSelection();
            _Gizmo.Select(_GizmoTarget);
        }
    }
}
