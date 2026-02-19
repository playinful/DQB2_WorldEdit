using EyeOfRubiss;
using EyeOfRubiss.Info;
using Godot;
using System;

public partial class BlockSelectorRetry : Control
{
	public void _On_Blocks_ItemList_ItemSelected(long index)
	{
		GetNode<ItemList>("Props_ItemList").DeselectAll();
	}
	public void _On_Props_ItemList_ItemSelected(long index)
	{
		GetNode<ItemList>("Blocks_ItemList").DeselectAll();
	}

	public void _On_ReloadBlocks_Button_Pressed()
	{
		ItemList blockItemList = GetNode<ItemList>("Blocks_ItemList");

		blockItemList.Clear();
		foreach (EyeOfRubiss.Info.DQB2.BlockInfo blockInfo in EyeOfRubiss.Info.DQB2.BlockInfo.GetAll())
		{
			blockItemList.AddItem("");
			blockItemList.SetItemIcon(-1, Util.GetItemIcon(blockInfo.Icon));
			blockItemList.SetItemTooltip(-1, blockInfo.Name);
			blockItemList.SetItemMetadata(-1, blockInfo.ID);
		}
	}
	public void _On_ReloadProps_Button_Pressed()
	{
		ItemList partsItemList = GetNode<ItemList>("Props_ItemList");

		partsItemList.Clear();
		foreach (EyeOfRubiss.Info.DQB2.BGPartsInfo partsInfo in EyeOfRubiss.Info.DQB2.BGPartsInfo.GetAll())
		{
			partsItemList.AddItem(partsInfo.Name);
		}
	}
}
