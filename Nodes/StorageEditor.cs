using EyeOfRubiss;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class StorageEditor : Window
{
	private ParamData.Storage _StorageDQB1;
	private StageData.Storage _StorageDQB2;

	public void Popup(ParamData.Storage storage)
	{
		_StorageDQB1 = storage;
		_StorageDQB2 = null;
		Title = $"Storage at {storage.GetPosition()}";
		PopupCentered();

		List<string> items = [];
		foreach (InventoryItem item in storage.GetItems())
		{
			if (item.ItemID != 0)
			{
				items.Add($"{EyeOfRubiss.Info.DQB1.ItemInfo.Get(item.ItemID).Name} x {item.Count}");
			}
		}
		GD.Print(string.Join(", ", items));
	}
	public void Popup(StageData.Storage storage)
	{
		_StorageDQB1 = null;
		_StorageDQB2 = storage;
		Title = $"Storage at {storage.GetPosition()}";
		PopupCentered();

		List<string> items = [];
		foreach (InventoryItem item in storage.GetItems())
		{
			if (item.ItemID != 0)
			{
				items.Add($"{EyeOfRubiss.Info.DQB2.ItemInfo.Get(item.ItemID).Name} x {item.Count}");
			}
		}
		GD.Print(string.Join(", ", items));
	}

	public void Close()
	{
		_StorageDQB1 = null;
		_StorageDQB2 = null;
		Hide();
	}
}
