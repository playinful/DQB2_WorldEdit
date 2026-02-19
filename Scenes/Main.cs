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

namespace EyeOfRubiss.Scenes
{
	/// <summary> The project's main scene. </summary>
	public partial class Main : Control
	{
		// TODO add backup functionality
		// TODO handle overwriting open files
		// TODO retake male body screenshots
		// TODO monster, animal, fish screenshots
		// TODO CMNDAT and SCSHDAT are CompressionLevel.Fastest, STGDAT is CompressionLevel.Optimal -- See if this has effect?
		//   Hacky-ass solution: if _Header.Length == StageData.HeaderLength

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

		[Export] private OptionButton _IslandSelector_Button;
		[Export] private SpinBox _Gratitude_SpinBox;
		[Export] private TimeSpinBox _Time_SpinBox;
		[Export] private OptionButton _Weather_OptionButton;

		[Export] private ItemButtonSelector _Block_ItemButtonSelector;
		[Export] private ItemButtonSelector _BGParts_ItemButtonSelector;
		[Export] private ItemButtonSelector _Fluid_ItemButtonSelector;
		
		[Export] private Control _Inventory_Panel;
		[Export] private PlayerEditor _PlayerEditor;

		[Export] private Control _ItemSelector_Panel;
		[Export] private ItemButtonSelector _ItemSelector;
		#endregion

		public override void _Ready()
		{
			//_OnReadyVariables();

			GetTree().AutoAcceptQuit = false;
			GetTree().Root.CloseRequested += _On_Root_CloseRequested;

			_InitializeFileDialogPath();
			UpdateLoadedData();
			UpdateMenuButtons();

			_InitializeItemButtonSelectors_DQB2();
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
		private void _InitializeItemButtonSelectors_DQB2()
		{
			foreach (Info.DQB2.BlockInfo blockInfo in Info.DQB2.BlockInfo.GetAll()[..1158].Where(b => !b.Tags.Contains("noeditor") && b.FluidType == FluidType.Air).OrderBy(b => b.Sort))
			{
				_Block_ItemButtonSelector.AddButton(blockInfo.ID, blockInfo.Name, blockInfo.Icon, 0 /*TODO*/, false /*TODO*/, 0 /*TODO*/);
			}
			foreach (Info.DQB2.BGPartsInfo partsInfo in Info.DQB2.BGPartsInfo.GetAll().OrderBy(b => b.Sort))
			{
				_BGParts_ItemButtonSelector.AddButton(partsInfo.ID, partsInfo.Name, partsInfo.Icon, partsInfo.Rarity, false /*TODO*/, 0 /*TODO*/);
			}
			
			_Fluid_ItemButtonSelector.AddButton((int)FluidType.Water,      "Water",         73);
            _Fluid_ItemButtonSelector.AddButton((int)FluidType.Seawater,   "Seawater",    2131);
            _Fluid_ItemButtonSelector.AddButton((int)FluidType.HotWater,   "Hot Water",    798);
            _Fluid_ItemButtonSelector.AddButton((int)FluidType.MuddyWater, "Muddy Water", 2130);
            _Fluid_ItemButtonSelector.AddButton((int)FluidType.SwampWater, "Swamp Water", 2130);
            _Fluid_ItemButtonSelector.AddButton((int)FluidType.Poison,     "Poison",        16);
            _Fluid_ItemButtonSelector.AddButton((int)FluidType.Lava,       "Liquid Lava",   24);
            _Fluid_ItemButtonSelector.AddButton((int)FluidType.Plasma,     "Plasma",      2135);

			_Block_ItemButtonSelector.Select(1); // Bedrock
		}

		private void _AddFileMenuButton(string text, int id)
		{
			_File_PopupMenu.RemoveItem(_File_PopupMenu.ItemCount - 1);
			_File_PopupMenu.RemoveItem(_File_PopupMenu.ItemCount - 1);
			_File_PopupMenu.RemoveItem(_File_PopupMenu.ItemCount - 1);
			_File_PopupMenu.RemoveItem(_File_PopupMenu.ItemCount - 1);
			_File_PopupMenu.RemoveItem(_File_PopupMenu.ItemCount - 1);

			if (_File_PopupMenu.ItemCount == 2)
				_File_PopupMenu.AddSeparator();

			_File_PopupMenu.AddItem(text, id);

			PopupMenu newPopupMenu = new();
			newPopupMenu.AddItem("Save",   0);
			newPopupMenu.AddItem("Save As...",   3);
			newPopupMenu.AddItem("Export...", 1);
			newPopupMenu.AddItem("Import...", 4);
			newPopupMenu.AddItem("Close",  2);

			switch (id)
			{
				case FILE_MENU_WORLD_DATA_ID:
					break;
				case FILE_MENU_BLUEPRINT_ASSET_DQB1_ID:
					newPopupMenu.SetItemDisabled(0, true);
					newPopupMenu.SetItemDisabled(1, true);
					newPopupMenu.SetItemDisabled(3, true);
					break;
				case FILE_MENU_DIORAMA_HEADER_ASSET_DQB1_ID:
					newPopupMenu.SetItemDisabled(0, true);
					newPopupMenu.SetItemDisabled(1, true);
					newPopupMenu.SetItemDisabled(3, true);
					break;
				case FILE_MENU_DIORAMA_DATA_ASSET_DQB1_ID:
					newPopupMenu.SetItemDisabled(0, true);
					newPopupMenu.SetItemDisabled(1, true);
					newPopupMenu.SetItemDisabled(3, true);
					break;
				case FILE_MENU_COMMON_DATA_ID:
					break;
				case FILE_MENU_STAGE_DATA_ID:
					break;
				case FILE_MENU_SCREENSHOT_DATA_ID:
					break;
				case FILE_MENU_BLUEPRINT_FILE_DQB2_ID:
					newPopupMenu.SetItemDisabled(3, true);
					break;
				case FILE_MENU_EYE_OF_RUBISS_STRUCTURE_FILE_ID:
					newPopupMenu.SetItemDisabled(3, true);
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
			int index = _File_PopupMenu.GetItemIndexById(id);
			if (index >= 0)
			{
				if (_File_PopupMenu.GetItemSubmenuNode(index) is PopupMenu submenu)
					submenu.QueueFree();
				_File_PopupMenu.RemoveItem(index);
			}

			if (_File_PopupMenu.ItemCount == 8)
			{
				_File_PopupMenu.RemoveItem(2);
			}
		}

		public void UpdateLoadedData()
		{
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
				_Time_SpinBox.Editable = true;
				_Time_SpinBox.UpdateLineEdit();
				_Weather_OptionButton.Select(_StageData.Weather);
				_Weather_OptionButton.Disabled = false;
			}
			else
			{
				_Gratitude_SpinBox.SetValueNoSignal(0);
				_Gratitude_SpinBox.Editable = false;
				_Time_SpinBox.SetValueNoSignal(0);
				_Time_SpinBox.Editable = false;
				_Weather_OptionButton.Select(0);
				_Weather_OptionButton.Disabled = true;
			}
		}
		public void UpdateMenuButtons()
		{
			_File_PopupMenu?.SetItemDisabled(-4, !AnyIsLoaded()); // Save All
			_File_PopupMenu?.SetItemDisabled(-3, !AnyIsLoaded()); // Close All

			_Inventory_Button.Disabled = _CommonData is null;
			_Player_Button.Disabled = _CommonData is null;
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
        }

		#region I/O Operations
		private enum FileDialogStateEnum
		{
			Unknown,

			OpenDirectory,
			SaveDirectory,

			OpenFile,

			SaveWorldData,
			ExportWorldData,
			ImportWorldData,

			ExportBlueprintAssetDQB1,

			ExportDioramaAssetDQB1,

			SaveCommonData,
			ExportCommonData,
			ImportCommonData,

			SaveStageData,
			ExportStageData,
			ImportStageData,

			SaveScreenshotData,
			ExportScreenshotData,
			ImportScreenshotData,

			ExportBlueprintFileDQB2,

			SaveEyeOfRubissStructureFile,
			ExportEyeOfRubissStructureFile
		}
		private FileDialogStateEnum _FileDialogState = FileDialogStateEnum.Unknown;

		public void ShowOpenFileDialog()
		{
			_FileDialog.FileMode = FileDialog.FileModeEnum.OpenFiles;
			_FileDialogState = FileDialogStateEnum.OpenFile;
			_FileDialog.Title = "Open a file";
			_FileDialog.SetFilter("*.bin, *.json");
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
			GD.Print($"Path: {path}");

			switch (Util.DetermineFileType(path))
            {
                case FileType.Unknown:
					GD.Print($"Could not open file {path}.");
					break;
				
				case FileType.DQB1_WorldData:
					TryOpenWorldData(path);
					break;

                case FileType.DQB1_BlueprintAsset:
                    TryOpenBlueprintAssetDQB1(path);
                    break;
                case FileType.DQB1_DioramaAssetHeader:
					TryOpenDioramaHeaderAssetDQB1(path);
					break;
				case FileType.DQB1_DioramaAssetData:
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

                case FileType.DQB2_Blueprint:
                    TryOpenBlueprintFileDQB2(path);
                    break;
            }
			
			UpdateLoadedData();
			UpdateMenuButtons();
		}
		public void CloseFile(SaveData saveData)
		{
			if (saveData is CommonData)
			{
				_WorldEditorScene.UnloadCommonData();
				_CommonData = null;
			}
			else if (saveData is StageData)
			{
				_WorldEditorScene.UnloadStageData();
				_StageData = null;
			}
			else if (saveData is ScreenshotData)
			{
				_ScreenshotData = null;
			}

			UpdateLoadedData();
			UpdateMenuButtons();
		}
		public void CloseAll()
		{
			CloseWorldData();
			CloseBlueprintAssetDQB1();
			CloseDioramaDataAssetDQB1();
			CloseDioramaHeaderAssetDQB1();
			CloseStageData();
			CloseCommonData();
			CloseScreenshotData();
			CloseBlueprintFileDQB2();
			CloseEyeOfRubissStructureFile();

			WorkingDirectory = null;
		}

		private WorldData _WorldData;
		private const int FILE_MENU_WORLD_DATA_ID = 100;
        private bool TryOpenWorldData(string path)
        {
			if (WorldData.TryLoad(path, out WorldData worldData))
            {
				CloseWorldData();
            	CloseStageData();
            	CloseCommonData();
            	CloseScreenshotData();
            	CloseBlueprintAssetDQB1();
				CloseDioramaDataAssetDQB1();
				CloseDioramaHeaderAssetDQB1();
            	CloseBlueprintFileDQB2();
				CloseEyeOfRubissStructureFile();

            	_WorldData = worldData;
            	_WorldEditorScene.LoadWorldData(worldData);

				_AddFileMenuButton(Path.GetFileName(path), FILE_MENU_WORLD_DATA_ID);

				return true;
            }
            else
            {
                GD.Print($"Could not open file {path}.");
				return false;
            }
        }
		private void SaveWorldData(string path = null)
		{
			// TODO
		}
		private void CloseWorldData()
		{
			_WorldEditorScene.UnloadWorldData();
			_WorldData = null;
			_RemoveFileMenuButton(FILE_MENU_WORLD_DATA_ID);
			UpdateLoadedData();
			UpdateMenuButtons();
		}

		private BlueprintAssetDQB1 _BlueprintAssetDQB1;
		private const int FILE_MENU_BLUEPRINT_ASSET_DQB1_ID = 110;
        private bool TryOpenBlueprintAssetDQB1(string path)
        {
            try
            {
                _BlueprintAssetDQB1 = BlueprintAssetDQB1.Load(path);

				CloseAll();

                _WorldEditorScene.LoadBlueprintAssetDQB1(_BlueprintAssetDQB1);

				_AddFileMenuButton(Path.GetFileName(path), FILE_MENU_BLUEPRINT_ASSET_DQB1_ID);

				return true;
            }
            catch
            {
                GD.Print($"Could not open file {path}.");
				return false;
            }
        }
        private void CloseBlueprintAssetDQB1()
        {
			_WorldEditorScene.UnloadBlueprintAssetDQB1();
            _BlueprintAssetDQB1 = null;
			_RemoveFileMenuButton(FILE_MENU_BLUEPRINT_ASSET_DQB1_ID);
			UpdateLoadedData();
			UpdateMenuButtons();
        }

		private DioramaHeaderAssetDQB1 _DioramaHeaderAssetDQB1;
		private const int FILE_MENU_DIORAMA_HEADER_ASSET_DQB1_ID = 120;
		private bool TryOpenDioramaHeaderAssetDQB1(string path)
		{
			try
			{
				DioramaHeaderAssetDQB1 header = JsonSerializer.Deserialize<DioramaHeaderAssetDQB1>(Godot.FileAccess.GetFileAsString(path));

				CloseWorldData();
				CloseBlueprintAssetDQB1();
				CloseDioramaHeaderAssetDQB1();
				CloseStageData();
				CloseCommonData();
				CloseScreenshotData();
				CloseBlueprintFileDQB2();
				CloseEyeOfRubissStructureFile();

				_DioramaHeaderAssetDQB1 = header;
				_WorldEditorScene.LoadDioramaHeaderAssetDQB1(header);

				_AddFileMenuButton(Path.GetFileName(path), FILE_MENU_DIORAMA_HEADER_ASSET_DQB1_ID);

				return true;
			}
			catch (Exception ex)
			{
                GD.Print($"Could not open file {path}.");
				GD.PrintErr(ex);
				return false;
			}
		}
		private void CloseDioramaHeaderAssetDQB1()
		{
			_WorldEditorScene.UnloadDioramaHeaderAssetDQB1();
			_DioramaHeaderAssetDQB1 = null;
			_RemoveFileMenuButton(FILE_MENU_DIORAMA_HEADER_ASSET_DQB1_ID);
			UpdateLoadedData();
			UpdateMenuButtons();
		}

		private DioramaDataAssetDQB1 _DioramaDataAssetDQB1;
		private const int FILE_MENU_DIORAMA_DATA_ASSET_DQB1_ID = 121;
		private bool TryOpenDioramaDataAssetDQB1(string path)
		{
			try
			{
				DioramaDataAssetDQB1 data = JsonSerializer.Deserialize<DioramaDataAssetDQB1>(Godot.FileAccess.GetFileAsString(path));

				CloseWorldData();
				CloseBlueprintAssetDQB1();
				CloseDioramaDataAssetDQB1();
				CloseStageData();
				CloseCommonData();
				CloseScreenshotData();
				CloseBlueprintFileDQB2();
				CloseEyeOfRubissStructureFile();

				_DioramaDataAssetDQB1 = data;
				_WorldEditorScene.LoadDioramaDataAssetDQB1(data);

				_AddFileMenuButton(Path.GetFileName(path), FILE_MENU_DIORAMA_DATA_ASSET_DQB1_ID);

				return true;
			}
			catch (Exception ex)
			{
				GD.PrintErr(ex);
                GD.Print($"Could not open file {path}.");
				return false;
			}
		}
		private void CloseDioramaDataAssetDQB1()
		{
			_WorldEditorScene.UnloadDioramaDataAssetDQB1();
			_DioramaDataAssetDQB1 = null;
			_RemoveFileMenuButton(FILE_MENU_DIORAMA_DATA_ASSET_DQB1_ID);
			UpdateLoadedData();
			UpdateMenuButtons();
		}

		private CommonData _CommonData;
		private const int FILE_MENU_COMMON_DATA_ID = 201;
        private bool TryOpenCommonData(string path)
        {
            if (CommonData.TryLoad(path, out CommonData commonData))
            {
				CloseWorldData();
				CloseBlueprintAssetDQB1();
				CloseDioramaDataAssetDQB1();
				CloseDioramaHeaderAssetDQB1();
				CloseCommonData();
				CloseBlueprintFileDQB2();
				CloseEyeOfRubissStructureFile();

                _CommonData = commonData;
                _WorldEditorScene.LoadCommonData(commonData);

				_AddFileMenuButton(Path.GetFileName(path), FILE_MENU_COMMON_DATA_ID);

				return true;
            }
            else
            {
                GD.Print($"Could not open file {path}.");
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
			UpdateMenuButtons();
        }

		private StageData _StageData;
		private const int FILE_MENU_STAGE_DATA_ID = 200;
        private bool TryOpenStageData(string path)
        {
            if (StageData.TryLoad(path, out StageData stageData))
            {
				CloseWorldData();
				CloseBlueprintAssetDQB1();
				CloseDioramaDataAssetDQB1();
				CloseDioramaHeaderAssetDQB1();
				CloseStageData();
				CloseBlueprintFileDQB2();
				CloseEyeOfRubissStructureFile();

                _StageData = stageData;
                _WorldEditorScene.LoadStageData(stageData);

				_AddFileMenuButton(Path.GetFileName(path), FILE_MENU_STAGE_DATA_ID);

				return true;
            }
            else
            {
                GD.Print($"Could not open file {path}.");
				return false;
            }
        }
        private void SaveStageData(string path = null)
		{
			_StageData?.Save(path);
		}
		private void CloseStageData()
        {
            _WorldEditorScene.UnloadStageData();
            _StageData = null;
			_RemoveFileMenuButton(FILE_MENU_STAGE_DATA_ID);
			UpdateLoadedData();
			UpdateMenuButtons();
        }

		private ScreenshotData _ScreenshotData;
		private const int FILE_MENU_SCREENSHOT_DATA_ID = 202;
        private bool TryOpenScreenshotData(string path)
        {
            if (ScreenshotData.TryLoad(path, out ScreenshotData screenshotData))
            {
				CloseWorldData();
				CloseBlueprintAssetDQB1();
				CloseDioramaDataAssetDQB1();
				CloseDioramaHeaderAssetDQB1();
				CloseScreenshotData();
				CloseBlueprintFileDQB2();
				CloseEyeOfRubissStructureFile();

                _ScreenshotData = screenshotData;

				_AddFileMenuButton(Path.GetFileName(path), FILE_MENU_SCREENSHOT_DATA_ID);

				return true;
            }
            else
            {
                GD.Print($"Could not open file {path}.");
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
			UpdateMenuButtons();
        }

		private BlueprintFileDQB2 _BlueprintFileDQB2;
		private const int FILE_MENU_BLUEPRINT_FILE_DQB2_ID = 210;
        private bool TryOpenBlueprintFileDQB2(string path)
        {
            if (BlueprintFileDQB2.TryLoad(path, out BlueprintFileDQB2 blueprintFile))
            {
				CloseAll();

                _BlueprintFileDQB2 = blueprintFile;
                _WorldEditorScene.LoadBlueprintDQB2(blueprintFile.Blueprint);

				_AddFileMenuButton(Path.GetFileName(path), FILE_MENU_BLUEPRINT_FILE_DQB2_ID);

				return true;
            }
            else
            {
                GD.Print($"Could not open file {path}.");
				return false;
            }
        }
		private void SaveBlueprintFileDQB2(string path = null)
		{
			// TODO
		}
		private void CloseBlueprintFileDQB2()
        {
			_WorldEditorScene.UnloadBlueprintDQB2();
            _BlueprintFileDQB2 = null;
			_RemoveFileMenuButton(FILE_MENU_BLUEPRINT_FILE_DQB2_ID);
			UpdateLoadedData();
			UpdateMenuButtons();
        }

		private EyeOfRubissStructureFile _EyeOfRubissStructureFile;
		private const int FILE_MENU_EYE_OF_RUBISS_STRUCTURE_FILE_ID = 10000;
		private bool TryOpenEyeOfRubissStructureFile(string path)
		{
			throw new NotImplementedException();// TODO
		}
		private void CloseEyeOfRubissStructureFile()
		{
			// TODO
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
			UpdateMenuButtons();
			// TODO
		}

		// These don't check for unsaved changes
		public void SaveAll()
		{
			_WorldData?.Save();

			_CommonData?.Save();
			_StageData?.Save();
			_ScreenshotData?.Save();
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
		public void TryCloseFile(SaveData saveData)
		{
			// TODO handle unsaved changes
			CloseFile(saveData);
			// CloseQueue.Enqueue(saveData);
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
				
				_BlueprintAssetDQB1 is not null || 

				_DioramaHeaderAssetDQB1 is not null || 
				_DioramaDataAssetDQB1 is not null || 

				_CommonData is not null || 
				_StageData is not null || 
				_ScreenshotData is not null || 

				_BlueprintFileDQB2 is not null ||

				_EyeOfRubissStructureFile is not null;
				
		}
		public static bool HasUnsavedChanges(SaveData saveData)
		{
			return saveData is not null && saveData.IsLoaded && saveData.UnsavedChanges;
		}
		#endregion


		#region Callbacks
		public void _On_File_PopupMenu_IdPressed(int id)
		{
			switch (id)
			{
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
							// TODO
							break;
						case 1: // Export...
							// TODO
							break;
						case 4: // Import...
							// TODO
							break;
						case 2: // Close
							CloseWorldData();
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
						case 1: // Export...
							// TODO
							break;
						case 4: // Import...
							// Can't do!
							break;
						case 2: // Close
							CloseBlueprintAssetDQB1();
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
						case 1: // Export...
							// TODO
							break;
						case 4: // Import...
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
						case 1: // Export...
							// TODO
							break;
						case 4: // Import...
							// Can't do!
							break;
						case 2: // Close
							CloseDioramaDataAssetDQB1();
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
							_FileDialog.Title = "Save Common Data...";
							_FileDialog.SetFilter("*.bin");
							_FileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
							_FileDialog.PopupCentered();
							break;
						case 1: // Export...
							_FileDialogState = FileDialogStateEnum.ExportCommonData;
							_FileDialog.Title = "Export CMNDAT...";
							_FileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
							_FileDialog.SetFilter("*", "All Files");
							_FileDialog.PopupCentered();
							break;
						case 4: // Import...
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
							_FileDialog.Title = "Save Stage Data...";
							_FileDialog.SetFilter("*.bin");
							_FileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
							_FileDialog.PopupCentered();
							break;
						case 1: // Export...
							_FileDialogState = FileDialogStateEnum.ExportStageData;
							_FileDialog.Title = "Export STGDAT...";
							_FileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
							_FileDialog.SetFilter("*", "All Files");
							_FileDialog.PopupCentered();
							break;
						case 4: // Import...
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
							_FileDialog.Title = "Save Sreenshot Data...";
							_FileDialog.SetFilter("*.bin");
							_FileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
							_FileDialog.PopupCentered();
							break;
						case 1: // Export...
							_FileDialogState = FileDialogStateEnum.ExportScreenshotData;
							_FileDialog.Title = "Export SCSHDAT...";
							_FileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
							_FileDialog.SetFilter("*", "All Files");
							_FileDialog.PopupCentered();
							break;
						case 4: // Import...
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
							SaveBlueprintFileDQB2();
							break;
						case 3: // Save As...
							// TODO
							break;
						case 1: // Export...
							// TODO
							break;
						case 4: // Import...
							// Can't do!
							break;
						case 2: // Close
							CloseBlueprintFileDQB2();
							break;
					}
					break;
				
				case FILE_MENU_EYE_OF_RUBISS_STRUCTURE_FILE_ID:
					// TODO
					break;
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
				case FileDialogStateEnum.OpenDirectory:
					TryOpenFolder(path);
					break;
				case FileDialogStateEnum.SaveDirectory:
					TrySaveFolder(path);
					break;
				case FileDialogStateEnum.OpenFile:
					OpenFile(path);
					break;
				case FileDialogStateEnum.SaveCommonData:
					_CommonData?.Save(path);
					break;
				case FileDialogStateEnum.SaveStageData:
					_StageData?.Save(path);
					break;
				case FileDialogStateEnum.SaveScreenshotData:
					_ScreenshotData?.Save(path);
					break;
				case FileDialogStateEnum.ExportCommonData:
					_CommonData?.Export(path);
					break;
				case FileDialogStateEnum.ExportStageData:
					_StageData?.Export(path);
					break;
				case FileDialogStateEnum.ExportScreenshotData:
					_ScreenshotData?.Export(path);
					break;
				case FileDialogStateEnum.ImportCommonData:
					_CommonData?.Import(path);
					_WorldEditorScene.Reload();
					break;
				case FileDialogStateEnum.ImportStageData:
					_StageData?.Import(path);
					_WorldEditorScene.Reload();
					break;
				case FileDialogStateEnum.ImportScreenshotData:
					_ScreenshotData?.Import(path);
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
		}
		public void _On_Time_SpinBox_ValueChanged(double value)
        {
            if (_StageData is not null)
				_StageData.Time = (float)(value / 1.2);
        }
		public void _On_Weather_OptionButton_ItemSelected(int index)
		{
			if (_StageData is not null)
				_StageData.Weather = (byte)index;
		}

		public void _On_Inventory_Button_Pressed()
		{
			_Inventory_Panel.ToggleVisible();
		}

		public void _On_Residents_Button_Pressed()
		{
			
		}

		public void _On_Root_CloseRequested()
		{
			// TODO Handle unsaved changes
			GetTree().Quit();

			//WantsToQuit = true;
			//TryCloseAll();
		}
		#endregion

		#region TEST
		/*public void DoVerySimpleCopy()
		{
			int x1 = (int)Math.Round(GetNode<SpinBox>("VeryBasicCopierWindow/VBoxContainer/GridContainer/SpinBoxX1").Value);
			int y1 = (int)Math.Round(GetNode<SpinBox>("VeryBasicCopierWindow/VBoxContainer/GridContainer/SpinBoxY1").Value);
			int z1 = (int)Math.Round(GetNode<SpinBox>("VeryBasicCopierWindow/VBoxContainer/GridContainer/SpinBoxZ1").Value);
			int x2 = (int)Math.Round(GetNode<SpinBox>("VeryBasicCopierWindow/VBoxContainer/GridContainer/SpinBoxX2").Value);
			int y2 = (int)Math.Round(GetNode<SpinBox>("VeryBasicCopierWindow/VBoxContainer/GridContainer/SpinBoxY2").Value);
			int z2 = (int)Math.Round(GetNode<SpinBox>("VeryBasicCopierWindow/VBoxContainer/GridContainer/SpinBoxZ2").Value);
			int x3 = (int)Math.Round(GetNode<SpinBox>("VeryBasicCopierWindow/VBoxContainer/HBoxContainer/SpinBoxX3").Value);
			int y3 = (int)Math.Round(GetNode<SpinBox>("VeryBasicCopierWindow/VBoxContainer/HBoxContainer/SpinBoxY3").Value);
			int z3 = (int)Math.Round(GetNode<SpinBox>("VeryBasicCopierWindow/VBoxContainer/HBoxContainer/SpinBoxZ3").Value);

			Vector3I from = new(x1, y1, z1);
			Vector3I bounds = new Vector3I(x2, y2, z2) - from;
			Vector3I to = new(x3, y3, z3);

			_WorldEditorScene.CopyPaste(from, bounds, to);
		}
		public void BasicPropEditor()
		{
			int propId = (int)Math.Round(GetNode<SpinBox>("BasicPropEditor/VBoxContainer/HBoxContainer1/SpinBox").Value);
			int rotation = (int)Math.Round(GetNode<SpinBox>("BasicPropEditor/VBoxContainer/HBoxContainer1/SpinBox2").Value);

			int x = (int)Math.Round(GetNode<SpinBox>("BasicPropEditor/VBoxContainer/HBoxContainer2/SpinBoxX").Value);
			int y = (int)Math.Round(GetNode<SpinBox>("BasicPropEditor/VBoxContainer/HBoxContainer2/SpinBoxY").Value);
			int z = (int)Math.Round(GetNode<SpinBox>("BasicPropEditor/VBoxContainer/HBoxContainer2/SpinBoxZ").Value);

			_WorldEditorScene.DoPropEditor(new Vector3I(x, y, z), (ushort)propId, (byte)rotation);
		}
		*/
		#endregion
	}
}
