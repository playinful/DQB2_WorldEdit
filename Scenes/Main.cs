using Godot;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using EyeOfRubiss.Nodes;
using EyeOfRubiss.Info;
using System.Net;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Collections;
using System.ComponentModel;
using System.Reflection.Metadata;
using Godot.NativeInterop;

namespace EyeOfRubiss.Scenes
{
	/// <summary> The project's main scene. </summary>
	public partial class Main : Control
	{
		/// References to scene elements
		#region Scene elements
		[ExportGroup("Scene Elements")]
		[Export] private WorldEditorScene _WorldEditorScene;
		[Export] private FileDialog _FileDialog;
		[Export] private Window _UnsavedChanges_Window;
		[Export] private Label _UnsavedChanges_Label;

		[Export] private PopupMenu _File_PopupMenu;
		[Export] private PopupMenu _Settings_PopupMenu;
		[Export] private Button _Inventory_Button;
		[Export] private Button _Player_Button;
		[Export] private Button _Snapshots_Button;

		[Export] private OptionButton _IslandSelector_Button;
		[Export] private SpinBox _Gratitude_SpinBox;
		[Export] private TimeSpinBox _Time_SpinBox;
		[Export] private OptionButton _Weather_OptionButton;
		[Export] private CheckBox _PassTime_CheckBox;

		[Export] private TabContainer _BlockSelector_TabContainer;
		[Export] private CustomItemList _Block_ItemList;
		[Export] private CustomItemList _BGParts_ItemList;
		[Export] private CustomItemList _Fluid_ItemList;
		[Export] private LineEdit _Block_SearchBox;
		[Export] private LineEdit _BGParts_SearchBox;

		[Export] private Window _New_Window;
		[Export] private OptionButton _New_Window_Game_OptionButton;
		[Export] private SpinBox _New_Window_X_SpinBox;
		[Export] private SpinBox _New_Window_Z_SpinBox;

		[Export] private InventoryEditor _InventoryEditor;
		[Export] private ResidentEditorDQB1 _ResidentEditorDQB1;
		[Export] private ResidentEditorDQB2 _ResidentEditorDQB2;
		[Export] private ScreenshotEditor _ScreenshotEditor;
		#endregion

		private static Main _Instance;

		public override void _Ready()
		{
			GD.Randomize();

			if (_Instance is null)
				_Instance = this;

			if (!OS.IsDebugBuild())
			{
				foreach (string arg in OS.GetCmdlineArgs())
				{
					if (Godot.FileAccess.FileExists(arg))
					{
						OpenFile(arg);
					}
				}	
			}

			GetTree().Root.FilesDropped += _On_Root_FilesDropped;

			GetTree().AutoAcceptQuit = false;
			GetTree().Root.CloseRequested += _On_Root_CloseRequested;

			_InitializeFileDialogPath();
			UpdateLoadedData();
		}
		private void _InitializeFileDialogPath()
		{
			// TODO add support for saving this as a preference
			var path = Path.Join(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "My Games", "DRAGON QUEST BUILDERS II", "Steam");
			if (Directory.GetDirectories(path).Length == 1)
			{
				path = Path.Join(Directory.GetDirectories(path)[0], "SD");
			}
			_FileDialog.SetCurrentDirRecursive(path);
		}
		
		private byte _ItemListMode = 0;
		private void _InitializeItemLists_DQB1()
		{
			PopulateBlockList();
			PopulateBGPartsList();

			_BlockSelector_TabContainer.CurrentTab = 0;
			_BlockSelector_TabContainer.SetTabHidden(2, true);

			_Block_ItemList.Select(1);

			_Block_SearchBox.Clear();
			_BGParts_SearchBox.Clear();

			_ItemListMode = 1;
		}
		private void _InitializeItemLists_DQB2()
		{
			PopulateBlockList();
			PopulateBGPartsList();
			PopulateFluidsList();

			_BlockSelector_TabContainer.CurrentTab = 0;
			_BlockSelector_TabContainer.SetTabHidden(2, false);

			_Block_ItemList.Select(1);

			_Block_SearchBox.Clear();
			_BGParts_SearchBox.Clear();

			_ItemListMode = 2;
		}

		public void PopulateBlockList(string searchText = null)
		{
			_Block_ItemList.Clear();

			if (_WorldData is not null || _ParamData is not null || _DioramaDataAssetDQB1 is not null || _DioramaHeaderAssetDQB1 is not null || (_EyeOfRubissStructure is not null && _EyeOfRubissStructure.SourceGame == 1))
			{
				foreach (Info.DQB1.BlockInfo blockInfo in Info.DQB1.BlockInfo.SearchByText(searchText).OrderBy(b => b.Sort))
				{
					_Block_ItemList.AddCustomItem(blockInfo.ID, blockInfo.Name, blockInfo.GetIcon());
				}
			}
			else if (_CommonData is not null || _StageData is not null || _ScreenshotData is not null || (_EyeOfRubissStructure is not null && _EyeOfRubissStructure.SourceGame == 2))
			{
				foreach (Info.DQB2.BlockInfo blockInfo in Info.DQB2.BlockInfo.SearchByText(searchText).Where(b => !b.Tags.Contains("noeditor") && b.FluidType == FluidType.Air && b.ID < 1158).OrderBy(b => b.Sort))
				{
					_Block_ItemList.AddCustomItem(blockInfo.ID, blockInfo.Name, blockInfo.GetIcon(), rarity: blockInfo.Rarity, color: blockInfo.Color);
				}
			}
		}
		public void PopulateBGPartsList(string searchText = null)
		{
			_BGParts_ItemList.Clear();

			if (_WorldData is not null || _ParamData is not null || _DioramaDataAssetDQB1 is not null || _DioramaHeaderAssetDQB1 is not null || (_EyeOfRubissStructure is not null && _EyeOfRubissStructure.SourceGame == 1))
			{
				foreach (Info.DQB1.BGPartsInfo partsInfo in Info.DQB1.BGPartsInfo.SearchByText(searchText).OrderBy(b => b.Sort))
				{
					_BGParts_ItemList.AddCustomItem(partsInfo.ID, partsInfo.Name, Util.GetItemIcon(partsInfo.Icon));
				}
			}
			else if (_CommonData is not null || _StageData is not null || _ScreenshotData is not null || (_EyeOfRubissStructure is not null && _EyeOfRubissStructure.SourceGame == 2))
			{
				foreach (Info.DQB2.BGPartsInfo partsInfo in Info.DQB2.BGPartsInfo.SearchByText(searchText).OrderBy(b => b.Sort))
				{
					_BGParts_ItemList.AddCustomItem(partsInfo.ID, partsInfo.Name, Util.GetItemIcon(partsInfo.Icon), rarity: partsInfo.Rarity, connecting: partsInfo.Connecting, color: partsInfo.Color);
				}	
			}
		}
		public void PopulateFluidsList()
		{
			_Fluid_ItemList.Clear();

			_Fluid_ItemList.AddCustomItem((int)FluidType.Water,      "Water",       Util.GetItemIcon(73));
			_Fluid_ItemList.AddCustomItem((int)FluidType.Seawater,   "Seawater",    Util.GetItemIcon(2131));
			_Fluid_ItemList.AddCustomItem((int)FluidType.HotWater,   "Hot Water",   Util.GetItemIcon(798));
			_Fluid_ItemList.AddCustomItem((int)FluidType.MuddyWater, "Muddy Water", Util.GetItemIcon(2130));
			_Fluid_ItemList.AddCustomItem((int)FluidType.SwampWater, "Swamp Water", Util.GetItemIcon(2130));
			_Fluid_ItemList.AddCustomItem((int)FluidType.Poison,     "Poison",      Util.GetItemIcon(16));
			_Fluid_ItemList.AddCustomItem((int)FluidType.Lava,       "Liquid Lava", Util.GetItemIcon(24));
			_Fluid_ItemList.AddCustomItem((int)FluidType.Plasma,     "Plasma",      Util.GetItemIcon(2135));
		}

		private void _AddFileMenuButton(string text, int id)
		{
			_File_PopupMenu.RemoveItem(_File_PopupMenu.ItemCount - 1);
			_File_PopupMenu.RemoveItem(_File_PopupMenu.ItemCount - 1);
			_File_PopupMenu.RemoveItem(_File_PopupMenu.ItemCount - 1);
			_File_PopupMenu.RemoveItem(_File_PopupMenu.ItemCount - 1);
			_File_PopupMenu.RemoveItem(_File_PopupMenu.ItemCount - 1);

			if (_File_PopupMenu.ItemCount == 3)
				_File_PopupMenu.AddSeparator();

			_File_PopupMenu.AddItem(text, id);

			PopupMenu newPopupMenu = new();
			newPopupMenu.AddItem("Save",          0);
			newPopupMenu.AddItem("Save As...",    3);
			newPopupMenu.AddItem("Export...",     5);
			newPopupMenu.AddItem("Export Raw...", 1);
			newPopupMenu.AddItem("Import Raw...", 4);
			newPopupMenu.AddItem("Close",         2);

			switch (id)
			{
				case FILE_MENU_PARAM_DATA_ID:
					newPopupMenu.SetItemDisabled(2, true);
					break;
				case FILE_MENU_WORLD_DATA_ID:
					break;

				case FILE_MENU_BLUEPRINT_ASSET_DQB1_ID:
					newPopupMenu.SetItemDisabled(0, true);
					newPopupMenu.SetItemDisabled(1, true);
					newPopupMenu.SetItemDisabled(3, true);
					newPopupMenu.SetItemDisabled(4, true);
					break;

				case FILE_MENU_DIORAMA_HEADER_ASSET_DQB1_ID:
					newPopupMenu.SetItemDisabled(0, true);
					newPopupMenu.SetItemDisabled(1, true);
					newPopupMenu.SetItemDisabled(2, true);
					newPopupMenu.SetItemDisabled(3, true);
					newPopupMenu.SetItemDisabled(4, true);
					break;
				case FILE_MENU_DIORAMA_DATA_ASSET_DQB1_ID:
					newPopupMenu.SetItemDisabled(0, true);
					newPopupMenu.SetItemDisabled(1, true);
					newPopupMenu.SetItemDisabled(2, true);
					newPopupMenu.SetItemDisabled(3, true);
					newPopupMenu.SetItemDisabled(4, true);
					break;

				case FILE_MENU_DIORAMA_ID:
					newPopupMenu.SetItemDisabled(0, true);
					newPopupMenu.SetItemDisabled(1, true);
					newPopupMenu.SetItemDisabled(3, true);
					newPopupMenu.SetItemDisabled(4, true);
					break;

				case FILE_MENU_COMMON_DATA_ID:
					newPopupMenu.SetItemDisabled(2, true);
					break;
				case FILE_MENU_STAGE_DATA_ID:
					break;
				case FILE_MENU_SCREENSHOT_DATA_ID:
					newPopupMenu.SetItemDisabled(2, true);
					break;
				
				case FILE_MENU_BLUEPRINT_FILE_DQB2_ID:
					newPopupMenu.SetItemDisabled(3, true);
					newPopupMenu.SetItemDisabled(4, true);
					break;
				case FILE_MENU_EYE_OF_RUBISS_STRUCTURE_ID:
					newPopupMenu.SetItemDisabled(3, true);
					newPopupMenu.SetItemDisabled(4, true);
					break;
			}

			_File_PopupMenu.SetItemSubmenuNode(_File_PopupMenu.ItemCount - 1, newPopupMenu);

			newPopupMenu.IdPressed += (long buttonId) => _On_File_PopupMenu_Submenu_IdPressed(id, buttonId);
			
			_File_PopupMenu.AddSeparator();
			_File_PopupMenu.AddItem("Save All", 2);
			_File_PopupMenu.AddItem("Close All", 3);
			_File_PopupMenu.AddSeparator();
			_File_PopupMenu.AddItem("Quit", 4);
		}
		private void _RemoveFileMenuButton(int id)
		{
			int index = _File_PopupMenu.GetItemIndex(id);
			if (index >= 0)
			{
				if (_File_PopupMenu.GetItemSubmenuNode(index) is PopupMenu submenu)
					submenu.QueueFree();
				_File_PopupMenu.RemoveItem(index);
			}

			if (_File_PopupMenu.ItemCount == 9)
			{
				_File_PopupMenu.RemoveItem(3);
			}
		}

		public void UpdateLoadedData()
		{
			_File_PopupMenu?.SetItemDisabled(-4, !AnyIsLoaded()); // Save All
			_File_PopupMenu?.SetItemDisabled(-3, !AnyIsLoaded()); // Close All

			_Inventory_Button.Disabled = _CommonData is null && _ParamData is null;
			_Player_Button.Disabled = _CommonData is null;
			_Snapshots_Button.Disabled = _ScreenshotData is null;
			
			if (!string.IsNullOrEmpty(WorkingDirectory))
			{
				_IslandSelector_Button.Disabled = false;
				for (int idx = 1; idx < _IslandSelector_Button.ItemCount; idx++)
				{
					int id = _IslandSelector_Button.GetItemId(idx);
					_IslandSelector_Button.SetItemDisabled(idx, !File.Exists(Path.Combine(WorkingDirectory, $"STGDAT{id:D2}.BIN")));
				}
			}
			else
			{
				_IslandSelector_Button.Select(0);
				_IslandSelector_Button.Disabled = true;
			}

			if (_StageData is not null)
			{
				if (_StageData.IslandID == 12 || _StageData.IslandID == 13 || _StageData.IslandID == 16)
                {
                    if (_CommonData is not null)
                    {
                        switch (_StageData.IslandID)
                        {
                            case 12:
								_Gratitude_SpinBox.SetValueNoSignal(_CommonData.Buildertopia1Gratitude);
								break;
							case 13:
								_Gratitude_SpinBox.SetValueNoSignal(_CommonData.Buildertopia2Gratitude);
								break;
							case 16:
								_Gratitude_SpinBox.SetValueNoSignal(_CommonData.Buildertopia3Gratitude);
								break;
                        }
						_Gratitude_SpinBox.Editable = true;
                    }
					else
                    {
                        
						_Gratitude_SpinBox.SetValueNoSignal(0);
						_Gratitude_SpinBox.Editable = false;
                    }
                }
				else
                {
					_Gratitude_SpinBox.SetValueNoSignal(_StageData.Gratitude);
					_Gratitude_SpinBox.Editable = true;
                }
				_Time_SpinBox.SetValueNoSignal(_StageData.Time * 1.2);
				_Time_SpinBox.UpdateLineEdit();
				_Time_SpinBox.Enable();

				_Weather_OptionButton.Clear();
				foreach (Info.DQB2.WeatherInfo weather in Info.DQB2.WeatherInfo.GetAll())
				{
					_Weather_OptionButton.AddItem(weather.Name, weather.ID);
				}
				_Weather_OptionButton.Select(_Weather_OptionButton.GetItemIndex(_StageData.Weather));
				_Weather_OptionButton.Disabled = false;
			}
			else if (_ParamData is not null)
			{
				_Time_SpinBox.SetValueNoSignal(_ParamData.Time * 2.4);
				_Time_SpinBox.UpdateLineEdit();
				_Time_SpinBox.Enable();

				_Gratitude_SpinBox.SetValueNoSignal(_ParamData.Score);
				_Gratitude_SpinBox.Editable = true;

				_Weather_OptionButton.Clear();
				foreach (Info.DQB1.WeatherInfo weather in Info.DQB1.WeatherInfo.GetAll())
				{
					_Weather_OptionButton.AddItem(weather.Name, weather.ID);
				}
				_Weather_OptionButton.Select(_Weather_OptionButton.GetItemIndex(_ParamData.Weather));
				_Weather_OptionButton.Disabled = false;
			}
			else
			{
				_Gratitude_SpinBox.SetValueNoSignal(0);
				_Gratitude_SpinBox.Editable = false;
				_Time_SpinBox.SetValueNoSignal(0);
				_Time_SpinBox.UpdateLineEdit();
				_Time_SpinBox.Disable();
				_Weather_OptionButton.Select(-1);
				_Weather_OptionButton.Disabled = true;
			}

			if (_CommonData is not null)
			{
				_PassTime_CheckBox.ButtonPressed = _CommonData.TimeIsPassing;
				_PassTime_CheckBox.Disabled = false;
			}
			else
			{
				_PassTime_CheckBox.ButtonPressed = false;
				_PassTime_CheckBox.Disabled = true;
			}
		}

        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event.IsActionPressed(Constants.Controls.KEYBOARD_SHORTCUT_OPEN_FOLDER))
			{
				ShowOpenFolderDialog();
			}
            else if (@event.IsActionPressed(Constants.Controls.KEYBOARD_SHORTCUT_OPEN_FILE))
			{
				ShowOpenFileDialog();
			}
            if (@event.IsActionPressed(Constants.Controls.KEYBOARD_SHORTCUT_SAVE))
			{
				SaveAll();
			}
            if (@event.IsActionPressed(Constants.Controls.KEYBOARD_SHORTCUT_CLOSE))
			{
				CloseAll();
			}

			// TEST
			if (@event.IsActionPressed(Constants.Controls.TEST1))
			{
			}
        }

		#region I/O Operations
		private enum FileDialogStateEnum
		{
			Unknown,

			OpenDirectory,
			SaveDirectory,

			OpenFile,

			SaveWorldData,
			ExportRawWorldData,
			ExportWorldData,
			ImportWorldData,

			SaveParamData,
			ExportRawParamData,
			ImportParamData,

			SaveCommonData,
			ExportRawCommonData,
			ImportCommonData,

			SaveStageData,
			ExportRawStageData,
			ExportStageData,
			ImportStageData,

			SaveScreenshotData,
			ExportRawScreenshotData,
			ImportScreenshotData,

			SaveBlueprintFileDQB2,

			SaveEyeOfRubissStructureFile,
			ExportEyeOfRubissStructureFile,

			ExportSelection,

			ExportResidentDQB1,
			ImportResidentDQB1,

			ExportResidentDQB2,
			ImportResidentDQB2,

			ExportSnapshot,
			ImportSnapshot,
			ExportAllSnapshots,
			
			CopyFromFile
		}
		private FileDialogStateEnum _FileDialogState = FileDialogStateEnum.Unknown;

		public void ShowOpenFileDialog()
		{
			_FileDialog.FileMode = FileDialog.FileModeEnum.OpenFiles;
			_FileDialogState = FileDialogStateEnum.OpenFile;
			_FileDialog.Title = "Open a file";
			_FileDialog.SetFilter("*.bin, *.json, *.unknown, *.dat");
			_FileDialog.PopupCentered();
		}
		public void ShowOpenFolderDialog()
		{
			_FileDialog.FileMode = FileDialog.FileModeEnum.OpenDir;
			_FileDialogState = FileDialogStateEnum.OpenDirectory;
			_FileDialog.Title = "Open a folder";
			_FileDialog.CurrentFile = "";
			_FileDialog.PopupCentered();
		}

		public void OpenFile(string path)
		{
			switch (Util.DetermineFileType(path))
            {
                case FileType.Unknown:
					StatusLabel.PrintMessage($"Could not open file {path}.");
					break;
				
				case FileType.DQB1_ParamData_PS3:
					TryOpenParamDataPS3(path);
					break;
				case FileType.DQB1_WorldData_PS3:
					TryOpenWorldDataPS3(path);
					break;

				case FileType.DQB1_ParamData_PS4:
					TryOpenParamDataPS4(path);
					break;
				case FileType.DQB1_WorldData_PS4:
					TryOpenWorldDataPS4(path);
					break;

				case FileType.DQB1_ParamData_Vita:
					TryOpenParamDataVita(path);
					break;

				case FileType.DQB1_WorldData_Switch:
					TryOpenWorldDataSwitch(path);
					break;
				case FileType.DQB1_ParamData_Switch:
					TryOpenParamDataSwitch(path);
					break;

                case FileType.DQB1_BlueprintAsset:
                    TryOpenBlueprintAssetDQB1(path);
                    break;
                case FileType.DQB1_DioramaHeaderAsset:
					TryOpenDioramaHeaderAssetDQB1(path);
					break;
				case FileType.DQB1_DioramaDataAsset:
					TryOpenDioramaDataAssetDQB1(path);
					break;

                case FileType.DQB2_StageData:
                    TryOpenStageData(path);
                    break;
                case FileType.DQB2_CommonData:
                    TryOpenCommonData(path);
                    break;
                case FileType.DQB2_ScreenshotData:
                    TryOpenScreenshotData(path);
                    break;

                case FileType.DQB2_PencilSketch:
                    TryOpenPencilSketchFileDQB2(path);
                    break;
				
				case FileType.DQB2_DioramaData:
					TryOpenDQB2Diorama(path);
					break;
				case FileType.DQB2_Blueprint:
					TryOpenDQB2Blueprint(path);
					break;
				
				case FileType.EyeOfRubissStructure:
					TryOpenEyeOfRubissStructure(path);
					break;
            }
			
			UpdateLoadedData();
		}

		public void CopyFromFile(string[] paths)
		{
			foreach (string path in paths)
			{
				GD.Print(path);
				try
				{
					switch (Util.DetermineFileType(path))
					{
						case FileType.DQB1_BlueprintAsset:
							GD.Print("DQB1_BlueprintAsset");
							_WorldEditorScene.Copy(EyeOfRubissStructure.From(BlueprintAssetDQB1.Load(path)));
							break;
						case FileType.DQB1_DioramaHeaderAsset:
							break;
						case FileType.DQB1_DioramaDataAsset:
							break;

						case FileType.DQB2_Blueprint:
							GD.Print("DQB2_Blueprint");
            				if (SaveData.TryLoad(path, out SaveData blueprint, 0, decompress: false))
            				{
								_WorldEditorScene.Copy(EyeOfRubissStructure.FromDQB2Blueprint(blueprint));
            				}
							else
								StatusLabel.PrintMessage($"Could not open file {path}.");
							break;
						case FileType.DQB2_DioramaHeader:
							// TODO
							break;
						case FileType.DQB2_DioramaData:
            				if (SaveData.TryLoad(path, out SaveData diorama, 0, decompress: false))
            				{
								EyeOfRubissStructure structure = EyeOfRubissStructure.FromDQB2Diorama(diorama,
									(int)(GetNode<SpinBox>("X_SpinBox").Value),
									(int)(GetNode<SpinBox>("Y_SpinBox").Value),
									(int)(GetNode<SpinBox>("Z_SpinBox").Value)
								);
								_WorldEditorScene.Copy(structure);
            				}
							else
								StatusLabel.PrintMessage($"Could not open file {path}.");
							break;
						case FileType.DQB2_PencilSketch:
							GD.Print("DQB2_PencilSketch");
            				if (PencilSketchFile.TryLoad(path, out PencilSketchFile pencilSketchFile))
            				{
								_WorldEditorScene.Copy(EyeOfRubissStructure.From(pencilSketchFile.PencilSketch));
            				}
							else
								StatusLabel.PrintMessage($"Could not open file {path}.");
							break;

						case FileType.EyeOfRubissStructure:
							GD.Print("EyeOfRubissStructure");
							_WorldEditorScene.Copy(EyeOfRubissStructure.Load(path));
							break;
						
						default:
							StatusLabel.PrintMessage($"Could not open file {path}.");
							break;
					}
				}
				catch (Exception ex)
				{
					StatusLabel.PrintMessage($"Could not open file {path}.");
					GD.PrintErr(ex);
				}
			}
		}

		public void CloseAll()
		{
			CloseWorldData();
			CloseParamData();
			CloseDioramaDataAssetDQB1();
			CloseDioramaHeaderAssetDQB1();
			CloseStageData();
			CloseCommonData();
			CloseScreenshotData();
			CloseEyeOfRubissStructure();

			WorkingDirectory = null;
		}

		private WorldData _WorldData;
		private const int FILE_MENU_WORLD_DATA_ID = 100;
        private bool TryOpenWorldDataPS3(string path)
        {
			if (WorldDataPS3.TryLoad(path, out WorldDataPS3 worldData))
            {
				CloseWorldData();
            	CloseStageData();
            	CloseCommonData();
            	CloseScreenshotData();
				CloseDioramaDataAssetDQB1();
				CloseDioramaHeaderAssetDQB1();
				CloseEyeOfRubissStructure();

            	_WorldData = worldData;
            	_WorldEditorScene.LoadWorldData(worldData);

				_AddFileMenuButton(Path.GetFileName(path), FILE_MENU_WORLD_DATA_ID);

				if (_ItemListMode != 1)
					_InitializeItemLists_DQB1();

				return true;
            }
            else
            {
                StatusLabel.PrintMessage($"Could not open file {path}.");
				return false;
            }
        }
        private bool TryOpenWorldDataPS4(string path)
        {
			if (WorldDataPS4.TryLoad(path, out WorldDataPS4 worldData))
            {
				CloseWorldData();
            	CloseStageData();
            	CloseCommonData();
            	CloseScreenshotData();
				CloseDioramaDataAssetDQB1();
				CloseDioramaHeaderAssetDQB1();
				CloseEyeOfRubissStructure();

            	_WorldData = worldData;
            	_WorldEditorScene.LoadWorldData(worldData);

				_AddFileMenuButton(Path.GetFileName(path), FILE_MENU_WORLD_DATA_ID);

				if (_ItemListMode != 1)
					_InitializeItemLists_DQB1();

				return true;
            }
            else
            {
                StatusLabel.PrintMessage($"Could not open file {path}.");
				return false;
            }
        }
        private bool TryOpenWorldDataSwitch(string path)
        {
			if (WorldDataSwitch.TryLoad(path, out WorldDataSwitch worldData))
            {
				CloseWorldData();
            	CloseStageData();
            	CloseCommonData();
            	CloseScreenshotData();
				CloseDioramaDataAssetDQB1();
				CloseDioramaHeaderAssetDQB1();
				CloseEyeOfRubissStructure();

            	_WorldData = worldData;
            	_WorldEditorScene.LoadWorldData(worldData);

				_AddFileMenuButton(Path.GetFileName(path), FILE_MENU_WORLD_DATA_ID);

				if (_ItemListMode != 1)
					_InitializeItemLists_DQB1();

				return true;
            }
            else
            {
                StatusLabel.PrintMessage($"Could not open file {path}.");
				return false;
            }
        }
		private void SaveWorldData(string path = null)
		{
			if (_ParamData is not null)
			{
				GD.Print($"OLD WORLD CHECKSUM: {_ParamData.WorldChecksum}");
				GD.Print($"NEW WORLD CHECKSUM: {_WorldData.CreateChecksum()}");
				_ParamData.WorldChecksum = _WorldData.CreateChecksum();
				GD.Print($"SAVED WORLD CHECKSUM: {_ParamData.WorldChecksum}");
			}
			_WorldData?.Save();
		}
		private void ExportWorldData(string path)
		{
			EyeOfRubissStructure structure = EyeOfRubissStructure.From(_WorldData);
			structure.Save(path);
		}
		private void CloseWorldData()
		{
			_WorldEditorScene.UnloadWorldData();
			_WorldData = null;
			_RemoveFileMenuButton(FILE_MENU_WORLD_DATA_ID);
			UpdateLoadedData();
		}

		private ParamData _ParamData;
		private const int FILE_MENU_PARAM_DATA_ID = 101;
        private bool TryOpenParamDataPS3(string path)
        {
			if (ParamDataPS3.TryLoad(path, out ParamDataPS3 paramData))
            {
				CloseParamData();
            	CloseStageData();
            	CloseCommonData();
            	CloseScreenshotData();
				CloseDioramaDataAssetDQB1();
				CloseDioramaHeaderAssetDQB1();
				CloseEyeOfRubissStructure();

            	_ParamData = paramData;
				_WorldEditorScene.LoadParamData(paramData);

				_AddFileMenuButton(Path.GetFileName(path), FILE_MENU_PARAM_DATA_ID);

				if (_ItemListMode != 1)
					_InitializeItemLists_DQB1();

				return true;
            }
            else
            {
                StatusLabel.PrintMessage($"Could not open file {path}.");
				return false;
            }
        }
        private bool TryOpenParamDataPS4(string path)
        {
			if (ParamDataPS4.TryLoad(path, out ParamDataPS4 paramData))
            {
				CloseParamData();
            	CloseStageData();
            	CloseCommonData();
            	CloseScreenshotData();
				CloseDioramaDataAssetDQB1();
				CloseDioramaHeaderAssetDQB1();
				CloseEyeOfRubissStructure();

            	_ParamData = paramData;
				_WorldEditorScene.LoadParamData(paramData);

				_AddFileMenuButton(Path.GetFileName(path), FILE_MENU_PARAM_DATA_ID);

				if (_ItemListMode != 1)
					_InitializeItemLists_DQB1();

				return true;
            }
            else
            {
                StatusLabel.PrintMessage($"Could not open file {path}.");
				return false;
            }
        }
        private bool TryOpenParamDataVita(string path)
        {
			if (ParamDataVita.TryLoad(path, out ParamDataVita paramData))
            {
				CloseParamData();
            	CloseStageData();
            	CloseCommonData();
            	CloseScreenshotData();
				CloseDioramaDataAssetDQB1();
				CloseDioramaHeaderAssetDQB1();
				CloseEyeOfRubissStructure();

            	_ParamData = paramData;
				_WorldEditorScene.LoadParamData(paramData);

				_AddFileMenuButton(Path.GetFileName(path), FILE_MENU_PARAM_DATA_ID);

				if (_ItemListMode != 1)
					_InitializeItemLists_DQB1();

				return true;
            }
            else
            {
                StatusLabel.PrintMessage($"Could not open file {path}.");
				return false;
            }
        }
        private bool TryOpenParamDataSwitch(string path)
        {
			if (ParamDataSwitch.TryLoad(path, out ParamDataSwitch paramData))
            {
				CloseParamData();
            	CloseStageData();
            	CloseCommonData();
            	CloseScreenshotData();
				CloseDioramaDataAssetDQB1();
				CloseDioramaHeaderAssetDQB1();
				CloseEyeOfRubissStructure();

            	_ParamData = paramData;
				_WorldEditorScene.LoadParamData(paramData);

				_AddFileMenuButton(Path.GetFileName(path), FILE_MENU_PARAM_DATA_ID);

				if (_ItemListMode != 1)
					_InitializeItemLists_DQB1();

				return true;
            }
            else
            {
                StatusLabel.PrintMessage($"Could not open file {path}.");
				return false;
            }
        }
		private void SaveParamData(string path = null)
		{
			_ParamData?.Save();
		}
		private void CloseParamData()
		{
			_WorldEditorScene.UnloadParamData();
			_ParamData = null;
			_RemoveFileMenuButton(FILE_MENU_PARAM_DATA_ID);
			UpdateLoadedData();
		}

		private const int FILE_MENU_BLUEPRINT_ASSET_DQB1_ID = 110;
        private bool TryOpenBlueprintAssetDQB1(string path)
        {
            try
            {
				CloseAll();

                BlueprintAssetDQB1 blueprint = BlueprintAssetDQB1.Load(path);

				_EyeOfRubissStructure = EyeOfRubissStructure.From(blueprint);
                _WorldEditorScene.LoadEyeOfRubissStructure(_EyeOfRubissStructure);

				_AddFileMenuButton(Path.GetFileName(path), FILE_MENU_BLUEPRINT_ASSET_DQB1_ID);

				if (_ItemListMode != 1)
					_InitializeItemLists_DQB1();

				return true;
            }
            catch (Exception ex)
            {
                StatusLabel.PrintMessage($"Could not open file {path}.");
				GD.PrintErr(ex);
				return false;
            }
        }

		private DioramaHeaderAssetDQB1 _DioramaHeaderAssetDQB1;
		private const int FILE_MENU_DIORAMA_HEADER_ASSET_DQB1_ID = 120;
		private const int FILE_MENU_DIORAMA_ID = 122;
		private bool TryOpenDioramaHeaderAssetDQB1(string path)
		{
			try
			{
				DioramaHeaderAssetDQB1 header = JsonSerializer.Deserialize<DioramaHeaderAssetDQB1>(Godot.FileAccess.GetFileAsString(path));

				CloseWorldData();
				CloseDioramaHeaderAssetDQB1();
				CloseStageData();
				CloseCommonData();
				CloseScreenshotData();
				CloseEyeOfRubissStructure();

				_DioramaHeaderAssetDQB1 = header;

				_AddFileMenuButton(Path.GetFileName(path), FILE_MENU_DIORAMA_HEADER_ASSET_DQB1_ID);

				if (_DioramaDataAssetDQB1 is not null)
				{
					_EyeOfRubissStructure = EyeOfRubissStructure.From(_DioramaHeaderAssetDQB1, _DioramaDataAssetDQB1);
					_WorldEditorScene.LoadEyeOfRubissStructure(_EyeOfRubissStructure);

					string name = _DioramaHeaderAssetDQB1.Name;

					CloseDioramaHeaderAssetDQB1();
					CloseDioramaDataAssetDQB1();

					_AddFileMenuButton(name, FILE_MENU_DIORAMA_ID);
				}

				if (_ItemListMode != 1)
					_InitializeItemLists_DQB1();

				return true;
			}
			catch (Exception ex)
			{
                StatusLabel.PrintMessage($"Could not open file {path}.");
				GD.PrintErr(ex);
				return false;
			}
		}
		private void CloseDioramaHeaderAssetDQB1()
		{
			_DioramaHeaderAssetDQB1 = null;
			_RemoveFileMenuButton(FILE_MENU_DIORAMA_HEADER_ASSET_DQB1_ID);
			UpdateLoadedData();
		}

		private DioramaDataAssetDQB1 _DioramaDataAssetDQB1;
		private const int FILE_MENU_DIORAMA_DATA_ASSET_DQB1_ID = 121;
		private bool TryOpenDioramaDataAssetDQB1(string path)
		{
			try
			{
				DioramaDataAssetDQB1 data = JsonSerializer.Deserialize<DioramaDataAssetDQB1>(Godot.FileAccess.GetFileAsString(path));

				CloseWorldData();
				CloseDioramaDataAssetDQB1();
				CloseStageData();
				CloseCommonData();
				CloseScreenshotData();
				CloseEyeOfRubissStructure();

				_DioramaDataAssetDQB1 = data;

				_AddFileMenuButton(Path.GetFileName(path), FILE_MENU_DIORAMA_DATA_ASSET_DQB1_ID);

				if (_DioramaHeaderAssetDQB1 is not null)
				{
					_EyeOfRubissStructure = EyeOfRubissStructure.From(_DioramaHeaderAssetDQB1, _DioramaDataAssetDQB1);
					_WorldEditorScene.LoadEyeOfRubissStructure(_EyeOfRubissStructure);

					CloseDioramaHeaderAssetDQB1();
					CloseDioramaDataAssetDQB1();

					_AddFileMenuButton(_DioramaHeaderAssetDQB1.Name, FILE_MENU_DIORAMA_ID);
				}
				
				if (_ItemListMode != 1)
					_InitializeItemLists_DQB1();

				return true;
			}
			catch (Exception ex)
			{
				StatusLabel.PrintMessage($"Could not open file {path}.");
				GD.PrintErr(ex);
				return false;
			}
		}
		private void CloseDioramaDataAssetDQB1()
		{
			_DioramaDataAssetDQB1 = null;
			_RemoveFileMenuButton(FILE_MENU_DIORAMA_DATA_ASSET_DQB1_ID);
			UpdateLoadedData();
		}

		private CommonData _CommonData;
		private const int FILE_MENU_COMMON_DATA_ID = 201;
        private bool TryOpenCommonData(string path)
        {
            if (CommonData.TryLoad(path, out CommonData commonData))
            {
				CloseWorldData();
				CloseDioramaDataAssetDQB1();
				CloseDioramaHeaderAssetDQB1();
				CloseCommonData();
				CloseEyeOfRubissStructure();

                _CommonData = commonData;
                _WorldEditorScene.LoadCommonData(commonData);

				_AddFileMenuButton(Path.GetFileName(path), FILE_MENU_COMMON_DATA_ID);

				if (_ItemListMode != 2)
					_InitializeItemLists_DQB2();

				return true;
            }
            else
            {
                StatusLabel.PrintMessage($"Could not open file {path}.");
				return false;
            }
        }
		private void SaveCommonData(string path = null)
		{
			_CommonData?.Save(path);
		}
		private void CloseCommonData()
        {
            _WorldEditorScene.UnloadCommonData();
            _CommonData = null;
			_RemoveFileMenuButton(FILE_MENU_COMMON_DATA_ID);
			UpdateLoadedData();
        }

		private StageData _StageData;
		private const int FILE_MENU_STAGE_DATA_ID = 200;
        private bool TryOpenStageData(string path)
        {
            if (StageData.TryLoad(path, out StageData stageData))
            {
				CloseWorldData();
				CloseDioramaDataAssetDQB1();
				CloseDioramaHeaderAssetDQB1();
				CloseStageData();
				CloseEyeOfRubissStructure();

                _StageData = stageData;
                _WorldEditorScene.LoadStageData(stageData);

				_AddFileMenuButton(Path.GetFileName(path), FILE_MENU_STAGE_DATA_ID);

				if (_ItemListMode != 2)
					_InitializeItemLists_DQB2();

				return true;
            }
            else
            {
                StatusLabel.PrintMessage($"Could not open file {path}.");
				return false;
            }
        }
        private void SaveStageData(string path = null)
		{
			_StageData?.Save(path);
		}
		private void ExportStageData(string path)
		{
			EyeOfRubissStructure structure = EyeOfRubissStructure.From(_StageData);
			structure.Save(path);
		}
		private void CloseStageData()
        {
            _WorldEditorScene.UnloadStageData();
            _StageData = null;
			_RemoveFileMenuButton(FILE_MENU_STAGE_DATA_ID);
			UpdateLoadedData();
        }

		private ScreenshotData _ScreenshotData;
		private const int FILE_MENU_SCREENSHOT_DATA_ID = 202;
        private bool TryOpenScreenshotData(string path)
        {
            if (ScreenshotData.TryLoad(path, out ScreenshotData screenshotData))
            {
				CloseWorldData();
				CloseDioramaDataAssetDQB1();
				CloseDioramaHeaderAssetDQB1();
				CloseScreenshotData();
				CloseEyeOfRubissStructure();

                _ScreenshotData = screenshotData;

				_AddFileMenuButton(Path.GetFileName(path), FILE_MENU_SCREENSHOT_DATA_ID);

				if (_ItemListMode != 2)
					_InitializeItemLists_DQB2();

				return true;
            }
            else
            {
                StatusLabel.PrintMessage($"Could not open file {path}.");
				return false;
            }
        }
        private void SaveScreenshotData(string path = null)
		{
			_ScreenshotData?.Save(path);
		}
		private void CloseScreenshotData()
        {
            _ScreenshotData = null;
			_RemoveFileMenuButton(FILE_MENU_SCREENSHOT_DATA_ID);
			UpdateLoadedData();
        }

		private const int FILE_MENU_BLUEPRINT_FILE_DQB2_ID = 210;
        private bool TryOpenPencilSketchFileDQB2(string path)
        {
            if (PencilSketchFile.TryLoad(path, out PencilSketchFile blueprintFile))
            {
				CloseAll();

				_EyeOfRubissStructure = EyeOfRubissStructure.From(blueprintFile.PencilSketch);
				_EyeOfRubissStructure.Filename = path;
                _WorldEditorScene.LoadEyeOfRubissStructure(_EyeOfRubissStructure);

				_AddFileMenuButton(Path.GetFileName(path), FILE_MENU_BLUEPRINT_FILE_DQB2_ID);

				if (_ItemListMode != 2)
					_InitializeItemLists_DQB2();

				return true;
            }
            else
            {
                StatusLabel.PrintMessage($"Could not open file {path}.");
				return false;
            }
        }

		private const int FILE_MENU_DQB2_DIORAMA_ID = 220;
        private bool TryOpenDQB2Diorama(string path)
        {
            if (SaveData.TryLoad(path, out SaveData diorama, 0, decompress: false))
            {
				CloseAll();

				_EyeOfRubissStructure = EyeOfRubissStructure.FromDQB2Diorama(diorama,
					(int)(GetNode<SpinBox>("X_SpinBox").Value),
					(int)(GetNode<SpinBox>("Y_SpinBox").Value),
					(int)(GetNode<SpinBox>("Z_SpinBox").Value)
				);
				_EyeOfRubissStructure.Filename = path;
                _WorldEditorScene.LoadEyeOfRubissStructure(_EyeOfRubissStructure);

				_AddFileMenuButton(Path.GetFileName(path), FILE_MENU_DQB2_DIORAMA_ID);

				if (_ItemListMode != 2)
					_InitializeItemLists_DQB2();

				return true;
            }
            else
            {
                StatusLabel.PrintMessage($"Could not open file {path}.");
				return false;
            }
        }

		private const int FILE_MENU_DQB2_BLUEPRINT_ID = 221;
        private bool TryOpenDQB2Blueprint(string path)
        {
            if (SaveData.TryLoad(path, out SaveData blueprint, 0, decompress: false))
            {
				CloseAll();

				_EyeOfRubissStructure = EyeOfRubissStructure.FromDQB2Blueprint(blueprint);
				_EyeOfRubissStructure.Filename = path;
                _WorldEditorScene.LoadEyeOfRubissStructure(_EyeOfRubissStructure);

				_AddFileMenuButton(Path.GetFileName(path), FILE_MENU_DQB2_BLUEPRINT_ID);

				if (_ItemListMode != 2)
					_InitializeItemLists_DQB2();

				return true;
            }
            else
            {
                StatusLabel.PrintMessage($"Could not open file {path}.");
				return false;
            }
        }

		private EyeOfRubissStructure _EyeOfRubissStructure;
		private const int FILE_MENU_EYE_OF_RUBISS_STRUCTURE_ID = 10000;
		public void CreateNewEyeOfRubissStructure(byte sourceGame, int sizeX, int sizeZ)
		{
			CloseAll();

            EyeOfRubissStructure structure = new()
            {
                SourceGame = sourceGame
            };

			for (int x = 0; x < sizeX; x++)
			{
				for (int z = 0; z < sizeZ; z++)
				{
					structure.SetBlock(new Vector3I(x, 0, z), Constants.BLOCK_EARTH);
				}
			}

			_EyeOfRubissStructure = structure;
			_WorldEditorScene.LoadEyeOfRubissStructure(_EyeOfRubissStructure);

			_AddFileMenuButton("Untitled", FILE_MENU_EYE_OF_RUBISS_STRUCTURE_ID);

			if (sourceGame == 1 && _ItemListMode != 1)
			{
				_InitializeItemLists_DQB1();
			}
			else if (sourceGame == 2 && _ItemListMode != 2)
			{
				_InitializeItemLists_DQB2();
			}
		}
		private bool TryOpenEyeOfRubissStructure(string path)
		{
            try
            {
				CloseAll();

				EyeOfRubissStructure structure = EyeOfRubissStructure.Load(path);

				if (structure is null)
					return false;

				_EyeOfRubissStructure = structure;
                _WorldEditorScene.LoadEyeOfRubissStructure(_EyeOfRubissStructure);

				_AddFileMenuButton(Path.GetFileName(path), FILE_MENU_EYE_OF_RUBISS_STRUCTURE_ID);

				if (_EyeOfRubissStructure.SourceGame == 1 && _ItemListMode != 1)
				{
					_InitializeItemLists_DQB1();
				}
				else if (_EyeOfRubissStructure.SourceGame == 2 && _ItemListMode != 2)
				{
					_InitializeItemLists_DQB2();
				}

				return true;
            }
            catch (Exception ex)
            {
                StatusLabel.PrintMessage($"Could not open file {path}.");
				GD.PrintErr(ex);
				return false;
            }
		}
		private void SaveEyeOfRubissStructureFile(string path = null)
		{
			if (string.IsNullOrEmpty(path) && string.IsNullOrEmpty(_EyeOfRubissStructure.Filename))
			{
				_FileDialogState = FileDialogStateEnum.SaveEyeOfRubissStructureFile;
				_FileDialog.Title = "Save Structure...";
				_FileDialog.SetFilter("*.json");
				_FileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
				_FileDialog.PopupCentered();
			}
			else
			{
				_EyeOfRubissStructure?.Save(path);
			}
		}
		private void ExportEyeOfRubissStructureFile(string path)
		{
			if (path.ToLower().EndsWith(".json"))
			{
				SaveEyeOfRubissStructureFile(path);
			}
			else
			{
				ExportEyeOfRubissStructureFileAsBlueprint(path);
			}
		}
		private void ExportEyeOfRubissStructureFileAsBlueprint(string path = null)
		{
			if (_EyeOfRubissStructure is not null)
			{
				PencilSketchFile blueprintFile = _EyeOfRubissStructure.ToBlueprint();
				if (blueprintFile is not null)
				{
					blueprintFile.Save(path ?? _EyeOfRubissStructure.Filename);
				}
				else
				{
					PopupWindow("Error", "This structure is too large to save as a DQB2 blueprint.");
				}
			}
		}
		private void CloseEyeOfRubissStructure()
		{
            _WorldEditorScene.UnloadEyeOfRubissStructure();
			_EyeOfRubissStructure = null;
			_RemoveFileMenuButton(FILE_MENU_BLUEPRINT_ASSET_DQB1_ID);
			_RemoveFileMenuButton(FILE_MENU_DIORAMA_ID);
			_RemoveFileMenuButton(FILE_MENU_BLUEPRINT_FILE_DQB2_ID);
			_RemoveFileMenuButton(FILE_MENU_EYE_OF_RUBISS_STRUCTURE_ID);
			_RemoveFileMenuButton(FILE_MENU_DQB2_DIORAMA_ID);
			_RemoveFileMenuButton(FILE_MENU_DQB2_BLUEPRINT_ID);
			UpdateLoadedData();
		}

		public string WorkingDirectory { get; set; } = null;
        public void OpenFolder(string path)
		{
			// TODO DQB1
			if (!TryOpenCommonData(Path.Join(path, "CMNDAT.BIN")))
				return;

			TryOpenScreenshotData(Path.Join(path, "SCSHDAT.BIN"));

			WorkingDirectory = path;

			UpdateLoadedData();
			// TODO
		}

		// These don't check for unsaved changes
		public void SaveAll()
		{
			SaveWorldData();
			SaveParamData();

			SaveStageData();
			SaveCommonData();
			SaveScreenshotData();

			if (_EyeOfRubissStructure is not null)
			{
				if (string.IsNullOrEmpty(_EyeOfRubissStructure.Filename) || _EyeOfRubissStructure.Filename.ToLower().EndsWith(".json"))
				{
					SaveEyeOfRubissStructureFile();
				}
				else
				{
					ExportEyeOfRubissStructureFileAsBlueprint();
				}
			}
		}
		public void TrySaveFolder(string path)
		{
			if (!DirAccess.DirExistsAbsolute(path))
			{
				DirAccess.MakeDirRecursiveAbsolute(path);
			}

			if (!string.IsNullOrEmpty(WorkingDirectory))
			{
				DirAccess dir = DirAccess.Open(WorkingDirectory);
				while (dir.GetNext() is string next && !string.IsNullOrEmpty(next))
				{
					if (next.ToLower().EndsWith(".bin") || next.ToLower().EndsWith(".tsf"))
					{
						DirAccess.CopyAbsolute(Path.Join(WorkingDirectory, next), Path.Join(path, next));
					}
				}
			}

			_CommonData?.Save(Path.Join(path, "CMNDAT.BIN"));
			_ScreenshotData?.Save(Path.Join(path, "SCSHDAT.BIN"));

			if (_StageData is not null)
			{
				_StageData.Save(Path.Join(path, _StageData.GetFileName()));
			}

			WorkingDirectory = path;
		}

		public void TryOpenFile(string path)
		{
			// TODO handle unsaved changes
			OpenFile(path);
			// OpenQueue.Enqueue(path);
			// DoCloseOpenQueue();
		}
		public void TryOpenFolder(string path)
		{
			// TODO handle unsaved changes
			OpenFolder(path);
			// OpenQueue.Enqueue(path);
			// DoCloseOpenQueue();
		}

		public void TryCloseAll()
		{
			// TODO handle unsaved changes
			CloseAll();
			// CloseQueue.Clear();
			// CloseQueue.Enqueue(StageData.Instance);
			// CloseQueue.Enqueue(CommonData.Instance);
			// CloseQueue.Enqueue(ScreenshotData.Instance);
			// DoCloseOpenQueue();
		}

		public void UnsavedChangesPopup(string message)
		{
			_UnsavedChanges_Label.Text = message;
			_UnsavedChanges_Window.PopupCentered();
		}

		/*public void DoCloseOpenQueue()
		{
			// Close queue
			while (CloseQueue.Count > 0)
			{
				SaveData saveData = CloseQueue.Peek();
				if (HasUnsavedChanges(saveData))
				{
					UnsavedChangesPopup($"{saveData.GetFileName()} has unsaved changes.\nWhat would you like to do?");
					return;
				}
				else
				{
					CloseFile(CloseQueue.Dequeue());
				}
			}

			if (WantsToQuit)
				GetTree().Quit();

			// Open queue
			while (OpenQueue.Count > 0)
			{
				string path = OpenQueue.Peek();

				if (DirAccess.DirExistsAbsolute(path))
				{
					if (AnyIsLoaded())
					{
						CloseQueue.Enqueue(StageData.Instance);
						CloseQueue.Enqueue(CommonData.Instance);
						CloseQueue.Enqueue(ScreenshotData.Instance);
						DoCloseOpenQueue();
						return;
					}

					OpenFolder(path);
				}
				if (StageData.IsStageDataFile(path) && StageData.HasInstance())
				{
					CloseQueue.Enqueue(StageData.Instance);
					DoCloseOpenQueue();
					return;
				}
				if (CommonData.IsCommonDataFile(path) && CommonData.HasInstance())
				{
					CloseQueue.Enqueue(CommonData.Instance);
					DoCloseOpenQueue();
					return;
				}
				if (ScreenshotData.IsScreenshotDataFile(path) && ScreenshotData.HasInstance())
				{
					CloseQueue.Enqueue(ScreenshotData.Instance);
					DoCloseOpenQueue();
					return;
				}

                OpenFile(OpenQueue.Dequeue());
			}

		}*/

		public static string GetDQB1Path()
		{
			return Path.Join(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "My Games", "DRAGON QUEST BUILDERS", "Steam");
		}
		public static string GetDQB2Path()
		{
			return Path.Join(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "My Games", "DRAGON QUEST BUILDERS II", "Steam");
		}
		public static string[] GetDQB2SteamAccountPaths()
		{
			return Directory.GetDirectories(GetDQB2Path());
		}
		public bool AnyIsLoaded()
		{
			return
				_WorldData is not null ||

				_DioramaHeaderAssetDQB1 is not null || 
				_DioramaDataAssetDQB1 is not null || 

				_CommonData is not null || 
				_StageData is not null || 
				_ScreenshotData is not null || 

				_EyeOfRubissStructure is not null;
				
		}
		#endregion

		#region Callbacks
		public void _On_File_PopupMenu_IdPressed(int id)
		{
			switch (id)
			{
				case 7: // New...
					_New_Window_X_SpinBox.Value = 16;
					_New_Window_Z_SpinBox.Value = 16;
					_New_Window.PopupCentered();
					break;

				case 0: // Open Folder...
					ShowOpenFolderDialog();
					break;
				case 1: // Open File...
					ShowOpenFileDialog();
					break;
				case 2: // Save All
					SaveAll();
					break;
				// case 3: // Save All As...
				// 	_FileDialog.FileMode = FileDialog.FileModeEnum.OpenDir;
				// 	_FileDialogState = FileDialogStateEnum.SaveDirectory;
				// 	_FileDialog.Title = "Choose a folder to save";
				// 	_FileDialog.PopupCentered();
				// 	break;
				case 3: // Close
					TryCloseAll(); // TODO handle unsaved changes
					break;
				case 4: // Quit
					_On_Root_CloseRequested();
					break;
				default:
					break;
			}
		}
		public void _On_File_PopupMenu_Submenu_IdPressed(long submenuId, long buttonId)
		{
			switch (submenuId)
			{
				case FILE_MENU_WORLD_DATA_ID:
					switch (buttonId)
					{
						case 0: // Save
							SaveWorldData();
							break;
						case 3: // Save As...
							_FileDialogState = FileDialogStateEnum.SaveWorldData;
							_FileDialog.Title = "Save WDAT...";
							_FileDialog.SetFilter("*.bin");
							_FileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
							_FileDialog.PopupCentered();
							break;
						case 5: // Export...
							_FileDialogState = FileDialogStateEnum.ExportWorldData;
							_FileDialog.Title = "Export...";
							_FileDialog.SetFilter("*.json");
							_FileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
							_FileDialog.PopupCentered();
							break;
						case 1: // Export Raw...
							_FileDialogState = FileDialogStateEnum.ExportRawWorldData;
							_FileDialog.Title = "Export WDAT...";
							_FileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
							_FileDialog.SetFilter("*", "All Files");
							_FileDialog.PopupCentered();
							break;
						case 4: // Import Raw...
							_FileDialogState = FileDialogStateEnum.ImportWorldData;
							_FileDialog.Title = "Import WDAT...";
							_FileDialog.FileMode = FileDialog.FileModeEnum.OpenFile;
							_FileDialog.SetFilter("*", "All Files");
							_FileDialog.PopupCentered();
							break;
						case 2: // Close
							CloseWorldData();
							break;
					}
					break;
				case FILE_MENU_PARAM_DATA_ID:
					switch (buttonId)
					{
						case 0: // Save
							SaveParamData();
							break;
						case 3: // Save As...
							_FileDialogState = FileDialogStateEnum.SaveParamData;
							_FileDialog.Title = "Save PRMDAT...";
							_FileDialog.SetFilter("*.bin");
							_FileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
							_FileDialog.PopupCentered();
							break;
						case 5: // Export...
							// Can't do!
							break;
						case 1: // Export Raw...
							_FileDialogState = FileDialogStateEnum.ExportRawParamData;
							_FileDialog.Title = "Export PRMDAT...";
							_FileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
							_FileDialog.SetFilter("*", "All Files");
							_FileDialog.PopupCentered();
							break;
						case 4: // Import Raw...
							_FileDialogState = FileDialogStateEnum.ImportParamData;
							_FileDialog.Title = "Import PRMDAT...";
							_FileDialog.FileMode = FileDialog.FileModeEnum.OpenFile;
							_FileDialog.SetFilter("*", "All Files");
							_FileDialog.PopupCentered();
							break;
						case 2: // Close
							CloseParamData();
							break;
					}
					break;

				case FILE_MENU_BLUEPRINT_ASSET_DQB1_ID:
					switch (buttonId)
					{
						case 0: // Save
							// Can't do!
							break;
						case 3: // Save As...
							// Can't do!
							break;
						case 5: // Export...
							_FileDialogState = FileDialogStateEnum.ExportEyeOfRubissStructureFile;
							_FileDialog.Title = "Export...";
							_FileDialog.SetFilter(["*.json", "*.bin"], ["Eye of Rubiss Structure", "DQB2 Blueprint"]);
							_FileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
							_FileDialog.PopupCentered();
							break;
						case 1: // Export Raw...
							// Can't do!
							break;
						case 4: // Import Raw...
							// Can't do!
							break;
						case 2: // Close
							CloseEyeOfRubissStructure();
							break;
					}
					break;
				
				case FILE_MENU_DIORAMA_HEADER_ASSET_DQB1_ID:
					switch (buttonId)
					{
						case 0: // Save
							// Can't do!
							break;
						case 3: // Save As...
							// Can't do!
							break;
						case 5: // Export...
							// Can't do!
							break;
						case 1: // Export Raw...
							// Can't do!
							break;
						case 4: // Import Raw...
							// Can't do!
							break;
						case 2: // Close
							CloseDioramaHeaderAssetDQB1();
							break;
					}
					break;
				case FILE_MENU_DIORAMA_DATA_ASSET_DQB1_ID:
					switch (buttonId)
					{
						case 0: // Save
							// Can't do!
							break;
						case 3: // Save As...
							// Can't do!
							break;
						case 5: // Export...
							// Can't do!
							break;
						case 1: // Export Raw...
							// Can't do!
							break;
						case 4: // Import Raw...
							// Can't do!
							break;
						case 2: // Close
							CloseDioramaDataAssetDQB1();
							break;
					}
					break;
				case FILE_MENU_DIORAMA_ID:
					switch (buttonId)
					{
						case 0: // Save
							// Can't do!
							break;
						case 3: // Save As...
							// Can't do!
							break;
						case 5: // Export...
							_FileDialogState = FileDialogStateEnum.ExportEyeOfRubissStructureFile;
							_FileDialog.Title = "Export...";
							_FileDialog.SetFilter(["*.json", "*.bin"], ["Eye of Rubiss Structure", "DQB2 Blueprint"]);
							_FileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
							_FileDialog.PopupCentered();
							break;
						case 1: // Export Raw...
							// Can't do!
							break;
						case 4: // Import Raw...
							// Can't do!
							break;
						case 2: // Close
							CloseEyeOfRubissStructure();
							break;
					}
					break;

				case FILE_MENU_COMMON_DATA_ID:
					switch (buttonId)
					{
						case 0: // Save
							SaveCommonData();
							break;
						case 3: // Save As...
							_FileDialogState = FileDialogStateEnum.SaveCommonData;
							_FileDialog.Title = "Save CMNDAT...";
							_FileDialog.SetFilter("*.bin");
							_FileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
							_FileDialog.PopupCentered();
							break;
						case 5: // Export...
							// Can't do!
							break;
						case 1: // Export Raw...
							_FileDialogState = FileDialogStateEnum.ExportRawCommonData;
							_FileDialog.Title = "Export CMNDAT...";
							_FileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
							_FileDialog.SetFilter("*", "All Files");
							_FileDialog.PopupCentered();
							break;
						case 4: // Import Raw...
							_FileDialogState = FileDialogStateEnum.ImportCommonData;
							_FileDialog.Title = "Import CMNDAT...";
							_FileDialog.FileMode = FileDialog.FileModeEnum.OpenFile;
							_FileDialog.SetFilter("*", "All Files");
							_FileDialog.PopupCentered();
							break;
						case 2: // Close
							CloseCommonData();
							break;
					}
					break;
				case FILE_MENU_STAGE_DATA_ID:
					switch (buttonId)
					{
						case 0: // Save
							SaveStageData();
							break;
						case 3: // Save As...
							_FileDialogState = FileDialogStateEnum.SaveStageData;
							_FileDialog.Title = "Save STGDAT...";
							_FileDialog.SetFilter("*.bin");
							_FileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
							_FileDialog.PopupCentered();
							break;
						case 5: // Export...
							_FileDialogState = FileDialogStateEnum.ExportStageData;
							_FileDialog.Title = "Export...";
							_FileDialog.SetFilter("*.json");
							_FileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
							_FileDialog.PopupCentered();
							break;
						case 1: // Export Raw...
							_FileDialogState = FileDialogStateEnum.ExportRawStageData;
							_FileDialog.Title = "Export STGDAT...";
							_FileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
							_FileDialog.SetFilter("*", "All Files");
							_FileDialog.PopupCentered();
							break;
						case 4: // Import Raw...
							_FileDialogState = FileDialogStateEnum.ImportStageData;
							_FileDialog.Title = "Import STGDAT...";
							_FileDialog.FileMode = FileDialog.FileModeEnum.OpenFile;
							_FileDialog.SetFilter("*", "All Files");
							_FileDialog.PopupCentered();
							break;
						case 2: // Close
							CloseStageData();
							break;
					}
					break;
				case FILE_MENU_SCREENSHOT_DATA_ID:
					switch (buttonId)
					{
						case 0: // Save
							SaveScreenshotData();
							break;
						case 3: // Save As...
							_FileDialogState = FileDialogStateEnum.SaveScreenshotData;
							_FileDialog.Title = "Save SCSHDAT...";
							_FileDialog.SetFilter("*.bin");
							_FileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
							_FileDialog.PopupCentered();
							break;
						case 5: // Export...
							// Can't do!
							break;
						case 1: // Export Raw...
							_FileDialogState = FileDialogStateEnum.ExportRawScreenshotData;
							_FileDialog.Title = "Export SCSHDAT...";
							_FileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
							_FileDialog.SetFilter("*", "All Files");
							_FileDialog.PopupCentered();
							break;
						case 4: // Import Raw...
							_FileDialogState = FileDialogStateEnum.ImportScreenshotData;
							_FileDialog.Title = "Import SCSHDAT...";
							_FileDialog.FileMode = FileDialog.FileModeEnum.OpenFile;
							_FileDialog.SetFilter("*", "All Files");
							_FileDialog.PopupCentered();
							break;
						case 2: // Close
							CloseScreenshotData();
							break;
					}
					break;
				
				case FILE_MENU_BLUEPRINT_FILE_DQB2_ID:
					switch (buttonId)
					{
						case 0: // Save
							ExportEyeOfRubissStructureFileAsBlueprint();
							break;
						case 3: // Save As...
							_FileDialogState = FileDialogStateEnum.SaveBlueprintFileDQB2;
							_FileDialog.Title = "Save Blueprint...";
							_FileDialog.SetFilter("*.bin");
							_FileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
							_FileDialog.PopupCentered();
							break;
						case 5: // Export...
							_FileDialogState = FileDialogStateEnum.ExportEyeOfRubissStructureFile;
							_FileDialog.Title = "Export...";
							_FileDialog.SetFilter(["*.json", "*.bin"], ["Eye of Rubiss Structure", "DQB2 Blueprint"]);
							_FileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
							_FileDialog.PopupCentered();
							break;
						case 1: // Export Raw...
							// Can't do!
							break;
						case 4: // Import Raw...
							// Can't do!
							break;
						case 2: // Close
							CloseEyeOfRubissStructure();
							break;
					}
					break;
				
				case FILE_MENU_EYE_OF_RUBISS_STRUCTURE_ID:
					switch (buttonId)
					{
						case 0: // Save
							SaveEyeOfRubissStructureFile();
							break;
						case 3: // Save As...
							_FileDialogState = FileDialogStateEnum.SaveEyeOfRubissStructureFile;
							_FileDialog.Title = "Save Structure...";
							_FileDialog.SetFilter("*.json");
							_FileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
							_FileDialog.PopupCentered();
							break;
						case 5: // Export...
							_FileDialogState = FileDialogStateEnum.ExportEyeOfRubissStructureFile;
							_FileDialog.Title = "Export...";
							_FileDialog.SetFilter(["*.json", "*.bin"], ["Eye of Rubiss Structure", "DQB2 Blueprint"]);
							_FileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
							_FileDialog.PopupCentered();
							break;
						case 1: // Export Raw...
							// Can't do!
							break;
						case 4: // Import Raw...
							// Can't do!
							break;
						case 2: // Close
							CloseEyeOfRubissStructure();
							break;
					}
					break;
			}
		}

		public void _On_Edit_PopupMenu_IdPressed(int id)
		{
			if (id == 8)
			{
				_FileDialog.FileMode = FileDialog.FileModeEnum.OpenFiles;
				_FileDialogState = FileDialogStateEnum.CopyFromFile;
				_FileDialog.Title = "Open a file";
				_FileDialog.SetFilter("*.bin, *.json, *.unknown, *.dat");
				_FileDialog.PopupCentered();
			}
		}

		public void _On_Settings_PopupMenu_IdPressed(int id)
		{
			switch (id)
			{
				case 0: // Advanced Mode
					break;
			}
		}

		public void _On_FileDialog_FileSelected(string path)
		{
			switch (_FileDialogState)
			{
				case FileDialogStateEnum.OpenFile:
					OpenFile(path);
					break;

				case FileDialogStateEnum.OpenDirectory:
					TryOpenFolder(path);
					break;
				case FileDialogStateEnum.SaveDirectory:
					TrySaveFolder(path);
					break;
				
				case FileDialogStateEnum.SaveWorldData:
					_WorldData?.Save(path);
					break;
				case FileDialogStateEnum.ExportWorldData:
					ExportWorldData(path);
					break;
				case FileDialogStateEnum.ExportRawWorldData:
					_WorldData?.Export(path);
					break;
				case FileDialogStateEnum.ImportWorldData:
					_WorldData.Import(path);
					_WorldEditorScene.Reload();
					break;

				case FileDialogStateEnum.SaveParamData:
					SaveParamData(path);
					break;
				case FileDialogStateEnum.ExportRawParamData:
					_ParamData?.Export(path);
					break;
				case FileDialogStateEnum.ImportParamData:
					_ParamData.Import(path);
					_WorldEditorScene.Reload();
					break;

				case FileDialogStateEnum.SaveStageData:
					_StageData?.Save(path);
					break;
				case FileDialogStateEnum.ExportRawStageData:
					_StageData?.Export(path);
					break;
				case FileDialogStateEnum.ImportStageData:
					_StageData?.Import(path);
					_WorldEditorScene.Reload();
					break;
				
				case FileDialogStateEnum.SaveCommonData:
					_CommonData?.Save(path);
					break;
				case FileDialogStateEnum.ExportStageData:
					ExportStageData(path);
					break;
				case FileDialogStateEnum.ExportRawCommonData:
					_CommonData?.Export(path);
					break;
				case FileDialogStateEnum.ImportCommonData:
					_CommonData?.Import(path);
					_WorldEditorScene.Reload();
					break;
				
				case FileDialogStateEnum.SaveScreenshotData:
					_ScreenshotData?.Save(path);
					break;
				case FileDialogStateEnum.ExportRawScreenshotData:
					_ScreenshotData?.Export(path);
					break;
				case FileDialogStateEnum.ImportScreenshotData:
					_ScreenshotData?.Import(path);
					break;
				
				case FileDialogStateEnum.SaveBlueprintFileDQB2:
					ExportEyeOfRubissStructureFileAsBlueprint(path);
					break;
				
				case FileDialogStateEnum.SaveEyeOfRubissStructureFile:
					SaveEyeOfRubissStructureFile(path);
					break;
				case FileDialogStateEnum.ExportEyeOfRubissStructureFile:
					ExportEyeOfRubissStructureFile(path);
					break;
				
				case FileDialogStateEnum.ExportSelection:
					_WorldEditorScene.ExportSelection(path);
					break;
				
				case FileDialogStateEnum.ExportResidentDQB1:
					_ResidentEditorDQB1.Export(path);
					break;
				case FileDialogStateEnum.ImportResidentDQB1:
					_ResidentEditorDQB1.Import(path);
					break;
				
				case FileDialogStateEnum.ExportResidentDQB2:
					_ResidentEditorDQB2.Export(path);
					break;
				case FileDialogStateEnum.ImportResidentDQB2:
					_ResidentEditorDQB2.Import(path);
					break;
				
				case FileDialogStateEnum.ExportSnapshot:
					_ScreenshotEditor.Export(path);
					break;
				case FileDialogStateEnum.ImportSnapshot:
					_ScreenshotEditor.Import(path);
					break;
				case FileDialogStateEnum.ExportAllSnapshots:
					_ScreenshotEditor.ExportAll(path);
					break;

				default:
					break;
			}
		}
		public void _On_FileDialog_FilesSelected(string[] paths)
		{
			if (_FileDialogState == FileDialogStateEnum.OpenFile)
			{
				foreach (string path in paths)
				{
					OpenFile(path);
				}
			}
			else if (_FileDialogState == FileDialogStateEnum.CopyFromFile)
			{
				CopyFromFile(paths);
			}
		}

		public void _On_UnsavedChanges_Window_Save_Button_Pressed()
		{
			_UnsavedChanges_Window.Hide();
			// TODO
		}
		public void _On_UnsavedChanges_Window_DontSave_Button_Pressed()
		{
			_UnsavedChanges_Window.Hide();
			// TODO
		}
		public void _On_UnsavedChanges_Window_Cancel_Button_Pressed()
		{
			_UnsavedChanges_Window.Hide();
			//WantsToQuit = false;
			// TODO
		}

		public void _On_IslandSelectorButton_ItemSelected(int index)
		{
			if (string.IsNullOrEmpty(WorkingDirectory))
				return;

			int id = _IslandSelector_Button.GetItemId(index);

			if (id <= 0)
			{
				CloseStageData();
			}
			else

			if (Godot.FileAccess.FileExists(Path.Join(WorkingDirectory, $"STGDAT{id:D2}.BIN")))
			{
				TryOpenStageData(Path.Join(WorkingDirectory, $"STGDAT{id:D2}.BIN"));
			}
		}

		public void _On_Gratitude_SpinBox_ValueChanged(float value)
		{
			if (_StageData is not null)
            {
				_StageData.Gratitude = (int)value;

				if (_CommonData is not null)
                {
                    switch (_StageData.IslandID)
                    {
                        case 12:
							_CommonData.Buildertopia1Gratitude = (int)value;
							break;
						case 13:
							_CommonData.Buildertopia2Gratitude = (int)value;
							break;
						case 16:
							_CommonData.Buildertopia3Gratitude = (int)value;
							break;
                    }
                }
            }
			else if (_ParamData is not null)
			{
				_ParamData.Score = (uint)value;
			}
		}
		public void _On_Time_SpinBox_ValueChanged(double value)
        {
            if (_StageData is not null)
				_StageData.Time = (float)(value / 1.2);
			else if (_ParamData is not null)
				_ParamData.Time = (float)(value / 2.4);
        }
		public void _On_Weather_OptionButton_ItemSelected(int index)
		{
			if (_StageData is not null)
			{
				_StageData.Weather = (byte)_Weather_OptionButton.GetItemId(index);
			}
			else if (_ParamData is not null)
			{
				_ParamData.Weather = (byte)_Weather_OptionButton.GetItemId(index);
			}
		}
		public void _On_PassTime_CheckBox_Toggled(bool toggledOn)
		{
			if (_CommonData is not null)
			{
				_CommonData.TimeIsPassing = toggledOn;
			}
		}

		public void _On_Inventory_Button_Pressed()
		{
			if (_ParamData is not null)
			{
				_InventoryEditor.Popup(_ParamData);
			}
			else if (_CommonData is not null)
			{
				_InventoryEditor.Popup(_CommonData);
			}
		}

		public void _On_Residents_Button_Pressed()
		{
			if (_ParamData is not null)
			{
				_ResidentEditorDQB1.Popup(_ParamData);
			}
			else if (_CommonData is not null)
			{
				_ResidentEditorDQB2.Popup(_CommonData);
			}
		}

		public void _On_Snapshots_Button_Pressed()
		{
			if (!_ScreenshotEditor.Visible)
			{
				_ScreenshotEditor.Show();

				_ScreenshotEditor.LoadScreenshotData(_ScreenshotData);
			}
			else
			{
				_ScreenshotEditor.Hide();
			}
		}

		public void _On_Blocks_ItemList_ItemSelected(int idx)
		{
			_BGParts_ItemList.DeselectAll();
			_Fluid_ItemList.DeselectAll();

			int id = _Block_ItemList.GetItemID(idx);
			_WorldEditorScene.SetBrushBlock(id);
		}
		public void _On_BGParts_ItemList_ItemSelected(int idx)
		{
			_Block_ItemList.DeselectAll();
			_Fluid_ItemList.DeselectAll();

			int id = _BGParts_ItemList.GetItemID(idx);
			_WorldEditorScene.SetBrushBGParts(id);
		}
		public void _On_Fluids_ItemList_ItemSelected(int idx)
		{
			_Block_ItemList.DeselectAll();
			_BGParts_ItemList.DeselectAll();
			
			int id = _Fluid_ItemList.GetItemID(idx);
			_WorldEditorScene.SetBrushFluid(id);
		}

		public void _On_Blocks_SearchBar_TextChanged(string newText)
		{
			PopulateBlockList(newText);
		}
		public void _On_BGParts_SearchBar_TextChanged(string newText)
		{
			PopulateBGPartsList(newText);
		}
		public void _On_Fluids_SearchBar_TextChanged(string newText)
		{
			// There is no fluids search bar.
		}
		
		public void _On_New_Window_Create_Button_Pressed()
		{
			_New_Window.Hide();

			byte sourceGame = (byte)(_New_Window_Game_OptionButton.Selected + 1);
			int sizeX = (int)_New_Window_X_SpinBox.Value;
			int sizeZ = (int)_New_Window_Z_SpinBox.Value;

			CreateNewEyeOfRubissStructure(sourceGame, sizeX, sizeZ);
        }
		
		public void _On_WorldHandler_ExportSelectionRequested()
		{
			_FileDialogState = FileDialogStateEnum.ExportSelection;
			_FileDialog.Title = "Export Selection...";
			_FileDialog.SetFilter(["*.json", "*.bin"], ["Eye of Rubiss Structure", "DQB2 Blueprint"]);
			_FileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
			_FileDialog.PopupCentered();
		}

		public void _On_ResidentEditorDQB1_ExportRequested()
		{
			_FileDialogState = FileDialogStateEnum.ExportResidentDQB1;
			_FileDialog.Title = "Export resident...";
			_FileDialog.SetFilter("*", "All Files");
			_FileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
			_FileDialog.PopupCentered();
		}
		public void _On_ResidentEditorDQB1_ImportRequested()
		{
			_FileDialogState = FileDialogStateEnum.ImportResidentDQB1;
			_FileDialog.Title = "Import resident...";
			_FileDialog.SetFilter("*", "All Files");
			_FileDialog.FileMode = FileDialog.FileModeEnum.OpenFile;
			_FileDialog.PopupCentered();
		}

		public void _On_ResidentEditorDQB2_ExportRequested()
		{
			_FileDialogState = FileDialogStateEnum.ExportResidentDQB2;
			_FileDialog.Title = "Export resident...";
			_FileDialog.SetFilter("*", "All Files");
			_FileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
			_FileDialog.PopupCentered();
		}
		public void _On_ResidentEditorDQB2_ImportRequested()
		{
			_FileDialogState = FileDialogStateEnum.ImportResidentDQB2;
			_FileDialog.Title = "Import resident...";
			_FileDialog.SetFilter("*", "All Files");
			_FileDialog.FileMode = FileDialog.FileModeEnum.OpenFile;
			_FileDialog.PopupCentered();
		}

		public void _On_ScreenshotEditor_ExportRequested()
		{
			_FileDialogState = FileDialogStateEnum.ExportSnapshot;
			_FileDialog.Title = "Export snapshot...";
			_FileDialog.SetFilter("*.jpg");
			_FileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
			_FileDialog.PopupCentered();
		}
		public void _On_ScreenshotEditor_ImportRequested()
		{
			_FileDialogState = FileDialogStateEnum.ImportSnapshot;
			_FileDialog.Title = "Import snapshot...";
			_FileDialog.SetFilter("*.jpg");
			_FileDialog.FileMode = FileDialog.FileModeEnum.OpenFile;
			_FileDialog.PopupCentered();
		}
		public void _On_ScreenshotEditor_ExportAllRequested()
		{
			_FileDialog.FileMode = FileDialog.FileModeEnum.OpenDir;
			_FileDialogState = FileDialogStateEnum.ExportAllSnapshots;
			_FileDialog.Title = "Export all snapshots...";
			_FileDialog.CurrentFile = "";
			_FileDialog.PopupCentered();
		}

		public void _On_Root_FilesDropped(string[] files)
		{
			foreach (string path in files)
			{
				OpenFile(path);
			}
		}
		public void _On_Root_CloseRequested()
		{
			// TODO Handle unsaved changes
			GetTree().Quit();

			//WantsToQuit = true;
			//TryCloseAll();
		}
		#endregion

		#region Static methods
		public static string GetPlayerName()
		{
			if (_Instance is not null)
			{
				if (_Instance._CommonData is CommonData commonData)
				{
					return commonData.PlayerName;
				}
			}

			return "the Builder";
		}
		public static string GetResidentName(int id)
		{
			if (_Instance._CommonData is null)
			{
				return Info.DQB2.StoryPeopleName.Get(id);
			}
			else
			{
				CommonData.Resident resident = _Instance._CommonData.GetResident(id);
				if (resident.UseCustomName)
				{
					return resident.Name;
				}
				else
				{
					return Info.DQB2.StoryPeopleName.Get(id);
					// TODO generic names
				}
			}
		}

		public static void PopupWindow(string title, string text)
		{
			if (_Instance is null)
				return;

            AcceptDialog dialog = new()
            {
                Title = title,
                DialogText = text
            };
            dialog.Canceled += dialog.QueueFree;
			dialog.Confirmed += dialog.QueueFree;
			
			_Instance.AddChild(dialog);
			dialog.PopupCentered();
		}
		#endregion
	}
}
