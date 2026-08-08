using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace EyeOfRubiss.Nodes
{
    public partial class StatusLabel : Label
    {
        private AnimationPlayer _AnimationPlayer;
        const string FADEOUT_ANIMATION = "fadeout";

        private static List<StatusLabel> _Instances = [];

        public override void _Ready()
        {
            _Instances.Add(this);
            TreeExited += () => _Instances.Remove(this);

            _OnReadyVariables();
            Text = "";
        }
        private void _OnReadyVariables()
        {
            _AnimationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        }

        public static void PrintMessage(string message, bool console = true)
        {
            if (console)
                GD.Print(message);
            foreach (StatusLabel statusLabel in _Instances)
            {
                statusLabel._PrintMessage(message);
            }
        }
        private void _PrintMessage(string message)
        {
            Text = message;
            _AnimationPlayer?.Stop();
            _AnimationPlayer?.Play(FADEOUT_ANIMATION);
        }
    }
}
