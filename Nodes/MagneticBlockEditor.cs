using EyeOfRubiss;
using EyeOfRubiss.Info.DQB2;
using Godot;
using System;
using System.Linq;

public partial class MagneticBlockEditor : Window
{
	[Export] private OptionButton _OptionButton;

	private StageData.MagneticBlock _MagneticBlock;

	public void Popup(StageData.MagneticBlock block)
	{
		if (_OptionButton.ItemCount <= 0)
		{
			foreach (BlockInfo blockInfo in BlockInfo.GetAll().OrderBy(b => b.Sort))
			{
				_OptionButton.AddItem(blockInfo.Name, blockInfo.ID);
			}
			foreach (BGPartsInfo bgPartsInfo in BGPartsInfo.GetAll().OrderBy(b => b.Sort))
			{
				_OptionButton.AddItem(bgPartsInfo.Name, bgPartsInfo.ID | 0b1000_0000_0000_0000);
			}
			_OptionButton.SetItemText(0, "None");
		}

		_MagneticBlock = block;
		int id = _MagneticBlock.Camouflage | (_MagneticBlock.BGPartsCamouflaged ? 0b1000_0000_0000_0000 : 0);
		_OptionButton.Selected = _OptionButton.GetItemIndex(id);
		Title = $"Magnetic Block at {block.GetPosition()}";
		PopupCentered();
	}

	public void _On_OptionButton_ItemSelected(int index)
	{
		if (_MagneticBlock is null)
			return;
		
		int id = _OptionButton.GetItemId(index);
		_MagneticBlock.Camouflage = (ushort)(id & 0b0111_1111_1111_1111);
		_MagneticBlock.BGPartsCamouflaged = (id & 0b1000_0000_0000_0000) != 0;
	}

	public void Close()
	{
		Hide();
		_MagneticBlock = null;
		_OptionButton.Selected = -1;
	}
}
