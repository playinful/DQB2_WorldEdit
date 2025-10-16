using Godot;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using EyeOfRubiss.Nodes;
using EyeOfRubiss.Info;
using System.Net;
using System.Collections.Generic;

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
		[ExportGroup("Scene Elements")]
		[Export] private WorldEditorScene _WorldEditorScene;
		[Export] private FileDialog _FileDialog;
		[Export] private Window _UnsavedChanges_Window;
		[Export] private Label _UnsavedChanges_Label;

		[Export] private PopupMenu _File_PopupMenu;
		[Export] private PopupMenu _File_SaveSingleFile_PopupMenu;
		[Export] private PopupMenu _File_SaveAsSingleFile_PopupMenu;
		[Export] private PopupMenu _File_Export_PopupMenu;
		[Export] private PopupMenu _File_Import_PopupMenu;
		[Export] private PopupMenu _Settings_PopupMenu;
		[Export] private PopupMenu _View_PopupMenu;
		[Export] private Button _Inventory_Button;
		[Export] private Button _Player_Button;

		[Export] private OptionButton _IslandSelector_Button;
		[Export] private SpinBox _Gratitude_SpinBox;
		[Export] private SpinBox _Time_SpinBox;
		[Export] private OptionButton _Weather_OptionButton;

		[Export] private ItemButtonSelector _Block_ItemButtonSelector;
		[Export] private ItemButtonSelector _Prop_ItemButtonSelector;
		[Export] private ItemButtonSelector _Fluid_ItemButtonSelector;
		
		[Export] private Control _Inventory_Panel;
		[Export] private PlayerEditor _PlayerEditor;

		[Export] private Control _ItemSelector_Panel;
		[Export] private ItemButtonSelector _ItemSelector;

		private enum FileDialogStateEnum
		{
			Unknown,
			OpenDirectory,
			SaveDirectory,
			OpenFile,
			SaveCMNDAT,
			SaveSTGDAT,
			SaveSCSHDAT,
			ExportCMNDAT,
			ExportSTGDAT,
			ExportSCSHDAT,
			ImportCMNDAT,
			ImportSTGDAT,
			ImportSCSHDAT
		}
		private FileDialogStateEnum _FileDialogState = FileDialogStateEnum.Unknown;

		public string WorkingDirectory { get; set; } = null;

		private SaveData PendingChangesSaveData;
		private Queue<SaveData> CloseQueue = [];
		private Queue<string> OpenQueue = [];
		private bool WantsToQuit = false;

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
			foreach (BlockInfo blockInfo in BlockInfo.GetAll()[..1158].Where(b => !b.Tags.Contains("noeditor") && b.FluidType == FluidType.Air).OrderBy(b => b.SortIndex))
			{
				_Block_ItemButtonSelector.AddButton(blockInfo.ID, blockInfo.Name, blockInfo.ImageID, 0 /*TODO*/, false /*TODO*/, 0 /*TODO*/);
			}
			foreach (PropInfo propInfo in PropInfo.GetAll().OrderBy(b => b.SortIndex))
			{
				_Prop_ItemButtonSelector.AddButton(propInfo.ID, propInfo.Name, propInfo.Icon, propInfo.Rarity, false /*TODO*/, 0 /*TODO*/);
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

			if (StageData.HasInstance())
			{
				_Gratitude_SpinBox.SetValueNoSignal(StageData.Instance.Gratitude);
				_Gratitude_SpinBox.Editable = true;
				_Time_SpinBox.SetValueNoSignal(StageData.Instance.Time);
				_Time_SpinBox.Editable = true;
				_Weather_OptionButton.Select((int)StageData.Instance.Weather);
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
			_File_PopupMenu?.SetItemDisabled(3, !AnyIsLoaded()); // Save All
			_File_PopupMenu?.SetItemDisabled(4, !AnyIsLoaded()); // Save All As...
			_File_PopupMenu?.SetItemDisabled(5, !AnyIsLoaded()); // Save File
			_File_PopupMenu?.SetItemDisabled(6, !AnyIsLoaded()); // Save File As
			_File_PopupMenu?.SetItemDisabled(8, !AnyIsLoaded()); // Export
			_File_PopupMenu?.SetItemDisabled(9, !AnyIsLoaded()); // Import
			_File_PopupMenu?.SetItemDisabled(11, !AnyIsLoaded()); // Close

			_File_SaveSingleFile_PopupMenu?.SetItemDisabled(0, !CommonData.HasInstance());
			_File_SaveSingleFile_PopupMenu?.SetItemDisabled(1, !StageData.HasInstance());
			_File_SaveSingleFile_PopupMenu?.SetItemDisabled(2, !ScreenshotData.HasInstance());

			_File_SaveAsSingleFile_PopupMenu?.SetItemDisabled(0, !CommonData.HasInstance());
			_File_SaveAsSingleFile_PopupMenu?.SetItemDisabled(1, !StageData.HasInstance());
			_File_SaveAsSingleFile_PopupMenu?.SetItemDisabled(2, !ScreenshotData.HasInstance());

			_File_Export_PopupMenu?.SetItemDisabled(0, !CommonData.HasInstance());
			_File_Export_PopupMenu?.SetItemDisabled(1, !StageData.HasInstance());
			_File_Export_PopupMenu?.SetItemDisabled(2, !ScreenshotData.HasInstance());

			_File_Import_PopupMenu?.SetItemDisabled(0, !CommonData.HasInstance());
			_File_Import_PopupMenu?.SetItemDisabled(1, !StageData.HasInstance());
			_File_Import_PopupMenu?.SetItemDisabled(2, !ScreenshotData.HasInstance());

			_Inventory_Button.Disabled = !CommonData.HasInstance();
			_Player_Button.Disabled = !CommonData.HasInstance();
		}

        public override void _Input(InputEvent @event)
        {
            
        }

		#region I/O Operations
		// These don't check for unsaved changes
		public static void SaveAll()
		{
			CommonData.Instance?.Save();
			StageData.Instance?.Save();
			ScreenshotData.Instance?.Save();
		}
		public void TrySaveFolder(string path)
		{
			// TODO
		}

		public void OpenFile(string path)
		{
			if (StageData.IsStageDataFile(path))
			{
				if (StageData.TryLoadAndSet(path) is StageData stageData)
				{
					_WorldEditorScene.LoadWorld(stageData);
					UpdateLoadedData();
					UpdateMenuButtons();
				}
			}
			else if (CommonData.IsCommonDataFile(path))
			{
				if (CommonData.TryLoadAndSet(path) is CommonData commonData)
				{
					_WorldEditorScene.LoadCommonData(commonData);
					UpdateLoadedData();
					UpdateMenuButtons();
				}
			}
			else if (ScreenshotData.IsScreenshotDataFile(path))
			{
				if (ScreenshotData.TryLoadAndSet(path) is ScreenshotData screenshotData)
				{
					UpdateLoadedData();
					UpdateMenuButtons();
				}
			}
			else
			{
				GD.Print($"COULDN'T OPEN FILE {path}");
			}
		}
		public void OpenFolder(string path)
		{
			if (CommonData.TryLoadAndSet(Path.Join(path, "CMNDAT.BIN")) is null)
				return;

			ScreenshotData.TryLoadAndSet(Path.Join(path, "SCSHDAT.BIN"));

			WorkingDirectory = path;

			UpdateLoadedData();
			UpdateMenuButtons();
			// TODO
		}
		public void CloseFile(SaveData saveData)
		{
			if (saveData is CommonData)
			{
				_WorldEditorScene.UnloadCommonData();
				CommonData.Close();
			}
			else if (saveData is StageData)
			{
				_WorldEditorScene.UnloadWorld();
				StageData.Close();
			}
			else if (saveData is ScreenshotData)
			{
				ScreenshotData.Close();
			}

			UpdateLoadedData();
		}
		public void CloseAll()
		{
			CloseFile(CommonData.Instance);
			CloseFile(StageData.Instance);
			CloseFile(ScreenshotData.Instance);
			WorkingDirectory = null;
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

		public static string GetDQB2Path()
		{
			return Path.Join(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "My Games", "DRAGON QUEST BUILDERS II", "Steam");
		}
		public static string[] GetDQB2SteamAccountPaths()
		{
			return Directory.GetDirectories(GetDQB2Path());
		}
		public static bool AnyIsLoaded()
		{
			return CommonData.HasInstance() || StageData.HasInstance() || ScreenshotData.HasInstance();
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
					_FileDialog.FileMode = FileDialog.FileModeEnum.OpenDir;
					_FileDialogState = FileDialogStateEnum.OpenDirectory;
					_FileDialog.Title = "Open a folder";
					_FileDialog.CurrentFile = "";
					_FileDialog.PopupCentered();
					break;
				case 1: // Open File...
					_FileDialog.FileMode = FileDialog.FileModeEnum.OpenFile;
					_FileDialogState = FileDialogStateEnum.OpenFile;
					_FileDialog.Title = "Open a file";
					_FileDialog.SetFilter("*.bin");
					_FileDialog.PopupCentered();
					break;
				case 2: // Save All
					SaveAll();
					break;
				case 3: // Save All As...
					_FileDialog.FileMode = FileDialog.FileModeEnum.OpenDir;
					_FileDialogState = FileDialogStateEnum.SaveDirectory;
					_FileDialog.Title = "Choose a folder to save";
					_FileDialog.PopupCentered();
					break;
				case 8: // Close
					TryCloseAll(); // TODO handle unsaved changes
					break;
				case 9: // Quit
					_On_Root_CloseRequested();
					break;
				default:
					break;
			}
		}
		public void _On_SaveSingleFile_PopupMenu_IdPressed(int id)
		{
			//_FileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
			switch (id)
			{
				case 0: // CMNDAT
					CommonData.Instance.Save();
					break;
				case 1: // STGDAT
					StageData.Instance.Save();
					break;
				case 2: // SCSHDAT
					ScreenshotData.Instance.Save();
					break;
				default:
					break;
			}
			//_FileDialog.PopupCentered();
		}
		public void _On_SaveAsSingleFile_PopupMenu_IdPressed(int id)
		{
			_FileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
			switch (id)
			{
				case 0: // CMNDAT
					_FileDialogState = FileDialogStateEnum.SaveCMNDAT;
					_FileDialog.Title = "Save CMNDAT...";
					_FileDialog.SetFilter("*.bin");
					break;
				case 1: // STGDAT
					_FileDialogState = FileDialogStateEnum.SaveSTGDAT;
					_FileDialog.Title = "Save STGDAT...";
					_FileDialog.SetFilter("*.bin");
					break;
				case 2: // SCSHDAT
					_FileDialogState = FileDialogStateEnum.SaveSCSHDAT;
					_FileDialog.Title = "Save SCSHDAT...";
					_FileDialog.SetFilter("*.bin");
					break;
				default:
					break;
			}
			_FileDialog.PopupCentered();
		}
		public void _On_Export_PopupMenu_IdPressed(int id)
		{
			_FileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
			_FileDialog.SetFilter("*", "All Files");
			switch (id)
			{
				case 0: // CMNDAT
					_FileDialogState = FileDialogStateEnum.ExportCMNDAT;
					_FileDialog.Title = "Export CMNDAT...";
					break;
				case 1: // STGDAT
					_FileDialogState = FileDialogStateEnum.ExportSTGDAT;
					_FileDialog.Title = "Export STGDAT...";
					break;
				case 2: // SCSHDAT
					_FileDialogState = FileDialogStateEnum.ExportSCSHDAT;
					_FileDialog.Title = "Export SCSHDAT...";
					break;
				default:
					break;
			}
			_FileDialog.PopupCentered();
		}
		public void _On_Import_PopupMenu_IdPressed(int id)
		{
			_FileDialog.FileMode = FileDialog.FileModeEnum.OpenFile;
			_FileDialog.SetFilter("*", "All Files");
			switch (id)
			{
				case 0: // CMNDAT
					_FileDialogState = FileDialogStateEnum.ImportCMNDAT;
					_FileDialog.Title = "Import CMNDAT...";
					break;
				case 1: // STGDAT
					_FileDialogState = FileDialogStateEnum.ImportSTGDAT;
					_FileDialog.Title = "Import STGDAT...";
					break;
				case 2: // SCSHDAT
					_FileDialogState = FileDialogStateEnum.ImportSCSHDAT;
					_FileDialog.Title = "Import SCSHDAT...";
					break;
				default:
					break;
			}
			_FileDialog.PopupCentered();
		}

		public void _On_Edit_PopupMenu_IdPressed(int id)
		{
			switch (id)
			{
				case 0: // Make Superflat...
					StageData.Instance?.MakeSuperflat([1, 2, 2, 3]);
					_WorldEditorScene.Reload();
					break;
				case 1: // Delete All Props
					StageData.Instance?.DeleteAllProps();
					break;
				case 2: // Very Simple Copy
					GetNode<Window>("VeryBasicCopierWindow").Popup();
					break;
			}
		}

		public void _On_Settings_PopupMenu_IdPressed(int id)
		{
			switch (id)
			{
				case 0: // Advanced Mode
					break;
				case 1: // Show FPS
					bool showFps = !_Settings_PopupMenu.IsItemChecked(1);
					_Settings_PopupMenu.SetItemChecked(1, showFps);
					_WorldEditorScene.ChangeFPSDisplay(showFps);
					break;
				case 2: // Show Debug Info
					bool showDebugInfo = !_Settings_PopupMenu.IsItemChecked(2);
					_Settings_PopupMenu.SetItemChecked(2, showDebugInfo);
					_WorldEditorScene.ChangeDebugInfoDisplay(showDebugInfo);
					break;
			}
		}

		public void _On_View_PopupMenu_IdPressed(int id)
		{
			switch (id)
			{
				case 0: // Terrain
					bool showTerrain = !_View_PopupMenu.IsItemChecked(0);
					_View_PopupMenu.SetItemChecked(0, showTerrain);
					_WorldEditorScene.ChangeTerrainDisplay(showTerrain);
					break;
				case 1: // Props
					bool showProps = !_View_PopupMenu.IsItemChecked(1);
					_View_PopupMenu.SetItemChecked(1, showProps);
					_WorldEditorScene.ChangePropDisplay(showProps);
					break;
				case 2: // Prop Shells
					bool showPropShells = !_View_PopupMenu.IsItemChecked(2);
					_View_PopupMenu.SetItemChecked(2, showPropShells);
					_WorldEditorScene.ChangePropShellDisplay(showPropShells);
					break;
				case 3: // Residents
					bool showResidents = !_View_PopupMenu.IsItemChecked(3);
					_View_PopupMenu.SetItemChecked(3, showResidents);
					_WorldEditorScene.ChangeNPCDisplay(showResidents);
					break;
				case 4: // Player
					bool showPlayer = !_View_PopupMenu.IsItemChecked(4);
					_View_PopupMenu.SetItemChecked(4, showPlayer);
					_WorldEditorScene.ChangePlayerDisplay(showPlayer);
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
					TryOpenFile(path);
					break;
				case FileDialogStateEnum.SaveCMNDAT:
					CommonData.Instance?.Save(path);
					break;
				case FileDialogStateEnum.SaveSTGDAT:
					StageData.Instance?.Save(path);
					break;
				case FileDialogStateEnum.SaveSCSHDAT:
					ScreenshotData.Instance?.Save(path);
					break;
				case FileDialogStateEnum.ExportCMNDAT:
					CommonData.Instance?.Export(path);
					break;
				case FileDialogStateEnum.ExportSTGDAT:
					StageData.Instance?.Export(path);
					break;
				case FileDialogStateEnum.ExportSCSHDAT:
					ScreenshotData.Instance?.Export(path);
					break;
				case FileDialogStateEnum.ImportCMNDAT:
					CommonData.Instance?.Import(path);
					break;
				case FileDialogStateEnum.ImportSTGDAT:
					StageData.Instance?.Import(path);
					break;
				case FileDialogStateEnum.ImportSCSHDAT:
					ScreenshotData.Instance?.Import(path);
					break;
				default:
					break;
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
			WantsToQuit = false;
			// TODO
		}

		public void _On_IslandSelectorButton_ItemSelected(int index)
		{
			if (string.IsNullOrEmpty(WorkingDirectory))
				return;

			int id = _IslandSelector_Button.GetItemId(index);

			if (id <= 0)
			{
				if (StageData.HasInstance())
					TryCloseFile(StageData.Instance);
			}
			else

			if (Godot.FileAccess.FileExists(Path.Join(WorkingDirectory, $"STGDAT{id:D2}.BIN")))
			{
				TryOpenFile(Path.Join(WorkingDirectory, $"STGDAT{id:D2}.BIN"));
			}
		}

		public void _On_Gratitude_SpinBox_ValueChanged(float value)
		{
			if (StageData.HasInstance())
				StageData.Instance.Gratitude = (int)value;
		}
		public void _On_Weather_OptionButton_ItemSelected(int index)
		{
			StageData.Instance.Weather = (byte)index;
		}

		public void _On_Inventory_Button_Pressed()
		{
			_Inventory_Panel.ToggleVisible();
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
		public void DoVerySimpleCopy()
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
		#endregion
	}
}
