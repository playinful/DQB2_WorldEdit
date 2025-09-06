using EyeOfRubiss.Info;
using Godot;
using System;
using System.Linq;

namespace EyeOfRubiss.Nodes
{
    public partial class InventoryItemSelector : ItemButtonSelector
    {
        private Control _Parent;

        public override void _Ready()
        {
            base._Ready();
            if (GetParent() is Control parent)
                _Parent = parent;
        }

        public void Populate()
        {
            foreach (ItemInfo item in ItemInfo.GetAll())
            {
                AddButton(item.ID, item);
            }
        }

        public void PopupAtPosition(Vector2 position)
        {
            //this.DisconnectAll(ItemButtonSelector.SignalName.ItemSelected);

            if (_Buttons is null || _Buttons.Count <= 0)
                Populate();

            _Parent.GlobalPosition = position;
            _Parent.Show();
        }
        public void Disengage()
        {
            //this.DisconnectAll(ItemButtonSelector.SignalName.ItemSelected);
            _Parent.Hide();
        }
    }
}