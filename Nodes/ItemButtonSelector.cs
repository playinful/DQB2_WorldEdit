using EyeOfRubiss.Info;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary> TODO </summary>
namespace EyeOfRubiss.Nodes
{
    public partial class ItemButtonSelector : Control
    {
        [Signal] public delegate void ItemSelectedEventHandler(int id);

        [Export] public int InitialSelection = -1;

        protected const string ItemButtonScenePath = "res://Nodes/ItemButton.tscn";
    
        protected ButtonGroup _ButtonGroup;

        protected Dictionary<int, ItemButton> _Buttons;

        public override void _Ready()
        {
            _ButtonGroup = new ButtonGroup();
            _Buttons = [];

            if (InitialSelection != -1)
                Select(InitialSelection);
        }

        public ItemButton AddButton(int id, string name = "", int iconIndex = 0, int rarity = 0, bool connecting = false, int colour = 0)
        {
            PackedScene itemButtonScene = ResourceLoader.Load<PackedScene>(ItemButtonScenePath);

            ItemButton button = itemButtonScene.Instantiate<ItemButton>();
            _Buttons.Add(id, button);
            button.Ready += () => button.SetItem(name, iconIndex, rarity, connecting, colour);
            button.ButtonGroup = _ButtonGroup;
            button.Pressed += () => _On_Button_Pressed(id);

            AddChild(button);
            return button;
        }
        public ItemButton AddButton(int id, BlockInfo blockInfo, int? count = null)
        {
            PackedScene itemButtonScene = ResourceLoader.Load<PackedScene>(ItemButtonScenePath);

            ItemButton button = itemButtonScene.Instantiate<ItemButton>();
            _Buttons.Add(id, button);
            button.Ready += () => button.SetBlock(blockInfo, count);
            button.ButtonGroup = _ButtonGroup;
            button.Pressed += () => _On_Button_Pressed(id);

            AddChild(button);
            return button;
        }
        public ItemButton AddButton(int id, ItemInfo itemInfo, int? count = null)
        {
            PackedScene itemButtonScene = ResourceLoader.Load<PackedScene>(ItemButtonScenePath);

            ItemButton button = itemButtonScene.Instantiate<ItemButton>();
            _Buttons.Add(id, button);
            button.Ready += () => button.SetItem(itemInfo, count);
            button.ButtonGroup = _ButtonGroup;
            button.Pressed += () => _On_Button_Pressed(id);

            AddChild(button);
            return button;
        }

        public void Sort(Comparison<int> comparison = null)
        {
            foreach (Node node in GetChildren())
            {
                RemoveChild(node);
            }

            var keys = _Buttons.Keys.ToList();
            if (comparison is null)
                keys.Sort();
            else
                keys.Sort(comparison);
            foreach (int id in keys)
            {
                AddChild(_Buttons[id]);
            }
        }
        public void Filter(Predicate<int> predicate)
        {
            foreach ((int id, ItemButton button) in _Buttons)
            {
                button.Visible = predicate(id);
            }
        }

        public void Select(int buttonId)
        {
            if (_Buttons is not null && _Buttons.TryGetValue(buttonId, out ItemButton value))
            {
                value.SetPressedNoSignal(true);
            }
        }
    
        public void _On_Button_Pressed(int button)
        {
            EmitSignal(SignalName.ItemSelected, button);
        }
    }
}