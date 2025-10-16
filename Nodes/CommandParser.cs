using EyeOfRubiss.Info;
using EyeOfRubiss.Scenes;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EyeOfRubiss.Nodes
{
    public partial class CommandParser : LineEdit
    {
        [Export] private WorldEditorScene _WorldEditorScene;
        [Export] private StatusLabel _StatusLabel;

        public override void _Input(InputEvent @event)
        {
			if (@event is InputEventKey inputEventKey && inputEventKey.Keycode == Key.Slash)
			{
				//lineEdit.Text = "/";
				Show();
				GrabFocus();
				//lineEdit.CaretColumn = 1;
			}
        }

        public void _On_TextSubmitted(string new_text)
        {
            Text = "";
            if (!new_text.StartsWith('/'))
            {
                _StatusLabel?.PrintMessage(new_text, console: false);
                return;
            }

            string[] command_parts = new_text[1..].Split(' ');
            try
            {
                switch (command_parts[0])
                {
                    case "tp":
                        _WorldEditorScene.MoveCamera(new Vector3(command_parts[1].ToFloat(), command_parts[2].ToFloat(), command_parts[3].ToFloat()) - new Vector3(1024, 0, 1024));
                        break;
                    case "reload":
                        _WorldEditorScene.Reload();
                        break;

                    case "setblock":
                        _WorldEditorScene.SetBlock(new Vector3I(command_parts[1].ToInt(), command_parts[2].ToInt(), command_parts[3].ToInt()), (ushort)command_parts[4].ToInt());
                        break;
                    case "fill":
                        _WorldEditorScene.FillCube(new Vector3I(command_parts[1].ToInt(), command_parts[2].ToInt(), command_parts[3].ToInt()),
                            new Vector3I(command_parts[4].ToInt(), command_parts[5].ToInt(), command_parts[6].ToInt()), (ushort)command_parts[7].ToInt());
                        break;
                    case "clone":
                        _WorldEditorScene.CopyPaste(new Vector3I(command_parts[1].ToInt(), command_parts[2].ToInt(), command_parts[3].ToInt()),
                            new Vector3I(command_parts[4].ToInt(), command_parts[5].ToInt(), command_parts[6].ToInt()),
                            new Vector3I(command_parts[7].ToInt(), command_parts[8].ToInt(), command_parts[9].ToInt()));
                        break;
                    case "setbrushblock":
                        _WorldEditorScene.SetBrushBlock((ushort)command_parts[1].ToInt());
                        break;
                    case "setbrushprop":
                        _WorldEditorScene.SetBrushProp((ushort)command_parts[1].ToInt());
                        break;

                    case "countblocks":
                        _WorldEditorScene.CountBlocks(command_parts[1]);
                        break;
                    case "countprops":
                        _WorldEditorScene.CountProps(command_parts[1]);
                        break;
                    case "findprop":
                        _WorldEditorScene.FindProp((ushort)command_parts[1].ToInt());
                        break;

                    case "superflat":
                        string[] superflat_args = command_parts[1].Split(",");
                        List<ushort> layers = [];
                        foreach (string arg in superflat_args)
                        {
                           int count;
                            ushort blockId;
                            if (arg.Contains('*'))
                            {
                                var split = arg.Split("*", 2);
                                blockId = (ushort)split[0].ToInt();
                                count = split[1].ToInt();
                            }
                            else
                            {
                                count = 1;
                                blockId = (ushort)arg.ToInt();
                            }

                            for (int i = 0; i < count; i++)
                                layers.Add(blockId);
                        }
                        _WorldEditorScene.MakeSuperflat(layers);
                        break;

                    case "sethotbaritem":
                        InventoryItem hotbar_item = CommonData.Instance.GetHotbarItem(command_parts[1].ToInt());
                        hotbar_item.ItemID = (ushort)command_parts[2].ToInt();
                        if (command_parts.Length > 2)
                            hotbar_item.Count = (short)command_parts[3].ToInt();
                        else if (hotbar_item.Count == 0)
                            hotbar_item.Count = 1;
                        break; 
                    case "setbagitem":
                        InventoryItem bag_item = CommonData.Instance.GetBagItem(command_parts[1].ToInt());
                        bag_item.ItemID = (ushort)command_parts[2].ToInt();
                        if (command_parts.Length > 2)
                            bag_item.Count = (short)command_parts[3].ToInt();
                        else if (bag_item.Count == 0)
                            bag_item.Count = 1;
                        break;
                    case "clearbag":
                        foreach (InventoryItem item in CommonData.Instance.GetBagItems())
                        {
                            item.Count = 0;
                            item.ItemID = 0;
                        }
                        break;
                    case "dyetest":
                        int q = 0;
                        foreach (ItemInfo item in ItemInfo.GetAll())
                        {
                            if (item.Name.Contains("White") || item.Name.Contains("Black") || item.Name.Contains("Purple") || item.Name.Contains("Pink") || item.Name.Contains("Red") || item.Name.Contains("Green") || item.Name.Contains("Yellow") || item.Name.Contains("Blue"))
                            {
                                InventoryItem inventoryItem = CommonData.Instance.GetBagItem(q);
                                inventoryItem.Count = 1;
                                inventoryItem.ItemID = item.ID;
                                q++;
                                if (q >= 420)
                                    break;
                            }
                        }
                        break;
                    case "testworkbench":
                        foreach (InventoryItem item in CommonData.Instance.GetBagItems())
                        {
                            item.Count = 0;
                            item.ItemID = 0;
                        }
                        int r = 0;
                        foreach (ItemInfo item in ItemInfo.GetAll())
                        {
                            if (item.Name.Contains("Workbench"))
                            {
                                InventoryItem inventoryItem = CommonData.Instance.GetBagItem(r);
                                inventoryItem.Count = 1;
                                inventoryItem.ItemID = item.ID;
                                r++;
                                if (r >= 420)
                                    break;
                            }
                        }
                        break;

                    case "propdata":
                        _WorldEditorScene.TEST_PropData();
                        break;

                    default:
                        _StatusLabel.PrintMessage($"Unknown command: {new_text}");
                        break;
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr(ex);
            }

            Hide();
        }
    }
}
