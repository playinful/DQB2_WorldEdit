using EyeOfRubiss;
using EyeOfRubiss.Nodes;
using EyeOfRubiss.Scenes;
using Godot;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class InventoryEditor : Window
{
    private ParamData _ParamData;
    private CommonData _CommonData;
    
    public void Popup(ParamData paramData)
    {
        _ParamData = paramData;
        _CommonData = null;

        FlowContainer container = new()
        {
            Size = Size
        };
        AddChild(container);
        foreach (InventoryItem item in _ParamData.GetHotbarItems())
        {
            if (item.ItemID != 0)
            {
                EyeOfRubiss.Info.DQB1.ItemInfo itemInfo = EyeOfRubiss.Info.DQB1.ItemInfo.Get(item.ItemID);
                AtlasTexture texture = itemInfo.GetIcon();
                Button button = new Button();
                button.Icon = texture;
                container.AddChild(button);   
            }
        }
        foreach (InventoryItem item in _ParamData.GetBagItems())
        {
            if (item.ItemID != 0)
            {
                EyeOfRubiss.Info.DQB1.ItemInfo itemInfo = EyeOfRubiss.Info.DQB1.ItemInfo.Get(item.ItemID);
                AtlasTexture texture = itemInfo.GetIcon();
                Button button = new Button();
                button.Icon = texture;
                container.AddChild(button);   
            }
        }

        PopupCentered();
    }
    public void Popup(CommonData commonData)
    {
        _ParamData = null;
        _CommonData = commonData;

        List<string> hotbarStrings = [];
        foreach (InventoryItem item in _CommonData.GetHotbarItems())
        {
            if (item.ItemID != 0)
            {
                hotbarStrings.Add($"{EyeOfRubiss.Info.DQB2.ItemInfo.Get(item.ItemID).GetNameRich()} x{item.Count}");
            }
        }
        
        List<string> bagStrings = [];
        foreach (InventoryItem item in _CommonData.GetBagItems())
        {
            if (item.ItemID != 0)
            {
                bagStrings.Add($"{EyeOfRubiss.Info.DQB2.ItemInfo.Get(item.ItemID).GetNameRich()} x{item.Count}");
            }
        }
        
        AddChild(new Label()
        {
            Text =
                "Hotbar Items: " + string.Join(", ", hotbarStrings) +
                "\n\nBag Items: " + string.Join(", ", bagStrings)
        });

        PopupCentered();
    }

    public void Close()
    {
        _ParamData = null;
        _CommonData = null;

        foreach (Node child in GetChildren())
        {
            child.QueueFree();
        }

        Hide();
    }
}