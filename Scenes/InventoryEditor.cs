using EyeOfRubiss;
using EyeOfRubiss.Nodes;
using EyeOfRubiss.Scenes;
using Godot;
using System;

public partial class InventoryEditor : Control
{
    [Export] private ItemButtonSelector _Hotbar_ItemButtonSelector;
    [Export] private ItemButtonSelector _Bag_ItemButtonSelector_1;
    [Export] private ItemButtonSelector _Bag_ItemButtonSelector_2;
    [Export] private ItemButtonSelector _Bag_ItemButtonSelector_3;
    [Export] private ItemButtonSelector _Bag_ItemButtonSelector_4;
    [Export] private ItemButtonSelector _Bag_ItemButtonSelector_5;
    [Export] private ItemButtonSelector _Bag_ItemButtonSelector_6;
    [Export] private ItemButtonSelector _Bag_ItemButtonSelector_7;

    [Export] private InventoryItemSelector _InventoryItemSelector;

    private bool _Initialized;

    private bool _HotbarSelected = false;
    private int _SelectedItem = -1;

    public void Initialize()
    {
        if (!CommonData.HasInstance())
            return;

        if (_Initialized)
            return;

        for (int i = 0; i < 15; i++)
        {
            InventoryItem item = CommonData.Instance.GetHotbarItem(i);
            _Hotbar_ItemButtonSelector.AddButton(i, item.GetInfo(), item.Count);
        }
        for (int i = 0; i < 60; i++)
        {
            InventoryItem item1 = CommonData.Instance.GetBagItem(i + 60 * 0);
            InventoryItem item2 = CommonData.Instance.GetBagItem(i + 60 * 1);
            InventoryItem item3 = CommonData.Instance.GetBagItem(i + 60 * 2);
            InventoryItem item4 = CommonData.Instance.GetBagItem(i + 60 * 3);
            InventoryItem item5 = CommonData.Instance.GetBagItem(i + 60 * 4);
            InventoryItem item6 = CommonData.Instance.GetBagItem(i + 60 * 5);
            InventoryItem item7 = CommonData.Instance.GetBagItem(i + 60 * 6);
            _Bag_ItemButtonSelector_1.AddButton(i + 60 * 0, item1.GetInfo(), item1.Count);
            _Bag_ItemButtonSelector_2.AddButton(i + 60 * 1, item2.GetInfo(), item2.Count);
            _Bag_ItemButtonSelector_3.AddButton(i + 60 * 2, item3.GetInfo(), item3.Count);
            _Bag_ItemButtonSelector_4.AddButton(i + 60 * 3, item4.GetInfo(), item4.Count);
            _Bag_ItemButtonSelector_5.AddButton(i + 60 * 4, item5.GetInfo(), item5.Count);
            _Bag_ItemButtonSelector_6.AddButton(i + 60 * 5, item6.GetInfo(), item6.Count);
            _Bag_ItemButtonSelector_7.AddButton(i + 60 * 6, item7.GetInfo(), item7.Count);
        }

        _Initialized = true;
    }

    public void _On_Hotbar_ItemSelected(int id)
    {
        if (CommonData.HasInstance())
        {
            InventoryItem item = CommonData.Instance.GetHotbarItem(id);
            GD.Print($"Hotbar item {id}: {item.GetInfo().Name} x {item.Count}");
        }
        else
        {
            GD.Print($"Hotbar item {id} (No CommonData loaded)");
        }
    }
    public void _On_Bag_ItemSelected(int id)
    {
        if (CommonData.HasInstance())
        {
            InventoryItem item = CommonData.Instance.GetBagItem(id);
            GD.Print($"Bag item {id}: {item.GetInfo().Name} x {item.Count}");
        }
        else
        {
            GD.Print($"Bag item {id} (No CommonData loaded)");
        }

        if (_InventoryItemSelector is null)
        {
            return;
        }

        _InventoryItemSelector.PopupAtPosition(GlobalPosition);
    }

    public void _On_ItemSelector_ItemSelected(int id)
    {
        GD.Print(id);
    }

    public void _On_TabBar_TabChanged(int tab)
    {
        _Bag_ItemButtonSelector_1.Hide();
        _Bag_ItemButtonSelector_2.Hide();
        _Bag_ItemButtonSelector_3.Hide();
        _Bag_ItemButtonSelector_4.Hide();
        _Bag_ItemButtonSelector_5.Hide();
        _Bag_ItemButtonSelector_6.Hide();
        _Bag_ItemButtonSelector_7.Hide();

        switch (tab)
        {
            case 0:
                _Bag_ItemButtonSelector_1.Show();
                break;
            case 1:
                _Bag_ItemButtonSelector_2.Show();
                break;
            case 2:
                _Bag_ItemButtonSelector_3.Show();
                break;
            case 3:
                _Bag_ItemButtonSelector_4.Show();
                break;
            case 4:
                _Bag_ItemButtonSelector_5.Show();
                break;
            case 5:
                _Bag_ItemButtonSelector_6.Show();
                break;
            case 6:
                _Bag_ItemButtonSelector_7.Show();
                break;
        }
    }
}