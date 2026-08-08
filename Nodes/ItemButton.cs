using EyeOfRubiss;
using Godot;
using System;

public partial class ItemButton : Button
{
	static readonly Texture2D ColorTexture = ResourceLoader.Load<Texture2D>("res://Resources/Graphics/BlockModifier/color.png");

	static readonly Texture2D ConnectingTexture = ResourceLoader.Load<Texture2D>("res://Resources/Graphics/BlockModifier/connect.png");

	static readonly Texture2D OneStarTexture = ResourceLoader.Load<Texture2D>("res://Resources/Graphics/BlockModifier/1star.png");
	static readonly Texture2D TwoStarTexture = ResourceLoader.Load<Texture2D>("res://Resources/Graphics/BlockModifier/2star.png");
	static readonly Texture2D ThreeStarTexture = ResourceLoader.Load<Texture2D>("res://Resources/Graphics/BlockModifier/3star.png");

	private int _IconIndex = -1;
	[Export] public int IconIndex { get => _IconIndex; set => SetIconIndex(value); }

	private int _Rarity = 0;
	[Export] public int Rarity { get => _Rarity; set => SetRarity(value); }

	private DyeColor _Color = DyeColor.Plain;
	[Export] public DyeColor Color { get => _Color; set => SetColor(value); }

	private bool _Connecting = false;
	[Export] public bool Connecting { get => _Connecting; set => SetConnecting(value); }

	private int _FallbackIconIndex = -1;
	[Export] public int FallbackIconIndex { get => _FallbackIconIndex; set => SetFallbackIconIndex(value); }

    public override void _Draw()
    {
		Texture2D rarityTexture = _Rarity switch
		{
			1 => OneStarTexture,
			2 => TwoStarTexture,
			3 => ThreeStarTexture,
			_ => null
		};

		if (rarityTexture is not null)
		{
			Vector2 textureSize = rarityTexture.GetSize() / 2;
			Vector2 position = new((Size.X / 2) - (textureSize.X / 2), 0);

			DrawTextureRect(
				rarityTexture,
				new Rect2(
					position,
					textureSize
				),
				tile: false
			);
		}

		if (_Color != DyeColor.Plain)
		{
			Color modulate = _Color switch
            {
                DyeColor.White =>  Godot.Color.FromHtml(Constants.Colors.WHITE),
                DyeColor.Black =>  Godot.Color.FromHtml(Constants.Colors.BLACK),
                DyeColor.Purple => Godot.Color.FromHtml(Constants.Colors.PURPLE),
                DyeColor.Pink =>   Godot.Color.FromHtml(Constants.Colors.PINK),
                DyeColor.Red =>    Godot.Color.FromHtml(Constants.Colors.RED),
                DyeColor.Green =>  Godot.Color.FromHtml(Constants.Colors.GREEN),
                DyeColor.Yellow => Godot.Color.FromHtml(Constants.Colors.YELLOW),
                DyeColor.Blue =>   Godot.Color.FromHtml(Constants.Colors.BLUE),
                _ => Colors.White
            };

			Vector2 textureSize = ColorTexture.GetSize() / 2;
			Vector2 position = new(0, Size.Y - textureSize.Y);

			DrawTextureRect(
				ColorTexture,
				new Rect2(
					position,
					textureSize
				),
				tile: false,
				modulate: modulate
			);
		}

		if (_Connecting)
		{
			Vector2 textureSize = ConnectingTexture.GetSize() / 2;
			Vector2 position = new(Size.X - textureSize.X, 0);

			DrawTextureRect(
				ConnectingTexture,
				new Rect2(
					position,
					textureSize
				),
				tile: false
			);
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

	public void SetIconIndex(int icon)
	{
		_IconIndex = icon;
		if (_IconIndex != -1)
			Icon = Util.GetItemIcon(_IconIndex);
		else
		{
			if (_FallbackIconIndex != 1)
				Icon = Util.GetItemIcon(_FallbackIconIndex);
			else
				Icon = null;
		}
	}
	public void SetRarity(int rarity)
	{
		_Rarity = rarity;
	}
	public void SetColor(DyeColor color)
	{
		_Color = color;
	}
	public void SetConnecting(bool connecting)
	{
		_Connecting = connecting;
	}
	public void SetFallbackIconIndex(int icon)
	{
		_FallbackIconIndex = icon;
		if (_IconIndex == -1)
		{
			if (_FallbackIconIndex != -1)
				Icon = Util.GetItemIcon(_FallbackIconIndex);
			else
				Icon = null;
		}
	}

	public void SetItem(EyeOfRubiss.Info.DQB1.ItemInfo item)
	{
		SetIconIndex(item.Icon);
		SetRarity(0);
		SetColor(DyeColor.Plain);
		SetConnecting(false);

		TooltipText = item.GetNameRich();
	}
	public void SetItem(EyeOfRubiss.Info.DQB2.ItemInfo item)
	{
		SetIconIndex(item.Icon);
		SetRarity(item.Rarity);
		//SetColor(item.Color);
		SetConnecting(item.Connecting);

		TooltipText = item.GetNameRich();
	}

	public void Clear()
	{
		IconIndex = -1;
		Rarity = 0;
		Color = DyeColor.Plain;
		Connecting = false;
	}
}
