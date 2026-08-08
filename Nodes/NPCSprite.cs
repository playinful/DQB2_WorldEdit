using Godot;
using System;

namespace EyeOfRubiss.Nodes
{
    public partial class NPCSprite : Node3D
    {
        [Export] private int _Icon;
        [Export] private string _Name;

        public int ResidentID { get; set; } = -1;

        private Sprite3D _Sprite;
        private Label3D _Label;

        public override void _Ready()
        {
            _OnReadyVariables();
            UpdateNPC();
        }
        private void _OnReadyVariables()
        {
            _Sprite = GetNode<Sprite3D>("Character/Sprite3D");
            _Label = GetNode<Label3D>("Character/Label3D");
        }

        public void SetNPC(CommonData.Resident resident)
        {
            ResidentID = resident.Index;
            _Name = resident.GetDisplayName();
            _Icon = resident.Type;
            UpdateNPC();
        }
        public void SetNPC(ParamData.Resident resident)
        {
            ResidentID = resident.Index;
            if (resident.Type == 0)
            {
                _Name = Info.DQB1.ResidentInfo.Get(resident.ResidentID).Name;
            }
            else
            {
                _Name = Info.DQB1.ResidentInfo.Get(resident.Type).Name;
            }
            _Icon = 0;
            UpdateNPC();
        }
        public void SetNPCIcon(int icon)
        {
            _Icon = icon;
            UpdateNPC();
        }
        public void SetNPCName(string name)
        {
            _Name = name;
            UpdateNPC();
        }
        private void UpdateNPC()
        {
            if (_Sprite is not null)
            {
                if (_Icon >= 1)
                {
                    _Sprite.Frame = _Icon - 1;
                    _Sprite.Show();
                }
                else
                    _Sprite.Hide();
            }
            if (_Label is not null)
            {
                _Label.Text = _Name;
            }
        }
    }
}
