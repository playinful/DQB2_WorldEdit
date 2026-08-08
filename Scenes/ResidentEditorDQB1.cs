using EyeOfRubiss;
using EyeOfRubiss.Info.DQB1;
using Godot;
using System;

public partial class ResidentEditorDQB1 : Window
{
	[Signal] public delegate void ExportRequestedEventHandler();
	[Signal] public delegate void ImportRequestedEventHandler();

	[Export] ItemList _Resident_ItemList;

	[Export] OptionButton _ResidentID_OptionButton;
	[Export] OptionButton _Type_OptionButton;

	[Export] SpinBox _HP_SpinBox;
	
	[Export] OptionButton _TownNpcState_OptionButton1;
	[Export] OptionButton _TownNpcState_OptionButton2;

	[Export] Button _Export_Button;
	[Export] Button _Import_Button;
	[Export] Button _Clone_Button;
	[Export] Button _Clear_Button;

	private ParamData _ParamData;
	private ParamData.Resident _SelectedResident;

	private bool _IsCloning = false;

    public override void _Ready()
    {
		_ResidentID_OptionButton.Clear();
		_Type_OptionButton.Clear();
		_TownNpcState_OptionButton1.Clear();
		_TownNpcState_OptionButton2.Clear();

        foreach (ResidentInfo resident in ResidentInfo.GetAll())
		{
			_ResidentID_OptionButton.AddItem(resident.Name, resident.ID);
			_Type_OptionButton.AddItem(resident.Name, resident.ID);
		}

		_TownNpcState_OptionButton1.AddItem("NULL");
		_TownNpcState_OptionButton1.AddItem("IDLE");
		_TownNpcState_OptionButton1.AddItem("REFUGEE_BUDDY");
		_TownNpcState_OptionButton1.AddItem("EVENT_BUDDY");
		_TownNpcState_OptionButton1.AddItem("TOWNSMAN_BUDDY");
		_TownNpcState_OptionButton1.AddItem("TOWNSMAN_BUDDY_END");
		_TownNpcState_OptionButton1.AddItem("LEAD");
		_TownNpcState_OptionButton1.AddItem("TOWN_IN");
		_TownNpcState_OptionButton1.AddItem("TOWN_IN_EVENT");
		_TownNpcState_OptionButton1.AddItem("TOWN_IN_CHASE");
		_TownNpcState_OptionButton1.AddItem("TOWN_USUALLY");
		_TownNpcState_OptionButton1.AddItem("TOWN_USUALLY_CHASE");
		_TownNpcState_OptionButton1.AddItem("FALL_SEA");
		_TownNpcState_OptionButton1.AddItem("ZOMBIE");
		_TownNpcState_OptionButton1.AddItem("DEAD");
		_TownNpcState_OptionButton1.AddItem("SLEEP");
		_TownNpcState_OptionButton1.AddItem("WAKE_UP");
		_TownNpcState_OptionButton1.AddItem("SICK");
		_TownNpcState_OptionButton1.AddItem("ZOMBIE_ESCAPE");
		_TownNpcState_OptionButton1.AddItem("BATTLE");
		_TownNpcState_OptionButton1.AddItem("PLAYER_LOST");
		_TownNpcState_OptionButton1.AddItem("REVIVAL");
		_TownNpcState_OptionButton1.AddItem("ESCAPE");
		_TownNpcState_OptionButton1.AddItem("TOWN_USUALLY_THINKING");
		_TownNpcState_OptionButton1.AddItem("TOWN_USUALLY_THINKING_WANDERING");
		_TownNpcState_OptionButton1.AddItem("TOWN_USUALLY_THINKING_TO_CHARACTER_MOVE");
		_TownNpcState_OptionButton1.AddItem("TOWN_USUALLY_THINKING_TO_CHARACTER_MOVE_RECEIVE");
		_TownNpcState_OptionButton1.AddItem("TOWN_USUALLY_THINKING_TO_CHARACTER_ACTION");
		_TownNpcState_OptionButton1.AddItem("TOWN_USUALLY_THINKING_TO_CHARACTER_ACTION_RECEIVE");
		_TownNpcState_OptionButton1.AddItem("TOWN_USUALLY_ROOM_MOVE");
		_TownNpcState_OptionButton1.AddItem("TOWN_USUALLY_ROOM_THINKING");
		_TownNpcState_OptionButton1.AddItem("TOWN_USUALLY_ROOM_THINKING_WANDERING");
		_TownNpcState_OptionButton1.AddItem("TOWN_USUALLY_ROOM_THINKING_TO_CHARACTER_MOVE");
		_TownNpcState_OptionButton1.AddItem("TOWN_USUALLY_ROOM_THINKING_TO_CHARACTER_MOVE_RECEIVE");
		_TownNpcState_OptionButton1.AddItem("TOWN_USUALLY_ROOM_THINKING_TO_CHARACTER_ACTION");
		_TownNpcState_OptionButton1.AddItem("TOWN_USUALLY_ROOM_THINKING_TO_CHARACTER_ACTION_RECEIVE");
		_TownNpcState_OptionButton1.AddItem("TOWN_USUALLY_ROOM_ACTION_MOVE");
		_TownNpcState_OptionButton1.AddItem("TOWN_USUALLY_ROOM_ACTION_EXECUTION");
		_TownNpcState_OptionButton1.AddItem("TOWN_USUALLY_ROOM_ACTION_END");
		_TownNpcState_OptionButton1.AddItem("TOWN_USUALLY_SLEEP_MOVE_TO_ROOM");
		_TownNpcState_OptionButton1.AddItem("TOWN_USUALLY_SLEEP_MOVE_TO_BED");
		_TownNpcState_OptionButton1.AddItem("TOWN_USUALLY_SLEEP_EXECUTION");
		_TownNpcState_OptionButton1.AddItem("TOWN_USUALLY_SLEEP_END");
		_TownNpcState_OptionButton1.AddItem("TOWN_USUALLY_GUARD_WANDERING");
		_TownNpcState_OptionButton1.AddItem("TOWN_USUALLY_WAVE");
		_TownNpcState_OptionButton1.AddItem("TOWN_USUALLY_WAVE_WAIT");
		_TownNpcState_OptionButton1.AddItem("TOWN_USUALLY_WAVE_SEARCHING");
		_TownNpcState_OptionButton1.AddItem("TOWN_USUALLY_WAVE_EVACUATION");
		_TownNpcState_OptionButton1.AddItem("TOWN_USUALLY_RESCUE_BLESSING");
		_TownNpcState_OptionButton1.AddItem("TOWN_USUALLY_NEW_ROOM_BLESSING");
		_TownNpcState_OptionButton1.AddItem("TOWN_USUALLY_WAVE_FLAG_BLESSING");
		_TownNpcState_OptionButton1.AddItem("TOWN_USUALLY_PURIFICATION_BLESSING");
		_TownNpcState_OptionButton1.AddItem("TOWN_USUALLY_BLUEPRINT_BLESSING");
		_TownNpcState_OptionButton1.AddItem("ENDING");
		_TownNpcState_OptionButton1.AddItem("STORY_BUDDY");
		_TownNpcState_OptionButton1.AddItem("FALL");
		_TownNpcState_OptionButton1.AddItem("WAIT");
		_TownNpcState_OptionButton1.AddItem("TOWN_USUALLY_MOVE_ERROR_WANDERING");
		_TownNpcState_OptionButton1.AddItem("WAIT_WAVE");
		_TownNpcState_OptionButton1.AddItem("DETOUR");

		_TownNpcState_OptionButton2.AddItem("NULL");
		_TownNpcState_OptionButton2.AddItem("IDLE");
		_TownNpcState_OptionButton2.AddItem("REFUGEE_BUDDY");
		_TownNpcState_OptionButton2.AddItem("EVENT_BUDDY");
		_TownNpcState_OptionButton2.AddItem("TOWNSMAN_BUDDY");
		_TownNpcState_OptionButton2.AddItem("TOWNSMAN_BUDDY_END");
		_TownNpcState_OptionButton2.AddItem("LEAD");
		_TownNpcState_OptionButton2.AddItem("TOWN_IN");
		_TownNpcState_OptionButton2.AddItem("TOWN_IN_EVENT");
		_TownNpcState_OptionButton2.AddItem("TOWN_IN_CHASE");
		_TownNpcState_OptionButton2.AddItem("TOWN_USUALLY");
		_TownNpcState_OptionButton2.AddItem("TOWN_USUALLY_CHASE");
		_TownNpcState_OptionButton2.AddItem("FALL_SEA");
		_TownNpcState_OptionButton2.AddItem("ZOMBIE");
		_TownNpcState_OptionButton2.AddItem("DEAD");
		_TownNpcState_OptionButton2.AddItem("SLEEP");
		_TownNpcState_OptionButton2.AddItem("WAKE_UP");
		_TownNpcState_OptionButton2.AddItem("SICK");
		_TownNpcState_OptionButton2.AddItem("ZOMBIE_ESCAPE");
		_TownNpcState_OptionButton2.AddItem("BATTLE");
		_TownNpcState_OptionButton2.AddItem("PLAYER_LOST");
		_TownNpcState_OptionButton2.AddItem("REVIVAL");
		_TownNpcState_OptionButton2.AddItem("ESCAPE");
		_TownNpcState_OptionButton2.AddItem("TOWN_USUALLY_THINKING");
		_TownNpcState_OptionButton2.AddItem("TOWN_USUALLY_THINKING_WANDERING");
		_TownNpcState_OptionButton2.AddItem("TOWN_USUALLY_THINKING_TO_CHARACTER_MOVE");
		_TownNpcState_OptionButton2.AddItem("TOWN_USUALLY_THINKING_TO_CHARACTER_MOVE_RECEIVE");
		_TownNpcState_OptionButton2.AddItem("TOWN_USUALLY_THINKING_TO_CHARACTER_ACTION");
		_TownNpcState_OptionButton2.AddItem("TOWN_USUALLY_THINKING_TO_CHARACTER_ACTION_RECEIVE");
		_TownNpcState_OptionButton2.AddItem("TOWN_USUALLY_ROOM_MOVE");
		_TownNpcState_OptionButton2.AddItem("TOWN_USUALLY_ROOM_THINKING");
		_TownNpcState_OptionButton2.AddItem("TOWN_USUALLY_ROOM_THINKING_WANDERING");
		_TownNpcState_OptionButton2.AddItem("TOWN_USUALLY_ROOM_THINKING_TO_CHARACTER_MOVE");
		_TownNpcState_OptionButton2.AddItem("TOWN_USUALLY_ROOM_THINKING_TO_CHARACTER_MOVE_RECEIVE");
		_TownNpcState_OptionButton2.AddItem("TOWN_USUALLY_ROOM_THINKING_TO_CHARACTER_ACTION");
		_TownNpcState_OptionButton2.AddItem("TOWN_USUALLY_ROOM_THINKING_TO_CHARACTER_ACTION_RECEIVE");
		_TownNpcState_OptionButton2.AddItem("TOWN_USUALLY_ROOM_ACTION_MOVE");
		_TownNpcState_OptionButton2.AddItem("TOWN_USUALLY_ROOM_ACTION_EXECUTION");
		_TownNpcState_OptionButton2.AddItem("TOWN_USUALLY_ROOM_ACTION_END");
		_TownNpcState_OptionButton2.AddItem("TOWN_USUALLY_SLEEP_MOVE_TO_ROOM");
		_TownNpcState_OptionButton2.AddItem("TOWN_USUALLY_SLEEP_MOVE_TO_BED");
		_TownNpcState_OptionButton2.AddItem("TOWN_USUALLY_SLEEP_EXECUTION");
		_TownNpcState_OptionButton2.AddItem("TOWN_USUALLY_SLEEP_END");
		_TownNpcState_OptionButton2.AddItem("TOWN_USUALLY_GUARD_WANDERING");
		_TownNpcState_OptionButton2.AddItem("TOWN_USUALLY_WAVE");
		_TownNpcState_OptionButton2.AddItem("TOWN_USUALLY_WAVE_WAIT");
		_TownNpcState_OptionButton2.AddItem("TOWN_USUALLY_WAVE_SEARCHING");
		_TownNpcState_OptionButton2.AddItem("TOWN_USUALLY_WAVE_EVACUATION");
		_TownNpcState_OptionButton2.AddItem("TOWN_USUALLY_RESCUE_BLESSING");
		_TownNpcState_OptionButton2.AddItem("TOWN_USUALLY_NEW_ROOM_BLESSING");
		_TownNpcState_OptionButton2.AddItem("TOWN_USUALLY_WAVE_FLAG_BLESSING");
		_TownNpcState_OptionButton2.AddItem("TOWN_USUALLY_PURIFICATION_BLESSING");
		_TownNpcState_OptionButton2.AddItem("TOWN_USUALLY_BLUEPRINT_BLESSING");
		_TownNpcState_OptionButton2.AddItem("ENDING");
		_TownNpcState_OptionButton2.AddItem("STORY_BUDDY");
		_TownNpcState_OptionButton2.AddItem("FALL");
		_TownNpcState_OptionButton2.AddItem("WAIT");
		_TownNpcState_OptionButton2.AddItem("TOWN_USUALLY_MOVE_ERROR_WANDERING");
		_TownNpcState_OptionButton2.AddItem("WAIT_WAVE");
		_TownNpcState_OptionButton2.AddItem("DETOUR");
    }

	public void Popup(ParamData paramData)
	{
		_ParamData = paramData;

		_PopulateItemList();

		PopupCentered();
	}

	private void _PopulateItemList()
	{
		_Resident_ItemList.Clear();

		if (_ParamData is null)
			return;
		
		foreach (ParamData.Resident resident in _ParamData.GetResidents())
		{
			_Resident_ItemList.AddItem(_GetResidentName(resident));
		}
	}

	private void _UpdateResidentInformation()
	{
		if (_SelectedResident is null)
		{
			_ResidentID_OptionButton.Disabled = true;
			_ResidentID_OptionButton.Selected = -1;
			_Type_OptionButton.Disabled = true;
			_Type_OptionButton.Selected = -1;

			_HP_SpinBox.Editable = false;
			_HP_SpinBox.SetValueNoSignal(0);

			_TownNpcState_OptionButton1.Disabled = true;
			_TownNpcState_OptionButton1.Selected = -1;
			_TownNpcState_OptionButton2.Disabled = true;
			_TownNpcState_OptionButton2.Selected = -1;
		}
		else
		{
			_ResidentID_OptionButton.Disabled = false;
			_ResidentID_OptionButton.Selected = _ResidentID_OptionButton.GetItemIndex(_SelectedResident.ResidentID);
			_Type_OptionButton.Disabled = false;
			_Type_OptionButton.Selected = _Type_OptionButton.GetItemIndex(_SelectedResident.Type);

			_HP_SpinBox.Editable = true;
			_HP_SpinBox.SetValueNoSignal(_SelectedResident.HP);

			_TownNpcState_OptionButton1.Disabled = false;
			_TownNpcState_OptionButton1.Selected = _SelectedResident.State1;
			_TownNpcState_OptionButton2.Disabled = false;
			_TownNpcState_OptionButton2.Selected = _SelectedResident.State2;

			_UpdateResidentName();
		}
	}
	private void _UpdateResidentName()
	{
		if (_SelectedResident is null)
			return;

		_Resident_ItemList.SetItemText(_SelectedResident.Index, _GetResidentName(_SelectedResident));
	}

	private static string _GetResidentName(ParamData.Resident resident)
	{
		ResidentInfo info;
		if (resident.Type != 0)
		{
			info = ResidentInfo.Get(resident.Type);
		}
		else
		{
			info = ResidentInfo.Get(resident.ResidentID);
		}

		return string.IsNullOrEmpty(info.Name) ? "Empty" : info.Name;
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
        fileBytes.AsSpan(0..ParamData.Resident.LENGTH).CopyTo(_SelectedResident.GetBytes());
		_UpdateResidentInformation();
	}

	public void _On_Resident_ItemList_ItemSelected(int index)
	{
		ParamData.Resident newResident = _ParamData.GetResident(index);

		if (_IsCloning)
		{
			_SelectedResident.GetBytes().CopyTo(newResident.GetBytes());
		}

		_IsCloning = false;
		_Clone_Button.ButtonPressed = false;

		_SelectedResident = newResident;
		_UpdateResidentInformation();
	}

	public void _On_ResidentID_OptionButton_ItemSelected(int index)
	{
		_IsCloning = false;
		_Clone_Button.ButtonPressed = false;

		if (_SelectedResident is null)
			return;
		
		_SelectedResident.ResidentID = (ushort)_ResidentID_OptionButton.GetItemId(index);
		_UpdateResidentName();
	}
	public void _On_Type_OptionButton_ItemSelected(int index)
	{
		_IsCloning = false;
		_Clone_Button.ButtonPressed = false;

		if (_SelectedResident is null)
			return;
		
		_SelectedResident.Type = (ushort)_Type_OptionButton.GetItemId(index);
		_UpdateResidentName();
	}
	public void _On_HP_SpinBox_ValueChanged(float value)
	{
		_IsCloning = false;
		_Clone_Button.ButtonPressed = false;

		if (_SelectedResident is null)
			return;
		
		_SelectedResident.HP = (ushort)value;
	}
	public void _On_TownNpcState1_OptionButton_ItemSelected(int index)
	{
		_IsCloning = false;
		_Clone_Button.ButtonPressed = false;

		if (_SelectedResident is null)
			return;
		
		_SelectedResident.State1 = (byte)index;
	}
	public void _On_TownNpcState2_OptionButton_ItemSelected(int index)
	{
		_IsCloning = false;
		_Clone_Button.ButtonPressed = false;

		if (_SelectedResident is null)
			return;
		
		_SelectedResident.State2 = (byte)index;
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
		if (_SelectedResident is null)
			return;
		
		_SelectedResident.Clear();
		_UpdateResidentInformation();
	}

	public void Close()
	{
		_ParamData = null;
		_SelectedResident = null;
		_Resident_ItemList.DeselectAll();

		_UpdateResidentInformation();
		Hide();
	}
}
