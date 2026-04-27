using EyeOfRubiss.Info;
using EyeOfRubiss.Info.DQB2;
using EyeOfRubiss.Nodes;
using Gizmo3DPlugin;
using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.XPath;

// TODO delete this
namespace EyeOfRubiss.Scenes
{
	public partial class WorldEditorScene : Node3D
	{
		[ExportGroup("Scene Elements")]
		[Export] public VoxelTerrain _VoxelTerrain;
		public VoxelTool _VoxelTool;
		[Export] public VoxelTerrain _VoxelTerrain_PropShells;
		public VoxelTool _VoxelTool_PropShells;
		[Export] public Node3D _ResidentLayer;
		[Export] public BGPartsGridManager _BGPartsGridManager;
		[Export] public NPCSprite _PlayerDisplay;

		[Export] public CameraController _CameraController;

		[Export] public Gizmo3D _Gizmo;

		[Export] public SelectionBox _SelectionBox;
		[Export] public SelectionBox _BoundaryBox;

		[Export] public CanvasItem _DebugInfoContainer;
		[Export] public FPSLabel _FPSLabel;
		[Export] public Label _PointedVoxelLabel;
		[Export] public StatusLabel _StatusLabel;
		[Export] public LineEdit _CommandParser;
		[Export] public AnimationPlayer _LoadingAnimationPlayer;

		[Export] public PopupMenu _View_PopupMenu;
		[Export] public PopupMenu _Edit_PopupMenu;
		[Export] public PopupMenu _Collision_PopupMenu;

		[Export] public Button _ChiselButton;
		[Export] public Button _PasteButton;

		[Export] public OptionButton _BGPartsBlock_OptionButton;

		[Export] public OptionButton _FluidLevel_OptionButton;

		[Export] private Window _Replace_Window;
		[Export] private OptionButton _ReplaceWindow_OptionButton_Replace;
		[Export] private OptionButton _ReplaceWindow_OptionButton_With;
		[Export] private CheckBox _ReplaceWindow_CheckBox_InSelection;

		[Export] private BGPartsEditor _Debug_PropEditor_Window;

		[ExportGroup("Settings")]
		[Export] public bool AutomaticallyGenerateBedrock = true;

		[Signal] public delegate void ExportSelectionRequestedEventHandler();

		private WorldHandler _WorldHandler;

		private NPCSprite _SelectedNPCSprite;

		private Vector3I? _AreaSelectionStart;
		private Vector3I? _AreaSelectionEnd;

		private bool _CollisionTerrain = true;
		private bool _CollisionFluids = true;
		private bool _CollisionBGParts = true;
		private bool _CollisionFloor = false;

		public override void _Ready()
		{
			_VoxelTool = _VoxelTerrain.GetVoxelTool();
			_VoxelTool_PropShells = _VoxelTerrain_PropShells.GetVoxelTool();

			_UpdateMenuButtons();
		}

		public override void _PhysicsProcess(double delta)
		{
			UpdatePointedVoxel();
		}

		private void _UpdateMenuButtons()
		{
			_Edit_PopupMenu?.SetItemDisabled(0, _AreaSelectionStart is null);
			_Edit_PopupMenu?.SetItemDisabled(1, _AreaSelectionStart is null);
			_Edit_PopupMenu?.SetItemDisabled(2, _AreaSelectionStart is null);
			_Edit_PopupMenu?.SetItemDisabled(3, _AreaSelectionStart is null);

			_PasteButton.Disabled = Clipboard is null;

			int sourceGame = GetSourceGame();
			_ChiselButton.Disabled = sourceGame == 1;

			_Edit_PopupMenu?.SetItemDisabled(5, !(sourceGame == 1 || sourceGame == 2));

			_BGPartsBlock_OptionButton.SetItemDisabled(8,  sourceGame == 1);
			_BGPartsBlock_OptionButton.SetItemDisabled(9,  sourceGame == 1);
			_BGPartsBlock_OptionButton.SetItemDisabled(10, sourceGame == 1);
			if (sourceGame == 1 && (_BGPartsBlock_OptionButton.Selected == 8 || _BGPartsBlock_OptionButton.Selected == 9 || _BGPartsBlock_OptionButton.Selected == 10))
			{
				_BGPartsBlock_OptionButton.Select(0);
				_BGPartsBlock = null;
			}
		}

		public VoxelRaycastResult GetPointedVoxel()
		{
			Vector3 origin = _CameraController.GlobalTransform.Origin;
			Vector3 forward = (Input.MouseMode == Input.MouseModeEnum.Captured) ?
				-_CameraController.Transform.Basis.Z.Normalized() : // Cast directly forward from camera if mouse is captured
				_CameraController.ProjectRayNormal(GetViewport().GetMousePosition()); // Otherwise cast by mouse position
			
			VoxelRaycastResult hit = _VoxelTool.Raycast(origin, forward, 4096, collisionMask: GetCollisionMask());
			return hit;
		}
		public uint GetCollisionMask()
		{
			uint collision = 0;
			if (_CollisionTerrain)
				collision |= 0b1;
			if (_CollisionFluids)
				collision |= 0b10;
			if (_CollisionBGParts)
				collision |= 0b100;
			if (_CollisionFloor)
				collision |= 0b1000;
			return collision;
		}
		public Node3D GetPointedObject()
		{
			Vector3 origin = _CameraController.GlobalTransform.Origin;
			Vector3 forward = (Input.MouseMode == Input.MouseModeEnum.Captured) ?
				-_CameraController.Transform.Basis.Z.Normalized() : // Cast directly forward from camera if mouse is captured
				_CameraController.ProjectRayNormal(GetViewport().GetMousePosition()); // Otherwise cast by mouse position
			var result = GetWorld3D().DirectSpaceState.IntersectRay(new PhysicsRayQueryParameters3D()
			{
				From = origin,
				To = origin + forward * 4096.0f
			});
			if (result.Count == 0)
				return null;
			
			Node collider = (Node) result["collider"];
			return collider.GetParent().GetParent<Node3D>();
		}
		private Vector3I? _LastPointedVoxel = null;
		private void UpdatePointedVoxel(bool forceUpdate = false)
		{
			VoxelRaycastResult result = GetPointedVoxel();
			if (result is not null)
			{
				if (BrushPrimary == BrushType.Pointer || BrushPrimary == BrushType.None)
				{
					HideSelectionBox();
				}
				else if (BrushPrimary == BrushType.Pencil)
				{
					ShowSelectionBox(result.PreviousPosition);
				}
				else if (BrushPrimary == BrushType.Paste)
				{
					ShowSelectionBox(result.PreviousPosition);
				}
				else
				{
					ShowSelectionBox(result.Position);
				}

				if ((!forceUpdate) && _LastPointedVoxel == result.Position)
					return;
				else
					_LastPointedVoxel = result.Position;

				_PointedVoxelLabel.Text = _WorldHandler.GetDebugInfo(result.Position);
			}
			else
			{
				_LastPointedVoxel = null;

				_PointedVoxelLabel.Text = "Targeted block: None";
				HideSelectionBox();
			}
		}

		private int GetSourceGame()
		{
			if (_WorldHandler is WorldHandlerDQB1)
				return 1;
			if (_WorldHandler is WorldHandlerDQB2)
				return 2;
			
			if (_WorldHandler is WorldHandlerEyeOfRubissStructure wheors && wheors.Structure is EyeOfRubissStructure structure)
			{
				return structure.SourceGame;
			}

			return 0;
		}



		public void PopupReplaceWindow()
		{
			_Replace_Window.PopupCentered();

			_ReplaceWindow_OptionButton_Replace.Clear();
			_ReplaceWindow_OptionButton_With.Clear();

			int sourceGame = GetSourceGame();
			if (sourceGame == 1)
			{
				foreach (Info.DQB1.BlockInfo blockInfo in Info.DQB1.BlockInfo.GetAll().OrderBy(b => b.Sort))
				{
					_ReplaceWindow_OptionButton_Replace.AddItem(blockInfo.Name);
					_ReplaceWindow_OptionButton_Replace.SetItemId(-1, blockInfo.ID);
					_ReplaceWindow_OptionButton_With.AddItem(blockInfo.Name);
					_ReplaceWindow_OptionButton_With.SetItemId(-1, blockInfo.ID);
					_ReplaceWindow_OptionButton_Replace.Select(0);
					_ReplaceWindow_OptionButton_With.Select(0);
				}
			}
			else if (sourceGame == 2)
			{
				foreach (Info.DQB2.BlockInfo blockInfo in Info.DQB2.BlockInfo.GetAll().OrderBy(b => b.Sort))
				{
					_ReplaceWindow_OptionButton_Replace.AddItem(blockInfo.Name);
					_ReplaceWindow_OptionButton_Replace.SetItemId(-1, blockInfo.ID);
					_ReplaceWindow_OptionButton_With.AddItem(blockInfo.Name);
					_ReplaceWindow_OptionButton_With.SetItemId(-1, blockInfo.ID);
					_ReplaceWindow_OptionButton_Replace.Select(0);
					_ReplaceWindow_OptionButton_With.Select(0);
				}
			}

			if (_AreaSelectionStart is not null)
			{
				_ReplaceWindow_CheckBox_InSelection.Disabled = false;
			}
			else
			{
				_ReplaceWindow_CheckBox_InSelection.ButtonPressed = false;
				_ReplaceWindow_CheckBox_InSelection.Disabled = true;
			}
		}

		#region Input
		public override void _Process(double delta)
		{
			if (Input.IsActionPressed(Constants.Controls.BRUSH_PRIMARY)   && BrushPrimary   == BrushType.Swap)
				DoBrush(GetPointedVoxel(), BrushPrimary);
			if (Input.IsActionPressed(Constants.Controls.BRUSH_SECONDARY) && BrushSecondary == BrushType.Swap)
				DoBrush(GetPointedVoxel(), BrushSecondary);
			if (Input.IsActionPressed(Constants.Controls.BRUSH_TERTIARY)  && BrushTertiary  == BrushType.Swap)
				DoBrush(GetPointedVoxel(), BrushTertiary);
		}
		public override void _UnhandledInput(InputEvent @event)
		{
			if (_Gizmo.Editing)
				return;

			if (@event.IsActionPressed(Constants.Controls.RESET_CAMERA))
            {
                ResetCamera();
            }

			if (@event.IsActionPressed(Constants.Controls.SCREENSHOT))
				TakeScreenshot();
			
			if (_WorldHandler is null)
				return;

			if (@event.IsActionPressed(Constants.Controls.BRUSH_PRIMARY))
				DoBrush(GetPointedVoxel(), BrushPrimary);
			if (@event.IsActionPressed(Constants.Controls.BRUSH_SECONDARY))
				DoBrush(GetPointedVoxel(), BrushSecondary);
			if (@event.IsActionPressed(Constants.Controls.BRUSH_TERTIARY))
				DoBrush(GetPointedVoxel(), BrushTertiary);
			
			if (@event.IsActionPressed(Constants.Controls.DELETE))
				DeleteSelection();
			if (@event.IsActionPressed(Constants.Controls.KEYBOARD_SHORTCUT_COPY))
				DoCopy();
			if (@event.IsActionPressed(Constants.Controls.KEYBOARD_SHORTCUT_CUT))
				DoCut();

            if (@event.IsActionPressed(Constants.Controls.KEYBOARD_SHORTCUT_FILL))
				FillSelection();
			
			if (OS.IsDebugBuild() && @event.IsActionPressed(Constants.Controls.PROP_EDITOR) && _WorldHandler is WorldHandlerDQB2 worldHandler && worldHandler._StageData is StageData stageData)
			{
				if (stageData.GetOverlappingBGParts(GetPointedVoxel().Position) is StageData.BGParts parts)
				{
					_Debug_PropEditor_Window.SetBGParts(parts);
					_Debug_PropEditor_Window.PopupCentered();
				}
			}
		}

		public void ResetCamera()
        {
			if (_WorldHandler is WorldHandlerDQB1)
			{
				_CameraController.Position = Vector3.Up * 32;
				_CameraController.Rotation = Vector3.Zero;
			}
			else if (_WorldHandler is WorldHandlerDQB2)
			{
				_CameraController.Position = Vector3.Up * 96;
				_CameraController.Rotation = Vector3.Zero;
			}
			else if (_WorldHandler is WorldHandlerEyeOfRubissStructure)
			{
				_CameraController.Position = new Vector3(16, 8, -8);
				_CameraController.Rotation = Vector3.Up * Mathf.Pi;
			}
			else
			{
				_CameraController.Position = Vector3.Zero;
				_CameraController.Rotation = Vector3.Zero;
			}
				
			_CameraController.Fov = 75.0f;
			_CameraController.Size = 41.6667f;
			_CameraController.Projection = Camera3D.ProjectionType.Perspective;
        }
		
		public void TakeScreenshot()
		{
			DateTime dateTime = DateTime.Now;
			var path = Path.Join(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyPictures), "Screenshot_" + dateTime.ToString("yyyy-MM-dd_HH-mm-ss.fff") + ".png");
			GetViewport().GetTexture().GetImage().SavePng(path);
		}

		public byte GetBGPartsPlacementDirection()
        {
			if (_BGPartsPlacementDirection >= 4)
			{
				int rotation = (int)Math.Round(_CameraController.Rotation.Y / (Math.PI / 2)) % 4;
				if (rotation < 0)
					rotation += 4;
				return (byte)rotation;	
			}
			else
				return _BGPartsPlacementDirection;
        }
		#endregion

		#region Callbacks
		public void _On_Edit_PopupMenu_IdPressed(int id)
		{
			switch (id)
			{
				case 3: // Copy
					DoCopy();
					break;
				case 4: // Cut
					DoCut();
					break;
				case 6: // Fill
					FillSelection();
					break;
				case 5: // Export Selection
					EmitSignal(SignalName.ExportSelectionRequested);
					break;

				case 7: // Replace...
					PopupReplaceWindow();
					break;

				case 0: // Make Superflat...
					//_StageData?.MakeSuperflat([1, 2, 2, 3]);
					//_WorldEditorScene.Reload();
					break;
				case 1: // Delete All Props
					//_StageData?.DeleteAllBGParts();
					break;
				case 2: // Very Simple Copy
					//GetNode<Window>("VeryBasicCopierWindow").Popup();
					break;
			}
		}
		public void _On_View_PopupMenu_IdPressed(int id)
		{
			int index = _View_PopupMenu.GetItemIndexById(id);
			switch (id)
			{
				case 6: // Show FPS
					bool showFps = !_View_PopupMenu.IsItemChecked(index);
					_View_PopupMenu.SetItemChecked(index, showFps);
					ChangeFPSDisplay(showFps);
					break;
				case 7: // Show Debug Info
					bool showDebugInfo = !_View_PopupMenu.IsItemChecked(index);
					_View_PopupMenu.SetItemChecked(index, showDebugInfo);
					ChangeDebugInfoDisplay(showDebugInfo);
					break;
				
				case 0: // Terrain
					bool showTerrain = !_View_PopupMenu.IsItemChecked(index);
					_View_PopupMenu.SetItemChecked(index, showTerrain);
					ChangeTerrainDisplay(showTerrain);
					break;
				case 8: // Fluids
					bool showFluids = !_View_PopupMenu.IsItemChecked(index);
					_View_PopupMenu.SetItemChecked(index, showFluids);
					ChangeFluidsDisplay(showFluids);
					break;
				case 1: // Props
					bool showBGParts = !_View_PopupMenu.IsItemChecked(index);
					_View_PopupMenu.SetItemChecked(index, showBGParts);
					ChangeBGPartsDisplay(showBGParts);
					break;
				case 2: // Prop Shells
					bool showPartsBlock = !_View_PopupMenu.IsItemChecked(index);
					_View_PopupMenu.SetItemChecked(index, showPartsBlock);
					ChangePartsBlockDisplay(showPartsBlock);
					break;
				case 3: // Residents
					bool showResidents = !_View_PopupMenu.IsItemChecked(index);
					_View_PopupMenu.SetItemChecked(index, showResidents);
					ChangeNPCDisplay(showResidents);
					break;
				case 4: // Player
					bool showPlayer = !_View_PopupMenu.IsItemChecked(index);
					_View_PopupMenu.SetItemChecked(index, showPlayer);
					ChangePlayerDisplay(showPlayer);
					break;
			}
		}
		public void _On_Collision_PopupMenu_IndexPressed(int index)
		{
			int id = _Collision_PopupMenu.GetItemId(index);
			switch (id)
			{
				case 0: // Terrain
					_CollisionTerrain = !_Collision_PopupMenu.IsItemChecked(index);
					_Collision_PopupMenu.SetItemChecked(index, _CollisionTerrain);
					break;
				case 1: // Fluids
					_CollisionFluids = !_Collision_PopupMenu.IsItemChecked(index);
					_Collision_PopupMenu.SetItemChecked(index, _CollisionFluids);
					break;
				case 2: // PartsBlock
					_CollisionBGParts = !_Collision_PopupMenu.IsItemChecked(index);
					_Collision_PopupMenu.SetItemChecked(index, _CollisionBGParts);
					break;
				case 3: // Floor
					_CollisionFloor = !_Collision_PopupMenu.IsItemChecked(index);
					_Collision_PopupMenu.SetItemChecked(index, _CollisionFloor);
					break;
			}
		}

		public void _On_BGPartsPlacementDirection_OptionButton_ItemSelected(int index)
		{
			if (index <= 0)
				_BGPartsPlacementDirection = 4;
			else
				_BGPartsPlacementDirection = (byte)(index - 1);
		}
		public void _On_BGPartsCollisionEnabled_Toggled(bool toggledOn)
		{
			_BGPartsCollisionEnabled = toggledOn;
		}
		public void _On_BGPartsEffectsEnabled_Toggled(bool toggledOn)
		{
			_BGPartsEffectsEnabled = toggledOn;
		}
		public void _On_BGPartsBlock_OptionButton_ItemSelected(int index)
		{
			int id = _BGPartsBlock_OptionButton.GetItemId(index);
			int partsType = id - 1;
			if (Enum.IsDefined(typeof(PartsType), partsType))
			{
				_BGPartsBlock = (PartsType)partsType;
			}
			else
			{
				_BGPartsBlock = null;
			}
		}

		public void _On_FluidLevel_OptionButton_ItemSelected(int index)
		{
			int id = _FluidLevel_OptionButton.GetItemId(index);
			_FluidLevel = id;
		}
		
		public void _On_SelectArea_Mode_OptionButton_ItemSelected(int index)
		{
			_SelectArea_Mode = index;
		}
		public void _On_Paste_Air_CheckBox_Toggled(bool toggledOn)
		{
			_Paste_Air = toggledOn;
		}

		public void _On_ReplaceWindow_Button_Apply_Pressed()
		{
			int replace = _ReplaceWindow_OptionButton_Replace.GetItemId(_ReplaceWindow_OptionButton_Replace.Selected);
			int with = _ReplaceWindow_OptionButton_With.GetItemId(_ReplaceWindow_OptionButton_With.Selected);

			if (_ReplaceWindow_CheckBox_InSelection.ButtonPressed && _AreaSelectionStart is Vector3I from)
			{
				Vector3I to = _AreaSelectionEnd ?? from;
				_WorldHandler?.ReplaceBlock(replace, with, from, to);
			}
			else
			{
				_WorldHandler?.ReplaceBlock(replace, with);
			}
		}
		#endregion
		
		#region Scene setup
		public void LoadWorldData(WorldData stageDataDQB1)
        {
			WorldHandlerDQB1 worldHandler;
            if (_WorldHandler is WorldHandlerDQB1)
            {
                worldHandler = _WorldHandler as WorldHandlerDQB1;
            }
			else
            {
                worldHandler = new WorldHandlerDQB1(this);
				_WorldHandler = worldHandler;
				ResetCamera();
            }

			worldHandler.LoadWorldData(stageDataDQB1);
			TranslateTerrain(new Vector3(-768, 0, -768));
			//ResetCamera();
			
			_UpdateMenuButtons();
        }
		public void UnloadWorldData()
        {
            if (_WorldHandler is WorldHandlerDQB1 worldHandler)
            {
                worldHandler.UnloadWorldData();
				UnselectArea();
			
				_UpdateMenuButtons();
            }
        }
		
		public void LoadParamData(ParamData paramData)
		{
			WorldHandlerDQB1 worldHandler;
            if (_WorldHandler is WorldHandlerDQB1)
            {
                worldHandler = _WorldHandler as WorldHandlerDQB1;
            }
			else
            {
                worldHandler = new WorldHandlerDQB1(this);
				_WorldHandler = worldHandler;
				ResetCamera();
            }

			worldHandler.LoadParamData(paramData);
			TranslateTerrain(new Vector3(-768, 0, -768));
			//ResetCamera();
			
			_UpdateMenuButtons();
		}
		public void UnloadParamData()
		{
            if (_WorldHandler is WorldHandlerDQB1 worldHandler)
            {
                worldHandler.UnloadParamData();
			
				_UpdateMenuButtons();
            }
		}

		public void LoadStageData(StageData stageData)
        {
			WorldHandlerDQB2 worldHandler;
            if (_WorldHandler is WorldHandlerDQB2)
            {
                worldHandler = _WorldHandler as WorldHandlerDQB2;
            }
			else
            {
                worldHandler = new WorldHandlerDQB2(this);
				_WorldHandler = worldHandler;
				ResetCamera();
            }

			worldHandler.LoadStageData(stageData);
			TranslateTerrain(new Vector3(-1024, 0, -1024));
			//ResetCamera();
			
			_UpdateMenuButtons();
        }
		public void UnloadStageData()
        {
            if (_WorldHandler is WorldHandlerDQB2 worldHandler)
            {
                worldHandler.UnloadStageData();
				UnselectArea();
			
				_UpdateMenuButtons();
            }
        }
		
		public void LoadCommonData(CommonData commonData)
        {
			WorldHandlerDQB2 worldHandler;
            if (_WorldHandler is WorldHandlerDQB2)
            {
                worldHandler = _WorldHandler as WorldHandlerDQB2;
            }
			else
            {
                worldHandler = new WorldHandlerDQB2(this);
				_WorldHandler = worldHandler;
            }

			worldHandler.LoadCommonData(commonData);
			
			_UpdateMenuButtons();
        }
		public void UnloadCommonData()
        {
            if (_WorldHandler is WorldHandlerDQB2 worldHandler)
            {
                worldHandler.UnloadCommonData();
				UnselectArea();
			
				_UpdateMenuButtons();
            }
        }
		
		public void LoadEyeOfRubissStructure(EyeOfRubissStructure structure)
		{
			if (_WorldHandler is WorldHandlerEyeOfRubissStructure)
			{
				(_WorldHandler as WorldHandlerEyeOfRubissStructure).Load(structure);
			}
			else
			{
				WorldHandlerEyeOfRubissStructure worldHandler = new WorldHandlerEyeOfRubissStructure(this);
				_WorldHandler = worldHandler;
				worldHandler.Load(structure);
				
				ResetCamera();
			}

			TranslateTerrain(Vector3.Zero);
			
			_UpdateMenuButtons();
		}
		public void UnloadEyeOfRubissStructure()
		{
            if (_WorldHandler is WorldHandlerEyeOfRubissStructure worldHandler)
            {
                worldHandler.Unload();
				UnselectArea();
			
				_UpdateMenuButtons();
            }
		}

		public void Reload()
        {
            _WorldHandler?.Reload();
        }
		
		public void TranslateTerrain(Vector3 position)
		{
			_VoxelTerrain.Position = position;
			_VoxelTerrain_PropShells.Position = position;
			_BGPartsGridManager.Position = position;
		}
		#endregion

		#region Display options
		public bool ShowFps { get; set; } = true;
		public void ChangeFPSDisplay(bool show)
		{
			ShowFps = show;
			_FPSLabel.Visible = show;
		}
		
		public bool ShowDebugInfo { get; set; } = true;
		public void ChangeDebugInfoDisplay(bool show)
		{
			ShowDebugInfo = show;
			_DebugInfoContainer.Visible = show;
		}
		
		public bool ShowTerrain { get; set; } = true;
		public void ChangeTerrainDisplay(bool show)
		{
			ShowTerrain = show;
			_WorldHandler?.OnTerrainDisplayChanged(show);
		}
		
		public bool ShowFluids { get; set; } = true;
		public void ChangeFluidsDisplay(bool show)
		{
			ShowFluids = show;
			_WorldHandler?.OnFluidsDisplayChanged(show);
		}
		
		public bool ShowPropShells { get; set; } = false;
		public void ChangePartsBlockDisplay(bool show)
		{
			ShowPropShells = show;
			_VoxelTerrain_PropShells.Visible = show;
			_WorldHandler?.OnPartsBlockDisplayChanged(show);
		}
		
		public bool ShowBGParts { get; set; } = true;
		public void ChangeBGPartsDisplay(bool show)
		{
			ShowBGParts = show;
			_BGPartsGridManager.Visible = show;
			_WorldHandler?.OnBGPartsDisplayChanged(show);
		}
		
		public bool ShowPlayer { get; set; } = true;
		public void ChangePlayerDisplay(bool show)
		{
			ShowPlayer = show;
			_PlayerDisplay.Visible = show;
			_WorldHandler?.OnPlayerDisplayChanged(show);
		}
		
		public bool ShowNPCs { get; set; } = true;
		public void ChangeNPCDisplay(bool show)
		{
			ShowNPCs = show;
			_ResidentLayer.Visible = show;
			_WorldHandler?.OnNPCDisplayChanged(show);
		}
		#endregion

		#region Brush methods
		public enum BrushType : int
		{
			None = -1,
			Pointer = 8,
			Erase = 0,
			Pencil = 1,
			Fill = 2,
			Swap = 3,
			Eyedropper = 5,
			PropChecker = 6, // deprecated
			PropMaker = 7, // deprecated
			Chisel = 9,
			SelectArea = 10,
			Paste = 11
		}

        public BrushType BrushPrimary = BrushType.Pencil;
        public BrushType BrushSecondary = BrushType.None;
        public BrushType BrushTertiary = BrushType.Eyedropper;

		public enum BrushObjectModeEnum : int
		{
			None = -1,
			Block = 0,
			BGParts = 1,
			Fluid = 2
		}
		public BrushObjectModeEnum BrushObjectMode = BrushObjectModeEnum.Block;
		public int BrushObject = 1;

		private byte _BGPartsPlacementDirection = byte.MaxValue;
		private bool _BGPartsCollisionEnabled = true;
		private bool _BGPartsEffectsEnabled = true;
		private PartsType? _BGPartsBlock = null;

		private int _FluidLevel = (int)FluidLevel.Full;

		private int _SelectArea_Mode = 0;

		private bool _Paste_Air = false;

		public void SetBrushPrimary(BrushType brush)
		{
			BrushPrimary = brush;
		}
		public void SetBrushPrimary(int brush)
		{
			SetBrushPrimary((BrushType)brush);
		}

		public void SetBrushBlock(int block)
		{
			BrushObjectMode = BrushObjectModeEnum.Block;
			BrushObject = block;
		}
		public void SetBrushBGParts(int bgParts)
		{
			BrushObjectMode = BrushObjectModeEnum.BGParts;
			BrushObject = bgParts;
		}
		public void SetBrushFluid(int fluid)
		{
			BrushObjectMode = BrushObjectModeEnum.Fluid;
			BrushObject = fluid;
		}

		public void DoBrush(VoxelRaycastResult result, BrushType brush)
		{
			if (result is null || _WorldHandler is null)
				return;

			switch (brush)
			{
				case BrushType.None:
					break;

				case BrushType.Erase:
					_WorldHandler?.DoEraser(result.Position);
					break;

                case BrushType.Pencil:
                    DoPencil(result.PreviousPosition);
                    break;

                case BrushType.Swap:
					DoPencil(result.Position);
					break;
				
				case BrushType.Paste:
					_WorldHandler?.DoPaste(result.PreviousPosition, Clipboard, _Paste_Air);
					break;

				case BrushType.Eyedropper:
					_WorldHandler?.DoEyedropper(result.Position);
					break;
				
				case BrushType.SelectArea:
					DoSelectArea(result.Position);
					break;
			}

			UpdatePointedVoxel(true);
		}

        private void DoPencil(Vector3I position)
        {
            switch (BrushObjectMode)
            {
                case BrushObjectModeEnum.Block:
                    _WorldHandler?.DoSetBlock(position, BrushObject);
                    break;
                case BrushObjectModeEnum.BGParts:
                    _WorldHandler?.DoSetBGParts(position, BrushObject, partsBlock: _BGPartsBlock, collision: _BGPartsCollisionEnabled, effects: _BGPartsEffectsEnabled);
                    break;
                case BrushObjectModeEnum.Fluid:
                    _WorldHandler?.DoSetFluid(position, BrushObject, _FluidLevel);
                    break;
            }
        }

        public void DoSelectArea(Vector3I position)
		{
			if (_SelectArea_Mode == 1)
			{
				if (_AreaSelectionStart is Vector3I start)
				{
					Vector3I end = _AreaSelectionEnd ?? start;
					Vector3I min = position.Min(start).Min(end);
					Vector3I max = position.Max(start).Max(end);
					_AreaSelectionStart = min;
					_AreaSelectionEnd = max;
					ShowBoundaryBox(min, max - min + Vector3I.One);
				}
				else
				{
					_AreaSelectionStart = position;
					_AreaSelectionEnd = null;
					ShowBoundaryBox(position);
				}
			}
			else
			{
				if (_AreaSelectionEnd is null && _AreaSelectionStart is Vector3I ass)
				{
					Vector3I start = ass.Min(position);
					Vector3I end = ass.Max(position);
					_AreaSelectionStart = start;
					_AreaSelectionEnd = end;
					ShowBoundaryBox(start, end - start + Vector3I.One);
				}
				else
				{
					_AreaSelectionStart = position;
					_AreaSelectionEnd = null;
					ShowBoundaryBox(position);
				}
			}

			_UpdateMenuButtons();
		}
		public void UnselectArea()
		{
			_AreaSelectionStart = null;
			_AreaSelectionEnd = null;
			HideBoundaryBox();

			_UpdateMenuButtons();
		}
		public void DeleteSelection()
		{
			if (_WorldHandler is null)
				return;
			if (_AreaSelectionStart is not Vector3I start)
				return;
			if (_AreaSelectionEnd is not Vector3I end)
				end = start;
			
			for (int x = start.X; x <= end.X; x++)
			{
				for (int y = start.Y; y <= end.Y; y++)
				{
					for (int z = start.Z; z <= end.Z; z++)
					{
						_WorldHandler.DoEraser(new Vector3I(x, y, z));
					}
				}
			}

			UnselectArea();
		}
		public void FillSelection()
		{
			if (_AreaSelectionStart is not Vector3I start)
				return;
			if (_AreaSelectionEnd is not Vector3I end)
				end = start;
			
			for (int x = start.X; x <= end.X; x++)
			{
				for (int y = start.Y; y <= end.Y; y++)
				{
					for (int z = start.Z; z <= end.Z; z++)
					{
						DoPencil(new Vector3I(x, y, z));
					}
				}
			}
		}
		#endregion
		
		#region Selection box
		public void ShowSelectionBox(Vector3 position, Vector3I? size = null)
        {
            _SelectionBox.Show();
			_SelectionBox.Position = position + _VoxelTerrain.Position;
			_SelectionBox.SetSize(size ?? Vector3.One);
        }
		public void HideSelectionBox()
        {
            _SelectionBox.Hide();
        }
		
		public void ShowBoundaryBox(Vector3 position, Vector3? size = null)
		{
			_BoundaryBox.Show();
			_BoundaryBox.Position = position + _VoxelTerrain.Position;
			_BoundaryBox.SetSize(size ?? Vector3.One);
		}
		public void HideBoundaryBox()
		{
			_BoundaryBox.Hide();
		}
		#endregion

		#region Gizmo functions
		public void _On_Gizmo3D_TransformEnd()
		{
			_WorldHandler?.OnGizmo3DTransformEnd();
		}
		#endregion

		#region Copy and paste
		public EyeOfRubissStructure Clipboard;

		public void DoCopy()
		{
			if (_AreaSelectionStart is not Vector3I start)
				return;
			if (_AreaSelectionEnd is not Vector3I end)
				end = start;
			
			Vector3I size = end - start + Vector3I.One;

			if (_WorldHandler is not null)
			{
				Clipboard = _WorldHandler.DoCopy(start, end);
				Clipboard.SizeX = size.X;
				Clipboard.SizeY = size.Y;
				Clipboard.SizeZ = size.Z;
				
				_PasteButton.Disabled = false;
			}
		}
		public void DoCut()
		{
			DoCopy();
			DeleteSelection();
		}
		
		public void ExportSelection(string path)
		{
			if (_AreaSelectionStart is not Vector3I start)
				return;
			if (_AreaSelectionEnd is not Vector3I end)
				end = start;
			
			if (_WorldHandler is not null)
			{
				EyeOfRubissStructure structure = _WorldHandler.DoCopy(start, end);

				if (structure is not null)
				{
					if (path.ToLower().EndsWith(".json"))
					{
						structure.Save(path);
					}
					else
					{
						BlueprintFileDQB2 blueprintFile = structure.ToBlueprint();
						blueprintFile.Save(path);
					}	
				}
			}
		}
		#endregion
	}
}