using EyeOfRubiss.Info;
using Godot;
using System;
using System.Linq;

namespace EyeOfRubiss.Nodes
{
    public partial class InventoryItemSelector : ItemButtonSelector
    {
        [Export] private Control _Panel;

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

            _Panel.GlobalPosition = position;
            _Panel.Show();
        }
        public void Disengage()
        {
            //this.DisconnectAll(ItemButtonSelector.SignalName.ItemSelected);
            _Panel.Hide();
        }
    }
}