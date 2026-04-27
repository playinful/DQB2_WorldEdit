using EyeOfRubiss;
using Godot;
using System;
using System.ComponentModel;
using System.IO;

public partial class ScreenshotEditor : Window
{
	private ScreenshotData _ScreenshotData;

	[Signal] public delegate void ExportRequestedEventHandler();
	[Signal] public delegate void ImportRequestedEventHandler();
	[Signal] public delegate void ExportAllRequestedEventHandler();

	[Export] private ItemList _ItemList;
	[Export] private TextureRect _Preview_TextureRect;

	private Image[] _ImageCache = new Image[100];

	private int _ImageCount = 0;
	private int _SelectedImage = -1;
	private int _AddImageIndex = -1;
	private bool _AddingNewImage = false;

    public override void _Ready()
    {
        CloseRequested += Hide;
    }

	public void LoadScreenshotData(ScreenshotData screenshotData)
	{
		if (screenshotData != _ScreenshotData)
		{
			_ScreenshotData = screenshotData;

			CallDeferred(MethodName.Populate);
		}
	}
	private void Populate()
	{
		for (int i = 0; i < _ImageCache.Length; i++)
		{
			_ImageCache[i] = null;
		}

		_ItemList.Clear();
		_ItemList.FixedColumnWidth = (int)_ItemList.Size.X / _ItemList.MaxColumns - 4;
		_ItemList.FixedIconSize = new Vector2I(_ItemList.FixedColumnWidth, (int)(_ItemList.FixedColumnWidth * 0.5625));

		for (int i = 0; i < _ImageCache.Length; i++)
		{
			Image image = _ScreenshotData.GetImage(i);
			if (image is null || image.IsEmpty())
			{
				for (int j = i; j < _ImageCache.Length; j++)
					_ImageCache[j] = null;
				_ImageCount = i;
				break;
			}

			_ImageCache[i] = image;

			ImageTexture imagetex = ImageTexture.CreateFromImage(image);

            _ItemList.AddIconItem(imagetex);
        }

		if (false)//(_ItemList.ItemCount < 100)
		{
			_AddImageIndex = _ItemList.ItemCount;
			_ItemList.AddIconItem(ResourceLoader.Load<Texture2D>("res://Resources/Graphics/add_image.png"));
		}
		else
		{
			_AddImageIndex = -1;
		}

		if (_ItemList.ItemCount > 1)
		{
			_ItemList.Select(0);
			_On_ItemList_ItemSelected(0);
		}
	}

	public void Export(string path)
	{
		using Godot.FileAccess file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write);
		file.StoreBuffer(_ImageCache[_SelectedImage].SaveJpgToBuffer());
	}
	public void ExportAll(string path)
	{
		for (int i = 0; i < _ImageCount; i++)
		{
			Image image = _ImageCache[i];
			
			using Godot.FileAccess file = Godot.FileAccess.Open(Path.Join(path, $"screenshot_{i:000}.jpg"), Godot.FileAccess.ModeFlags.Write);
			file.StoreBuffer(image.SaveJpgToBuffer());
		}
	}
	public void Import(string path)
	{
		if (_AddingNewImage)
		{
			_AddingNewImage = false;

			if (_ImageCount >= 100)
				return;

			if (_ScreenshotData.TrySetImage(_ImageCount, path))
			{
				Image image = _ScreenshotData.GetImage(_ImageCount);

				_ImageCache[_ImageCount] = image;

				ImageTexture imageTexture = ImageTexture.CreateFromImage(image);

				_ItemList.SetItemIcon(_ImageCount, imageTexture);
				_Preview_TextureRect.Texture = imageTexture;

				_ImageCount++;

				if (_ImageCount < 100)
				{
					_AddImageIndex = _ImageCount;
					_ItemList.AddIconItem(ResourceLoader.Load<Texture2D>("res://Resources/Graphics/add_image.png"));
				}
				else
				{
					_AddImageIndex = -1;
				}
			}
		}
		else
		{
			if (_ScreenshotData.TrySetImage(_SelectedImage, path))
			{
				Image image = _ScreenshotData.GetImage(_SelectedImage);

				_ImageCache[_SelectedImage] = image;

				ImageTexture imageTexture = ImageTexture.CreateFromImage(image);

				_ItemList.SetItemIcon(_SelectedImage, imageTexture);
				_Preview_TextureRect.Texture = imageTexture;
			}
		}
	}

	public void _On_ItemList_ItemSelected(int index)
	{
		if (index == _AddImageIndex)
		{
			_AddingNewImage = true;
			EmitSignal(SignalName.ImportRequested);
		}
		else
		{
			_AddingNewImage = false;
			_SelectedImage = index;
			_Preview_TextureRect.Texture = ImageTexture.CreateFromImage(_ImageCache[index]);
		}
	}

	public void _On_Export_Button_Pressed()
	{
		_AddingNewImage = false;
		EmitSignal(SignalName.ExportRequested);
	}
	public void _On_Import_Button_Pressed()
	{
		_AddingNewImage = false;
		EmitSignal(SignalName.ImportRequested);
	}
	public void _On_ExportAll_Button_Pressed()
	{
		_AddingNewImage = false;
		EmitSignal(SignalName.ExportAllRequested);
	}
}
