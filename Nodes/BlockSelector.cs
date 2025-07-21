using EyeOfRubiss.Info;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary> TODO </summary>
namespace EyeOfRubiss.Nodes
{
    public partial class BlockSelector : Control
    {
        [Signal] delegate void BlockSelectedEventHandler(ushort block);

        [Export] public int InitialSelectedBlock = -1;

        protected const string ItemButtonScenePath = "res://Nodes/ItemButton.tscn";
    
        private ButtonGroup _ButtonGroup;

        private Dictionary<ushort, ItemButton> _Buttons;

        public override void _Ready()
        {
            _ButtonGroup = new ButtonGroup();

            TryInitializeSelectedBlock();
        }

        private void Pregenerate()
        {
            PackedScene itemButtonScene = ResourceLoader.Load<PackedScene>(ItemButtonScenePath);

            _Buttons = [];

            foreach (BlockInfo blockInfo in BlockInfo.GetAll())
            {
                ItemButton button = itemButtonScene.Instantiate<ItemButton>();
                _Buttons.Add(blockInfo.ID, button);
                button.Ready += () => button.SetBlock(blockInfo);
                button.ButtonGroup = _ButtonGroup;
                button.Pressed += () => _On_Button_Pressed(blockInfo.ID);
            }
        }
        public void Populate()
        {
            Pregenerate();

            SortAndFilter();

            TryInitializeSelectedBlock();
        }

        public void SortAndFilter()
        {
            Sort();
            Filter();
        }
        public void Sort()
        {
            foreach (Node node in GetChildren())
            {
                RemoveChild(node);
            }

            foreach (ushort blockId in _Buttons.Keys.OrderBy(id => BlockInfo.Get(id).SortIndex))
            {
                AddChild(_Buttons[blockId]);
            }
        }
        public void Filter()
        {
            foreach ((ushort id, ItemButton button) in _Buttons)
            {
                BlockInfo blockInfo = BlockInfo.Get(id);

                if (blockInfo.Tags.Contains("noeditor"))
                    button.Hide();
                if (blockInfo.Tags.Contains("liquid"))
                    button.Hide();
            }
        }

        private void TryInitializeSelectedBlock()
        {
            if (InitialSelectedBlock < ushort.MinValue || InitialSelectedBlock > ushort.MaxValue)
                return;

            ushort blockId = (ushort)InitialSelectedBlock;

            if (_Buttons is not null && _Buttons.TryGetValue(blockId, out ItemButton value))
            {
                value.SetPressedNoSignal(true);
            }
        }
    
        public void _On_Button_Pressed(ushort button)
        {
            EmitSignal(SignalName.BlockSelected, button);
        }
    }
}
