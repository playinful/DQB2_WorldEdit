using EyeOfRubiss;
using Godot;
using Microsoft.VisualBasic;
using System;
using System.Diagnostics.Tracing;
using System.Linq;

public partial class ItemDisplayEditor : Window
{
	[Export] private OptionButton _OptionButton;
	[Export] private SpinBox _SpinBox;

	private ParamData.ItemDisplay _ItemDisplayDQB1;
	private StageData.ItemDisplay _ItemDisplayDQB2;

	public void Popup(ParamData.ItemDisplay display)
	{
		_OptionButton.Clear();
		foreach (EyeOfRubiss.Info.DQB1.ItemInfo itemInfo in EyeOfRubiss.Info.DQB1.ItemInfo.GetAll().OrderBy(i => i.Sort))
		{
			_OptionButton.AddItem(itemInfo.Name, itemInfo.ID);
		}

		_ItemDisplayDQB1 = display;
		_ItemDisplayDQB2 = null;
		InventoryItem item = display.Item;
		_OptionButton.Selected = _OptionButton.GetItemIndex(item.ItemID);
		_SpinBox.Value = item.Count;
		Title = $"Item Display at {display.GetPosition()}";
		PopupCentered();
	}
	public void Popup(StageData.ItemDisplay display)
	{
		_OptionButton.Clear();
		foreach (EyeOfRubiss.Info.DQB2.ItemInfo itemInfo in EyeOfRubiss.Info.DQB2.ItemInfo.GetAll().OrderBy(i => i.Sort))
		{
			_OptionButton.AddItem(itemInfo.Name, itemInfo.ID);
		}

		_ItemDisplayDQB1 = null;
		_ItemDisplayDQB2 = display;
		InventoryItem item = display.Item;
		_OptionButton.Selected = _OptionButton.GetItemIndex(item.ItemID);
		_SpinBox.Value = item.Count;
		Title = $"Item Display at {display.GetPosition()}";
		PopupCentered();
	}

	public void _On_OptionButton_ItemSelected(int index)
	{
		if (_ItemDisplayDQB1 is not null)
		{
			_ItemDisplayDQB1.Item.ItemID = (ushort)_OptionButton.GetItemId(index);
		}
		else if (_ItemDisplayDQB2 is not null)
		{
			_ItemDisplayDQB2.Item.ItemID = (ushort)_OptionButton.GetItemId(index);
		}
	}
	public void _On_SpinBox_ValueChanged(double value)
	{
		if (_ItemDisplayDQB1 is not null)
		{
			_ItemDisplayDQB1.Item.Count = (ushort)value;
		}
		else if (_ItemDisplayDQB2 is not null)
		{
			_ItemDisplayDQB2.Item.Count = (ushort)value;
		}
	}

	public void Close()
	{
		_SpinBox.Apply();

		Hide();
		_ItemDisplayDQB1 = null;
		_ItemDisplayDQB2 = null;
		_OptionButton.Selected = -1;
		_SpinBox.Value = 0;
	}
}
