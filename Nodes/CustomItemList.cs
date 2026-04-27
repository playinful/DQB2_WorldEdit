using EyeOfRubiss;
using EyeOfRubiss.Info.DQB2;
using Godot;
using System;
using System.Net.Http.Headers;

public partial class CustomItemList : ItemList
{
	static readonly Texture2D ColorTexture = ResourceLoader.Load<Texture2D>("res://Resources/Graphics/BlockModifier/color.png");

	static readonly Texture2D ConnectingTexture = ResourceLoader.Load<Texture2D>("res://Resources/Graphics/BlockModifier/connect.png");

	static readonly Texture2D OneStarTexture = ResourceLoader.Load<Texture2D>("res://Resources/Graphics/BlockModifier/1star.png");
	static readonly Texture2D TwoStarTexture = ResourceLoader.Load<Texture2D>("res://Resources/Graphics/BlockModifier/2star.png");
	static readonly Texture2D ThreeStarTexture = ResourceLoader.Load<Texture2D>("res://Resources/Graphics/BlockModifier/3star.png");

    public override void _Draw()
    {
		DrawSetTransform(new Vector2((float)-GetHScrollBar().Value, (float)-GetVScrollBar().Value));

		for (int i = 0; i < ItemCount; i++)
		{
			if (GetItemMetadata(i).AsGodotObject() is not ItemMetadata metadata)
			{
				continue;
			}

			switch (metadata.Rarity)
			{
				case 1:
					DrawStarTexture(i, OneStarTexture);
					break;
				case 2:
					DrawStarTexture(i, TwoStarTexture);
					break;
				case 3:
					DrawStarTexture(i, ThreeStarTexture);
					break;
			}
			
			if (metadata.Connecting)
				DrawConnectingTexture(i);
			
			switch (metadata.Color)
			{
				case DyeColor.White:
					DrawColorTexture(i, Color.FromHtml(Constants.Colors.WHITE));
					break;
				case DyeColor.Black:
					DrawColorTexture(i, Color.FromHtml(Constants.Colors.BLACK));
					break;
				case DyeColor.Purple:
					DrawColorTexture(i, Color.FromHtml(Constants.Colors.PURPLE));
					break;
				case DyeColor.Pink:
					DrawColorTexture(i, Color.FromHtml(Constants.Colors.PINK));
					break;
				case DyeColor.Red:
					DrawColorTexture(i, Color.FromHtml(Constants.Colors.RED));
					break;
				case DyeColor.Green:
					DrawColorTexture(i, Color.FromHtml(Constants.Colors.GREEN));
					break;
				case DyeColor.Yellow:
					DrawColorTexture(i, Color.FromHtml(Constants.Colors.YELLOW));
					break;
				case DyeColor.Blue:
					DrawColorTexture(i, Color.FromHtml(Constants.Colors.BLUE));
					break;
			}
		}
    }

    public override void _Ready()
    {
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton && mouseButton.ButtonMask == MouseButtonMask.Right)
		{
			AcceptEvent();
		}
    }
    public override GodotObject _MakeCustomTooltip(string forText)
    {
		if (string.IsNullOrEmpty(forText))
			return null;

        RichTextLabel label = new()
        {
            Text = Util.ToRichText(forText),
			FitContent = true,
			ScrollActive = false,
			AutowrapMode = TextServer.AutowrapMode.Off,
			BbcodeEnabled = true,
			VerticalAlignment = VerticalAlignment.Center
        };
        return label;
    }

	public void AddCustomItem(int id, string name, Texture2D icon, byte rarity = 0, bool connecting = false, DyeColor color = DyeColor.Plain)
	{
		AddIconItem(icon);
		SetItemTooltip(-1, name);
		SetItemMetadata(-1, new ItemMetadata
		{
			ID = id,
			Rarity = rarity,
			Connecting = connecting,
			Color = color
		});
	}

	public void DrawStarTexture(int index, Texture2D texture)
	{
		Vector2 textureSizeModified = texture.GetSize() / 2;
		Rect2 itemRect = GetItemRect(index, expand: false);

		DrawTextureRect(
			texture, 
			new Rect2(
				new Vector2(
					itemRect.Position.X + (itemRect.Size.X / 2) - (textureSizeModified.X / 2),
					itemRect.Position.Y
				),
				textureSizeModified
			),
			tile: false
		);
	}
	public void DrawConnectingTexture(int index)
	{
		Rect2 itemRect = GetItemRect(index, expand: false);
		Vector2 textureSizeModified = ConnectingTexture.GetSize() / 2;

		DrawTextureRect(
			ConnectingTexture,
			new Rect2(
				new Vector2(
					itemRect.Position.X + itemRect.Size.X - textureSizeModified.X,
					itemRect.Position.Y
				),
				textureSizeModified
			),
			tile: false);
	}
	public void DrawColorTexture(int index, Color color)
	{
		Rect2 itemRect = GetItemRect(index, expand: false);
		Vector2 textureSizeModified = ColorTexture.GetSize() / 2;

		DrawTextureRect(
			ColorTexture,
			new Rect2(
				new Vector2(
					itemRect.Position.X,
					itemRect.Position.Y + itemRect.Size.Y - textureSizeModified.Y
				),
				textureSizeModified
			),
			tile: false,
			modulate: color
		);
	}
	public void DrawCount(int index, int count)
	{
		Rect2 itemRect = GetItemRect(index, expand: false);

		DrawString(ThemeDB.FallbackFont, itemRect.Position + itemRect.Size * Vector2.Down, count.ToString(), HorizontalAlignment.Right, width: itemRect.Size.X);
	}

	public int GetItemID(int index)
	{
		if (GetItemMetadata(index).AsGodotObject() is ItemMetadata metadata)
			return metadata.ID;
		else return -1;
	}

	public partial class ItemMetadata : GodotObject
	{
		public int ID;
		public byte Rarity;
		public bool Connecting;
		public DyeColor Color;
	}
}
