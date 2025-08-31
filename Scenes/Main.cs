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
		[Export] private PopupMenu _File_PopupMenu;
		[Export] private PopupMenu _File_SaveSingleFile_PopupMenu;
		[Export] private PopupMenu _File_SaveAsSingleFile_PopupMenu;
		[Export] private PopupMenu _File_Export_PopupMenu;
		[Export] private PopupMenu _File_Import_PopupMenu;
		[Export] private PopupMenu _Settings_PopupMenu;
		[Export] private PopupMenu _View_PopupMenu;
		[Export] private OptionButton _IslandSelector_Button;
		[Export] private SpinBox _Gratitude_SpinBox;
		[Export] private SpinBox _Time_SpinBox;
		[Export] private OptionButton _Weather_OptionButton;

		/// <summary> Enum repreresenting the state of the FileDialog. </summary>
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
		/// <summary> Current state of the FileDialog. Handles how the FileDialog should behave. </summary>
		private FileDialogStateEnum _FileDialogState = FileDialogStateEnum.Unknown;

		public string WorkingDirectory { get; set; } = null;

		private bool WantsToQuit = false;

		public override void _Ready()
		{
			// NOMERGE temp workaround until I can export the Godot project as a windows app
			//if (IntegrationMain.ShouldSwitchToIntegrationMode(OS.GetCmdlineUserArgs()))
			if (IntegrationMain.ShouldSwitchToIntegrationMode(new string[] { "--driverFile", @"C:\Users\kramer\Documents\My Games\DRAGON QUEST BUILDERS II\Steam\76561198073553084\_INTEGRATE\driver.json" }))
			{
				CallDeferred(nameof(SwitchToIntegrationMode));
				return;
			}

			//_OnReadyVariables();

			GetTree().AutoAcceptQuit = false;
			GetTree().Root.CloseRequested += _On_Root_CloseRequested;

			_InitializeFileDialogPath();
			UpdateLoadedData();
			UpdateMenuButtons();
		}
		private void SwitchToIntegrationMode()
		{
			GetTree().ChangeSceneToFile("res://Integration/IntegrationMain.tscn");
		}

		/*private void _OnReadyVariables()
		{
			//_WorldEditorScene = GetNode<WorldEditorScene>("%WorldEditor");

			_FileDialog = GetNode<FileDialog>("Popups/FileDialog");

			_UnsavedChanges_Window = GetNode<Window>("Popups/UnsavedChangesWindow");

			_File_PopupMenu = GetNode<PopupMenu>("Toolbar/VBoxContainer/MenuBar_Main/MenuBar/FileMenu");
			_File_SaveSingleFile_PopupMenu = GetNode<PopupMenu>("Toolbar/VBoxContainer/MenuBar_Main/MenuBar/FileMenu/SaveSingleFileMenu");
			_File_SaveAsSingleFile_PopupMenu = GetNode<PopupMenu>("Toolbar/VBoxContainer/MenuBar_Main/MenuBar/FileMenu/SaveAsSingleFileMenu");
			_File_Export_PopupMenu = GetNode<PopupMenu>("Toolbar/VBoxContainer/MenuBar_Main/MenuBar/FileMenu/ExportMenu");
			_File_Import_PopupMenu = GetNode<PopupMenu>("Toolbar/VBoxContainer/MenuBar_Main/MenuBar/FileMenu/ImportMenu");

			_Settings_PopupMenu = GetNode<PopupMenu>("Toolbar/VBoxContainer/MenuBar_Main/MenuBar/SettingsMenu");

			_IslandSelector_Button = GetNode<OptionButton>("Toolbar/VBoxContainer/MenuBar_Stage/IslandSelectorButton");

			_Gratitude_SpinBox = GetNode<SpinBox>("Toolbar/VBoxContainer/MenuBar_Stage/GratitudeBox");
			//_Time_SpinBox = GetNode<SpinBox>("Toolbar/TimeBox");
			//_Weather_OptionButton = GetNode<OptionButton>("Toolbar/ComplexWeatherSelect");
		}*/
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
			_File_PopupMenu.SetItemDisabled(3, !AnyIsLoaded()); // Save All
			_File_PopupMenu.SetItemDisabled(4, !AnyIsLoaded()); // Save All As...
			_File_PopupMenu.SetItemDisabled(5, !AnyIsLoaded()); // Save File
			_File_PopupMenu.SetItemDisabled(6, !AnyIsLoaded()); // Save File As
			_File_PopupMenu.SetItemDisabled(8, !AnyIsLoaded()); // Export
			_File_PopupMenu.SetItemDisabled(9, !AnyIsLoaded()); // Import
			_File_PopupMenu.SetItemDisabled(11, !AnyIsLoaded()); // Close

			_File_SaveSingleFile_PopupMenu.SetItemDisabled(0, !CommonData.HasInstance());
			_File_SaveSingleFile_PopupMenu.SetItemDisabled(1, !StageData.HasInstance());
			_File_SaveSingleFile_PopupMenu.SetItemDisabled(2, !ScreenshotData.HasInstance());

			_File_SaveAsSingleFile_PopupMenu.SetItemDisabled(0, !CommonData.HasInstance());
			_File_SaveAsSingleFile_PopupMenu.SetItemDisabled(1, !StageData.HasInstance());
			_File_SaveAsSingleFile_PopupMenu.SetItemDisabled(2, !ScreenshotData.HasInstance());

			_File_Export_PopupMenu.SetItemDisabled(0, !CommonData.HasInstance());
			_File_Export_PopupMenu.SetItemDisabled(1, !StageData.HasInstance());
			_File_Export_PopupMenu.SetItemDisabled(2, !ScreenshotData.HasInstance());

			_File_Import_PopupMenu.SetItemDisabled(0, !CommonData.HasInstance());
			_File_Import_PopupMenu.SetItemDisabled(1, !StageData.HasInstance());
			_File_Import_PopupMenu.SetItemDisabled(2, !ScreenshotData.HasInstance());
		}

		public static void SaveAll()
		{
			CommonData.Instance?.Save();
			StageData.Instance?.Save();
			ScreenshotData.Instance?.Save();
		}
		public void CloseAll()
		{
			CommonData.Close();
			StageData.Close();
			ScreenshotData.Close();
			_WorldEditorScene.UnloadWorld();
		}

		public bool TryCloseFile()
		{
			if ((CommonData.HasInstance() && CommonData.Instance.UnsavedChanges) || (StageData.HasInstance() && StageData.Instance.UnsavedChanges) || (ScreenshotData.HasInstance() && ScreenshotData.Instance.UnsavedChanges))
			{
				_UnsavedChanges_Window.PopupCentered();
				return false;
			}
			else
			{
				CloseAll();
				return true;
			}
		}

		public bool TryLoadFolder(string path)
		{
			CloseAll(); // TODO handle
			if (CommonData.TryLoadAndSet(Path.Join(path, "CMNDAT.BIN")) is null)
				return false;

			ScreenshotData.TryLoadAndSet(Path.Join(path, "SCSHDAT.BIN"));

			WorkingDirectory = path;

			UpdateLoadedData();
			UpdateMenuButtons();

			return true;
			// TODO
		}
		public bool TryLoadFile(string path)
		{
			if (CommonData.TryLoadAndSet(path) is not null)
			{
				UpdateLoadedData();
				UpdateMenuButtons();
				_WorldEditorScene.CreateResidents(CommonData.Instance);
				return true;
			}
			if (StageData.TryLoadAndSet(path) is not null)
			{
				UpdateLoadedData();
				UpdateMenuButtons();
				_WorldEditorScene.UnloadWorld();
				_WorldEditorScene.LoadWorld(StageData.Instance);
				return true;
			}
			if (ScreenshotData.TryLoadAndSet(path) is not null)
			{
				UpdateLoadedData();
				UpdateMenuButtons();
				return true;
			}

			return false;
		}
		public void TrySaveFolder(string path)
		{
			// TODO
		}

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

		// Callback methods
		public void _On_File_PopupMenu_IdPressed(int id)
		{
			switch (id)
			{
				case 0: // Open Folder...
					_FileDialog.FileMode = FileDialog.FileModeEnum.OpenDir;
					_FileDialogState = FileDialogStateEnum.OpenDirectory;
					_FileDialog.Title = "Open a folder";
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
					TryCloseFile();
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
					TryLoadFolder(path);
					break;
				case FileDialogStateEnum.SaveDirectory:
					TrySaveFolder(path);
					break;
				case FileDialogStateEnum.OpenFile:
					TryLoadFile(path);
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
			SaveAll();
			CloseAll();
			if (WantsToQuit)
				GetTree().Quit();
		}
		public void _On_UnsavedChanges_Window_DontSave_Button_Pressed()
		{
			_UnsavedChanges_Window.Hide();
			CloseAll();
			if (WantsToQuit)
				GetTree().Quit();
		}
		public void _On_UnsavedChanges_Window_Cancel_Button_Pressed()
		{
			_UnsavedChanges_Window.Hide();
			WantsToQuit = false;
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

		public void _On_Root_CloseRequested()
		{
			WantsToQuit = true;
			if (TryCloseFile())
				GetTree().Quit();
		}

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
	}
}
