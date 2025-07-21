using EyeOfRubiss.Info;
using Godot;
using System;
using System.IO;
using System.Reflection.Metadata;

namespace EyeOfRubiss.Nodes
{
	public partial class ItemButton : Button
	{
		private Label Count_Label { get; set; }
		private TextureRect Item_TextureRect { get; set; }
		private TextureRect Connecting_TextureRect { get; set; }
		private TextureRect Rarity_TextureRect { get; set; }
		private ColorRect Colour_ColorRect { get; set; }

		// public ushort ID { get; set; }

		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			_OnReadyVariables();
		}
		private void _OnReadyVariables()
		{
			Count_Label = GetNode<Label>("CountLabel");
			Item_TextureRect = GetNode<TextureRect>("ItemIcon");
			Rarity_TextureRect = GetNode<TextureRect>("RarityIcon");
			Connecting_TextureRect = GetNode<TextureRect>("ConnectingIcon");
			Colour_ColorRect = GetNode<ColorRect>("ColorRect");
		}

		public void SetItem(ItemInfo itemInfo)
		{
			SetItem(itemInfo.Name, itemInfo.ImageID, itemInfo.Rarity, itemInfo.Connecting, 0);
			//SetItem(Util.ToRichText(itemInfo.Name), itemInfo.ImageID, itemInfo.Rarity, itemInfo.Connecting, 0);
		}
		public void SetBlock(BlockInfo blockInfo)
		{
			SetItem(blockInfo.Name, blockInfo.ImageID, 0, false, 0);
			//SetItem(Util.ToRichText(blockInfo.Name), blockInfo.ImageID, 0, false, 0);
		}

		public void SetItem(string name, int iconIndex, int rarity, bool connecting, int colour)
		{
			Item_TextureRect.Texture = Util.GetItemIcon(iconIndex);

			Connecting_TextureRect.Visible = connecting;

			switch (rarity)
			{
				case 1:
					Rarity_TextureRect.Show();
					Rarity_TextureRect.Texture = ResourceLoader.Load<Texture2D>("res://Graphics/BlockModifier/1star.png");
					break;
				case 2:
					Rarity_TextureRect.Show();
					Rarity_TextureRect.Texture = ResourceLoader.Load<Texture2D>("res://Graphics/BlockModifier/2star.png");
					break;
				case 3:
					Rarity_TextureRect.Show();
					Rarity_TextureRect.Texture = ResourceLoader.Load<Texture2D>("res://Graphics/BlockModifier/3star.png");
					break;
				default:
					Rarity_TextureRect.Hide();
					break;
			}

			switch (colour)
			{
				case 1:
					Colour_ColorRect.Show();
					Colour_ColorRect.Color = new Color(Constants.Colors.WHITE);
					break;
				case 2:
					Colour_ColorRect.Show();
					Colour_ColorRect.Color = new Color(Constants.Colors.BLACK);
					break;
				case 3:
					Colour_ColorRect.Show();
					Colour_ColorRect.Color = new Color(Constants.Colors.PURPLE);
					break;
				case 4:
					Colour_ColorRect.Show();
					Colour_ColorRect.Color = new Color(Constants.Colors.PINK);
					break;
				case 5:
					Colour_ColorRect.Show();
					Colour_ColorRect.Color = new Color(Constants.Colors.RED);
					break;
				case 6:
					Colour_ColorRect.Show();
					Colour_ColorRect.Color = new Color(Constants.Colors.GREEN);
					break;
				case 7:
					Colour_ColorRect.Show();
					Colour_ColorRect.Color = new Color(Constants.Colors.YELLOW);
					break;
				case 8:
					Colour_ColorRect.Show();
					Colour_ColorRect.Color = new Color(Constants.Colors.BLUE);
					break;
				default:
					Colour_ColorRect.Hide();
					break;
			}

			TooltipText = name;
		}
		public void SetItemCount(int count)
		{
			Count_Label.Text = count.ToString();
		}
	}
}
