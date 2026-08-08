using EyeOfRubiss;
using EyeOfRubiss.Info.DQB2;
using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Metadata;

public partial class ResidentEditorDQB2 : Window
{
	[Signal] public delegate void ExportRequestedEventHandler();
	[Signal] public delegate void ImportRequestedEventHandler();

	private CommonData _CommonData;

	private CommonData.Resident _SelectedResident;

	private byte _TypeFilter = 0b11;
	private byte _IslandFilter = 0;

	[Export] ItemList _ResidentItemList;

	[Export] Label _ResidentIndex_Label;
	[Export] LineEdit _Name_LineEdit;

	[Export] SpinBox _HP_SpinBox;
	[Export] SpinBox _Sex_SpinBox;
	[Export] SpinBox _Type_SpinBox;
	[Export] SpinBox _Job_SpinBox;
	[Export] SpinBox _GenericName_SpinBox;

	[Export] SpinBox _Hair_SpinBox;
	[Export] SpinBox _Face_SpinBox;
	[Export] SpinBox _Body_SpinBox;

	[Export] SpinBox _HairColor_SpinBox;
	[Export] SpinBox _SkinColor_SpinBox;
	[Export] SpinBox _EyeColor_SpinBox;

	[Export] CheckBox _CanEquip_CheckBox;
	[Export] CheckBox _CanBattle_CheckBox;
	[Export] CheckBox _Hidden_CheckBox;
	[Export] CheckBox _LockGraphic_CheckBox;
	[Export] CheckBox _LockType_CheckBox;
	[Export] CheckBox _Dead_CheckBox;
	[Export] CheckBox _Clothed_CheckBox;
	[Export] CheckBox _InRags_CheckBox;

	[Export] OptionButton _HomeIsland_OptionButton;
	[Export] OptionButton _CurrentIsland_OptionButton;
	
	[Export] OptionButton _RoomSize_OptionButton;
	[Export] OptionButton _RoomFanciness_OptionButton;
	[Export] OptionButton _RoomAmbience_OptionButton;

	[Export] ItemButton _Weapon_ItemButton;
	[Export] ItemButton _Armour_ItemButton;

	[Export] Button _Export_Button;
	[Export] Button _Import_Button;
	[Export] Button _Clone_Button;
	[Export] Button _Clear_Button;

	private bool _IsCloning = false;

	public void Popup(CommonData commonData)
	{
		_CommonData = commonData;

		_PopulateItemList();
		_UpdateResidentInformation();

		PopupCentered();
	}

	private void _PopulateItemList()
	{
		_ResidentItemList.Clear();

		if (_CommonData is null)
			return;

		IEnumerable<CommonData.Resident> residents;
		if ((_TypeFilter & 0b01) != 0)
		{
			if ((_TypeFilter & 0b10) != 0)
			{
				residents = _CommonData.GetResidents(1);
			}
			else
			{
				residents = _CommonData.GetImportantResidents(1);
			}
		}
		else
		{
			if ((_TypeFilter & 0b10) != 0)
			{
				residents = _CommonData.GetGenericResidents();
			}
			else
			{
				residents = [];
			}
		}

		foreach (CommonData.Resident resident in residents)
		{
			if (_IslandFilter != 0 && resident.CurrentIsland != _IslandFilter)
				continue;

			string displayName = resident.GetDisplayName();
			if (string.IsNullOrEmpty(displayName))
				displayName = $"[{resident.Index}]";
			_ResidentItemList.AddItem(displayName);
			_ResidentItemList.SetItemMetadata(-1, resident.Index);

			if (_SelectedResident is not null && _SelectedResident.Index == resident.Index)
				_ResidentItemList.Select(-1);
		}
	}

	private void _UpdateResidentInformation()
	{
		if (_SelectedResident is null)
		{
			_ResidentIndex_Label.Text = $"";

			_Name_LineEdit.Text = "";
			_Name_LineEdit.Editable = false;

			_HP_SpinBox.SetValueNoSignal(0);
			_HP_SpinBox.Editable = false;
			_Sex_SpinBox.SetValueNoSignal(0);
			_Sex_SpinBox.Editable = false;
			_Type_SpinBox.SetValueNoSignal(0);
			_Type_SpinBox.Editable = false;
			_Job_SpinBox.SetValueNoSignal(0);
			_Job_SpinBox.Editable = false;
			_GenericName_SpinBox.SetValueNoSignal(0);
			_GenericName_SpinBox.Editable = false;

			_Hair_SpinBox.SetValueNoSignal(0);
			_Hair_SpinBox.Editable = false;
			_Face_SpinBox.SetValueNoSignal(0);
			_Face_SpinBox.Editable = false;
			_Body_SpinBox.SetValueNoSignal(0);
			_Body_SpinBox.Editable = false;

			_HairColor_SpinBox.SetValueNoSignal(0);
			_HairColor_SpinBox.Editable = false;
			_SkinColor_SpinBox.SetValueNoSignal(0);
			_SkinColor_SpinBox.Editable = false;
			_EyeColor_SpinBox.SetValueNoSignal(0);
			_EyeColor_SpinBox.Editable = false;

			_CanEquip_CheckBox.ButtonPressed = false;
			_CanEquip_CheckBox.Disabled = true;
			_CanBattle_CheckBox.ButtonPressed = false;
			_CanBattle_CheckBox.Disabled = true;
			_Hidden_CheckBox.ButtonPressed = false;
			_Hidden_CheckBox.Disabled = true;
			_Dead_CheckBox.ButtonPressed = false;
			_Dead_CheckBox.Disabled = true;
			_Clothed_CheckBox.ButtonPressed = false;
			_Clothed_CheckBox.Disabled = true;
			_InRags_CheckBox.ButtonPressed = false;
			_InRags_CheckBox.Disabled = true;
			_LockGraphic_CheckBox.ButtonPressed = false;
			_LockGraphic_CheckBox.Disabled = true;
			_LockType_CheckBox.ButtonPressed = false;
			_LockType_CheckBox.Disabled = true;

			_HomeIsland_OptionButton.Selected = 0;
			_HomeIsland_OptionButton.Disabled = true;
			_CurrentIsland_OptionButton.Selected = 0;
			_CurrentIsland_OptionButton.Disabled = true;

			_RoomSize_OptionButton.Selected = 0;
			_RoomSize_OptionButton.Disabled = true;
			_RoomFanciness_OptionButton.Selected = 0;
			_RoomFanciness_OptionButton.Disabled = true;
			_RoomAmbience_OptionButton.Selected = 0;
			_RoomAmbience_OptionButton.Disabled = true;

			_Weapon_ItemButton.Clear();
			_Weapon_ItemButton.Disabled = true;
			_Armour_ItemButton.Clear();
			_Armour_ItemButton.Disabled = true;

			_Export_Button.Disabled = true;
			_Import_Button.Disabled = true;
			_Clone_Button.Disabled = true;
			_Clear_Button.Disabled = true;
		}
		else
		{
			_ResidentIndex_Label.Text = $"Resident #{_SelectedResident.Index}";

			_Name_LineEdit.Editable = true;
			_UpdateResidentName();

			_HP_SpinBox.Editable = true;
			_HP_SpinBox.SetValueNoSignal(_SelectedResident.HP);
			_Sex_SpinBox.Editable = true;
			_Sex_SpinBox.SetValueNoSignal(_SelectedResident.Sex);
			_Type_SpinBox.Editable = true;
			_Type_SpinBox.SetValueNoSignal(_SelectedResident.Type);
			_Job_SpinBox.Editable = true;
			_Job_SpinBox.SetValueNoSignal(_SelectedResident.Job);
			_GenericName_SpinBox.Editable = true;
			_GenericName_SpinBox.SetValueNoSignal(_SelectedResident.GenericName);

			_Hair_SpinBox.Editable = !_SelectedResident.LockGraphic;
			_Hair_SpinBox.SetValueNoSignal(_SelectedResident.Hair);
			_Face_SpinBox.Editable = !_SelectedResident.LockGraphic;
			_Face_SpinBox.SetValueNoSignal(_SelectedResident.Face);
			_Body_SpinBox.Editable = !_SelectedResident.LockGraphic;
			_Body_SpinBox.SetValueNoSignal(_SelectedResident.Body);

			_HairColor_SpinBox.Editable = true;
			_HairColor_SpinBox.SetValueNoSignal(_SelectedResident.HairColor);
			_SkinColor_SpinBox.Editable = true;
			_SkinColor_SpinBox.SetValueNoSignal(_SelectedResident.SkinColor);
			_EyeColor_SpinBox.Editable = true;
			_EyeColor_SpinBox.SetValueNoSignal(_SelectedResident.EyeColor);

			_CanEquip_CheckBox.Disabled = false;
			_CanEquip_CheckBox.ButtonPressed = _SelectedResident.CanEquip;
			_CanBattle_CheckBox.Disabled = false;
			_CanBattle_CheckBox.ButtonPressed = _SelectedResident.CanBattle;
			_Hidden_CheckBox.Disabled = false;
			_Hidden_CheckBox.ButtonPressed = _SelectedResident.Hidden;
			_Dead_CheckBox.Disabled = false;
			_Dead_CheckBox.ButtonPressed = _SelectedResident.Dead;
			_Clothed_CheckBox.Disabled = false;
			_Clothed_CheckBox.ButtonPressed = _SelectedResident.Clothed;
			_InRags_CheckBox.Disabled = false;
			_InRags_CheckBox.ButtonPressed = _SelectedResident.InRags;
			_LockGraphic_CheckBox.Disabled = false;
			_LockGraphic_CheckBox.ButtonPressed = _SelectedResident.LockGraphic;
			_LockType_CheckBox.Disabled = false;
			_LockType_CheckBox.ButtonPressed = _SelectedResident.TypeLock;

			_HomeIsland_OptionButton.Disabled = false;
			_HomeIsland_OptionButton.Select(_HomeIsland_OptionButton.GetItemIndex(_SelectedResident.HomeIsland));
			if (_HomeIsland_OptionButton.Selected == -1)
			{
				_HomeIsland_OptionButton.AddItem($"[{_SelectedResident.HomeIsland}]", _SelectedResident.HomeIsland);
				_HomeIsland_OptionButton.Select(_HomeIsland_OptionButton.ItemCount - 1);
			}
			_CurrentIsland_OptionButton.Disabled = false;
			int currentIsland = (_SelectedResident.CurrentRegion << 8) | _SelectedResident.CurrentIsland;
			_CurrentIsland_OptionButton.Select(_CurrentIsland_OptionButton.GetItemIndex(currentIsland));
			if (_CurrentIsland_OptionButton.Selected == -1)
			{
				_CurrentIsland_OptionButton.AddItem($"[{_SelectedResident.CurrentIsland}]", currentIsland);
				_CurrentIsland_OptionButton.Select(_CurrentIsland_OptionButton.ItemCount - 1);
			}

			_RoomSize_OptionButton.Disabled = false;
			_RoomSize_OptionButton     .Select(_SelectedResident.RoomSize      < _RoomSize_OptionButton.ItemCount      ? _SelectedResident.RoomSize      : -1);
			_RoomFanciness_OptionButton.Disabled = false;
			_RoomFanciness_OptionButton.Select(_SelectedResident.RoomFanciness < _RoomFanciness_OptionButton.ItemCount ? _SelectedResident.RoomFanciness : -1);
			_RoomAmbience_OptionButton.Disabled = false;
			_RoomAmbience_OptionButton .Select(_SelectedResident.RoomAmbience  < _RoomAmbience_OptionButton.ItemCount  ? _SelectedResident.RoomAmbience  : -1);

			_Weapon_ItemButton.Disabled = false;
			_Weapon_ItemButton.SetItem(ItemInfo.Get(_SelectedResident.Weapon.ItemID));
			_Armour_ItemButton.Disabled = false;
			_Armour_ItemButton.SetItem(ItemInfo.Get(_SelectedResident.Armour.ItemID));

			_Export_Button.Disabled = false;
			_Import_Button.Disabled = false;
			_Clone_Button.Disabled = false;
			_Clear_Button.Disabled = false;
		}
	}
	private void _UpdateResidentName()
	{
		if (_SelectedResident is null)
			return;

		_Name_LineEdit.Text = _SelectedResident.UseCustomName ? _SelectedResident.Name : string.Empty;
		_Name_LineEdit.PlaceholderText = _SelectedResident.GetInternalName();
		
		int index = -1;
		for (int i = 0; i < _ResidentItemList.ItemCount; i++)
		{
			if (_ResidentItemList.GetItemMetadata(i).As<int>() is int id && id == _SelectedResident.Index)
			{
				index = i;
				break;
			}
		}

		if (index != -1)
		{
			string displayName = _SelectedResident.GetDisplayName();
			_ResidentItemList.SetItemText(index, string.IsNullOrEmpty(displayName) ? $"[{_SelectedResident.Index}]" : displayName);
		}
	}

	public void Export(string path)
	{
		if (_SelectedResident is null)
			return;

        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        file.StoreBuffer(_SelectedResident.GetBytes());
	}
	public void Import(string path)
	{
		if (_SelectedResident is null)
			return;
		
		_SelectedResident.Clear();
        byte[] fileBytes = FileAccess.GetFileAsBytes(path);
        fileBytes.AsSpan(0..CommonData.Resident.LENGTH).CopyTo(_SelectedResident.GetBytes());
		_UpdateResidentInformation();
	}

	public void _On_Resident_ItemList_ItemSelected(int index)
	{
		int residentId = (int)_ResidentItemList.GetItemMetadata(index);
		CommonData.Resident newResident = _CommonData.GetResident(residentId);

		if (_IsCloning)
		{
			_SelectedResident.GetBytes().CopyTo(newResident.GetBytes());
		}

		_IsCloning = false;
		_Clone_Button.ButtonPressed = false;

		_SelectedResident = newResident;
		_UpdateResidentInformation();
	}

	public void _On_TypeFilter_Button_Pressed(byte type)
	{
		_TypeFilter = type;
		_PopulateItemList();
	}
	public void _On_IslandFilter_Button_Pressed(byte islandId)
	{
		_IslandFilter = islandId;
		_PopulateItemList();
	}

	public void _On_Name_LineEdit_EditingToggled(bool toggledOn)
	{
		if (!toggledOn && _SelectedResident is not null)
		{
			_SelectedResident.Name = _Name_LineEdit.Text;
			_SelectedResident.UseCustomName = !string.IsNullOrEmpty(_Name_LineEdit.Text);
			_Name_LineEdit.Text = _SelectedResident.Name;

			_UpdateResidentName();
		}

		_IsCloning = false;
		_Clone_Button.ButtonPressed = false;
	}
	public void _On_HP_SpinBox_ValueChanged(double value)
	{
		if (_SelectedResident is not null)
			_SelectedResident.HP = (short)value;

		_IsCloning = false;
		_Clone_Button.ButtonPressed = false;
	}
	public void _On_Sex_SpinBox_ValueChanged(double value)
	{
		if (_SelectedResident is not null)
		{
			_SelectedResident.Sex = (byte)value;
			_UpdateResidentName();
		}

		_IsCloning = false;
		_Clone_Button.ButtonPressed = false;
	}
	public void _On_Type_SpinBox_ValueChanged(double value)
	{
		if (_SelectedResident is not null)
		{
			_SelectedResident.Type = (ushort)value;
			_UpdateResidentName();
		}

		_IsCloning = false;
		_Clone_Button.ButtonPressed = false;
	}
	public void _On_Job_SpinBox_ValueChanged(double value)
	{
		if (_SelectedResident is not null)
		{
			_SelectedResident.Job = (byte)value;
			_UpdateResidentName();
		}

		_IsCloning = false;
		_Clone_Button.ButtonPressed = false;
	}
	public void _On_GenericName_SpinBox_ValueChanged(double value)
	{
		if (_SelectedResident is not null)
		{
			_SelectedResident.GenericName = (byte)value;
			_UpdateResidentName();
		}

		_IsCloning = false;
		_Clone_Button.ButtonPressed = false;
	}
	public void _On_Hair_SpinBox_ValueChanged(double value)
	{
		if (_SelectedResident is not null)
			_SelectedResident.Hair = (ushort)value;

		_IsCloning = false;
		_Clone_Button.ButtonPressed = false;
	}
	public void _On_Face_SpinBox_ValueChanged(double value)
	{
		if (_SelectedResident is not null)
			_SelectedResident.Face = (ushort)value;

		_IsCloning = false;
		_Clone_Button.ButtonPressed = false;
	}
	public void _On_Body_SpinBox_ValueChanged(double value)
	{
		if (_SelectedResident is not null)
			_SelectedResident.Body = (ushort)value;

		_IsCloning = false;
		_Clone_Button.ButtonPressed = false;
	}
	public void _On_HairColor_SpinBox_ValueChanged(double value)
	{
		if (_SelectedResident is not null)
			_SelectedResident.HairColor = (ushort)value;

		_IsCloning = false;
		_Clone_Button.ButtonPressed = false;
	}
	public void _On_SkinColor_SpinBox_ValueChanged(double value)
	{
		if (_SelectedResident is not null)
			_SelectedResident.SkinColor = (ushort)value;

		_IsCloning = false;
		_Clone_Button.ButtonPressed = false;
	}
	public void _On_EyeColor_SpinBox_ValueChanged(double value)
	{
		if (_SelectedResident is not null)
			_SelectedResident.EyeColor = (ushort)value;

		_IsCloning = false;
		_Clone_Button.ButtonPressed = false;
	}
	public void _On_CanEquip_CheckBox_Toggled(bool toggledOn)
	{
		if (_SelectedResident is not null)
			_SelectedResident.CanEquip = toggledOn;

		_IsCloning = false;
		_Clone_Button.ButtonPressed = false;
	}
	public void _On_CanBattle_CheckBox_Toggled(bool toggledOn)
	{
		if (_SelectedResident is not null)
			_SelectedResident.CanBattle = toggledOn;

		_IsCloning = false;
		_Clone_Button.ButtonPressed = false;
	}
	public void _On_Hidden_CheckBox_Toggled(bool toggledOn)
	{
		if (_SelectedResident is not null)
			_SelectedResident.Hidden = toggledOn;

		_IsCloning = false;
		_Clone_Button.ButtonPressed = false;
	}
	public void _On_Dead_CheckBox_Toggled(bool toggledOn)
	{
		if (_SelectedResident is not null)
		{
			if (toggledOn)
			{
				_SelectedResident.Dead = true;
			}
			else
			{
				_SelectedResident.Resurrect();
			}
		}

		_IsCloning = false;
		_Clone_Button.ButtonPressed = false;
	}
	public void _On_Clothed_CheckBox_Toggled(bool toggledOn)
	{
		if (_SelectedResident is not null)
			_SelectedResident.Clothed = toggledOn;

		_IsCloning = false;
		_Clone_Button.ButtonPressed = false;
	}
	public void _On_InRags_CheckBox_Toggled(bool toggledOn)
	{
		if (_SelectedResident is not null)
			_SelectedResident.InRags = toggledOn;

		_IsCloning = false;
		_Clone_Button.ButtonPressed = false;
	}
	public void _On_LockGraphic_CheckBox_Toggled(bool toggledOn)
	{
		if (_SelectedResident is not null)
			_SelectedResident.LockGraphic = toggledOn;

		_IsCloning = false;
		_Clone_Button.ButtonPressed = false;
	}
	public void _On_LockType_CheckBox_Toggled(bool toggledOn)
	{
		if (_SelectedResident is not null)
			_SelectedResident.TypeLock = toggledOn;

		_IsCloning = false;
		_Clone_Button.ButtonPressed = false;
	}
	public void _On_RoomSize_OptionButton_ItemSelected(int index)
	{
		if (_SelectedResident is not null)
			_SelectedResident.RoomSize = (byte)index;

		_IsCloning = false;
		_Clone_Button.ButtonPressed = false;
	}
	public void _On_RoomFanciness_OptionButton_ItemSelected(int index)
	{
		if (_SelectedResident is not null)
			_SelectedResident.RoomFanciness = (byte)index;

		_IsCloning = false;
		_Clone_Button.ButtonPressed = false;
	}
	public void _On_RoomAmbience_OptionButton_ItemSelected(int index)
	{
		if (_SelectedResident is not null)
			_SelectedResident.RoomAmbience = (byte)index;

		_IsCloning = false;
		_Clone_Button.ButtonPressed = false;
	}
	public void _On_HomeIsland_OptionButton_ItemSelected(int index)
	{
		if (_SelectedResident is not null)
			_SelectedResident.HomeIsland = (byte)_HomeIsland_OptionButton.GetItemId(index);

		_IsCloning = false;
		_Clone_Button.ButtonPressed = false;
	}
	public void _On_CurrentIsland_OptionButton_ItemSelected(int index)
	{
		if (_SelectedResident is not null)
		{
			int id = _CurrentIsland_OptionButton.GetItemId(index);
			byte island = (byte)id;
			byte region = (byte)(id >> 8);

			_SelectedResident.CurrentIsland = island;
			_SelectedResident.CurrentRegion = region;
		}

		_IsCloning = false;
		_Clone_Button.ButtonPressed = false;
	}
	public void _On_Weapon_ItemButton_Pressed()
	{
		_IsCloning = false;
		_Clone_Button.ButtonPressed = false;

		if (_SelectedResident is null)
			return;
		
		InventoryItem weapon = _SelectedResident.Weapon;
		weapon.ItemID++;
		_Weapon_ItemButton.SetItem(ItemInfo.Get(weapon.ItemID));
		// TODO
	}
	public void _On_Armour_ItemButton_Pressed()
	{
		_IsCloning = false;
		_Clone_Button.ButtonPressed = false;

		if (_SelectedResident is null)
			return;
		
		InventoryItem armour = _SelectedResident.Armour;
		armour.ItemID++;
		_Armour_ItemButton.SetItem(ItemInfo.Get(armour.ItemID));
		// TODO
	}

	public void _On_Export_Button_Pressed()
	{
		_IsCloning = false;
		_Clone_Button.ButtonPressed = false;
		
		if (_SelectedResident is null)
			return;
		
		EmitSignal(SignalName.ExportRequested);
	}
	public void _On_Import_Button_Pressed()
	{
		_IsCloning = false;
		_Clone_Button.ButtonPressed = false;
		
		if (_SelectedResident is null)
			return;
		
		EmitSignal(SignalName.ImportRequested);
	}
	public void _On_Clone_Button_Toggled(bool toggledOn)
	{
		_IsCloning = toggledOn;
	}
	public void _On_Clear_Button_Pressed()
	{
		_SelectedResident?.Clear();
		_UpdateResidentInformation();

		_IsCloning = false;
		_Clone_Button.ButtonPressed = false;
	}

	public void Close()
	{
		_SelectedResident = null;
		_CommonData = null;
		_TypeFilter = 0b11;
		_IslandFilter = 0;

		_IsCloning = false;
		_Clone_Button.ButtonPressed = false;

		Hide();
	}
}