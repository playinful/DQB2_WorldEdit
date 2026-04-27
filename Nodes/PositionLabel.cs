using Godot;
using System;

namespace EyeOfRubiss.Nodes
{
    public partial class PositionLabel : Label
    {
        [Export(PropertyHint.MultilineText)] string DisplayText { get; set; } = "Current position: %s\nFacing: %t";
        [Export] Node Target { get; set; }
        [Export] bool UseGlobalPosition { get; set;} = false;

        public override void _Process(double delta)
        {
            if (!Visible)
                return;

            if (Target is Node2D target2D)
            {
                Text = DisplayText.Replace("%s", UseGlobalPosition ? target2D.GlobalPosition.ToString() : target2D.Position.ToString());
            }
            else if (Target is Node3D target3D)
            {
                Text = DisplayText.Replace("%s", UseGlobalPosition ? target3D.GlobalPosition.ToString() : target3D.Position.ToString())
                    .Replace("%t", _GetDirectionString(target3D.GetFacingDirection(UseGlobalPosition)));
            }
            else
            {
                Text = "Error: Target object not found.";
            }
        }

        private string _GetDirectionString(byte direction)
        {
            return direction switch
            {
                0 => "North",
				1 => "West",
				2 => "South",
				3 => "East",
				_ => "UNKNOWN"
            };
        }
    }
}
