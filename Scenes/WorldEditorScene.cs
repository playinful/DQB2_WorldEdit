using EyeOfRubiss.Info;
using EyeOfRubiss.Nodes;
using Gizmo3DPlugin;
using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
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
		[Export] public PropGridHacky _BGPartsGrid;
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

		[Export] public Button _ChiselButton;
		[Export] public Button _PasteButton;

		[ExportGroup("Settings")]
		[Export] public bool AutomaticallyGenerateBedrock = true;

		private WorldHandler _WorldHandler;

		public bool ShowDebugInfo { get; set; } = true;
		public bool ShowFps { get; set; } = true;
		public bool ShowTerrain { get; set; } = true;
		public bool ShowPropShells { get; set; } = false;
		public bool ShowBGParts { get; set; } = true;
		public bool ShowNPCs { get; set; } = true;
		public bool ShowPlayer { get; set; } = true;

		private NPCSprite _SelectedNPCSprite;

		private Vector3I? _AreaSelectionStart;
		private Vector3I? _AreaSelectionEnd;

		public override void _Ready()
		{
			_VoxelTool = _VoxelTerrain.GetVoxelTool();
			_VoxelTool_PropShells = _VoxelTerrain_PropShells.GetVoxelTool();

			_UpdateMenuButtons();
		}

		public override void _Process(double delta)
		{
			
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

			_PasteButton.Disabled = Clipboard is null;

			_ChiselButton.Disabled = (_WorldHandler is not WorldHandlerDQB2) && (_WorldHandler is not WorldHandlerBlueprintDQB2);
		}

		public VoxelRaycastResult GetPointedVoxel()
		{
			Vector3 origin = _CameraController.GlobalTransform.Origin;
			Vector3 forward = (Input.MouseMode == Input.MouseModeEnum.Captured) ?
				-_CameraController.Transform.Basis.Z.Normalized() : // Cast directly forward from camera if mouse is captured
				_CameraController.ProjectRayNormal(GetViewport().GetMousePosition()); // Otherwise cast by mouse position
			
			VoxelRaycastResult hit = _VoxelTool.Raycast(origin, forward, 4096);
			return hit;
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
		private void UpdatePointedVoxel()
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
					if (Clipboard is not null)
						ShowSelectionBox(result.PreviousPosition, Clipboard.Size);
					else
						ShowSelectionBox(result.PreviousPosition);
				}
				else
				{
					ShowSelectionBox(result.Position);
				}

				if (_LastPointedVoxel == result.Position)
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



		#region Input
		public override void _UnhandledInput(InputEvent @event)
		{
			if (_Gizmo.Editing)
				return;

			if (@event.IsActionPressed(Constants.Controls.RESET_CAMERA))
            {
                ResetCamera();
            }

			if (_WorldHandler is null)
				return;

			if (@event.IsActionPressed(Constants.Controls.BRUSH_PRIMARY))
				DoBrush(GetPointedVoxel(), BrushPrimary);
			if (@event.IsActionPressed(Constants.Controls.BRUSH_SECONDARY))
				DoBrush(GetPointedVoxel(), BrushSecondary);
			if (@event.IsActionPressed(Constants.Controls.BRUSH_TERTIARY))
				DoBrush(GetPointedVoxel(), BrushTertiary);
			
			if (Input.IsActionPressed(Constants.Controls.DELETE))
				DeleteSelection();
			if (Input.IsActionPressed(Constants.Controls.KEYBOARD_SHORTCUT_COPY))
				DoCopy();
			if (Input.IsActionPressed(Constants.Controls.KEYBOARD_SHORTCUT_CUT))
				DoCut();

            if (@event.IsActionPressed(Constants.Controls.KEYBOARD_SHORTCUT_FILL))
				FillSelection();
		}

		public void ResetCamera()
        {
			if (_WorldHandler is WorldHandlerDQB1)
			{
				_CameraController.Position = Vector3.Up * 32;
				_CameraController.Rotation = Vector3.Zero;
			}
			else if (_WorldHandler is WorldHandlerBlueprintAssetDQB1)
			{
				_CameraController.Position = new Vector3(16, 8, -8);
				_CameraController.Rotation = Vector3.Up * Mathf.Pi;
			}
			else if (_WorldHandler is WorldHandlerDioramaAssetDQB1)
			{
				_CameraController.Position = new Vector3(16, 8, -8);
				_CameraController.Rotation = Vector3.Up * Mathf.Pi;
			}
			else if (_WorldHandler is WorldHandlerDQB2)
			{
				_CameraController.Position = Vector3.Up * 96;
				_CameraController.Rotation = Vector3.Zero;
			}
			else if (_WorldHandler is WorldHandlerBlueprintDQB2)
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
		
		public byte GetBGPartsPlacementDirection()
        {
			int rotation = (int)Math.Round(_CameraController.Rotation.Y / (Math.PI / 2)) % 4;
			if (rotation < 0)
				rotation += 4;
			return (byte)rotation;
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
		
		public void LoadBlueprintAssetDQB1(BlueprintAssetDQB1 blueprint)
		{
			WorldHandlerBlueprintAssetDQB1 worldHandler;
            if (_WorldHandler is WorldHandlerBlueprintAssetDQB1)
            {
                worldHandler = _WorldHandler as WorldHandlerBlueprintAssetDQB1;
            }
			else
            {
                worldHandler = new WorldHandlerBlueprintAssetDQB1(this);
				_WorldHandler = worldHandler;
				ResetCamera();
            }

			worldHandler.Load(blueprint);
			TranslateTerrain(Vector3.Zero);
			
			_UpdateMenuButtons();
		}
		public void UnloadBlueprintAssetDQB1()
		{
            if (_WorldHandler is WorldHandlerBlueprintAssetDQB1 worldHandler)
            {
                worldHandler.Unload();
				UnselectArea();
			
				_UpdateMenuButtons();
            }
		}

		public void LoadDioramaHeaderAssetDQB1(DioramaHeaderAssetDQB1 header)
		{
            if (_WorldHandler is not WorldHandlerDioramaAssetDQB1 worldHandler)
            {
                worldHandler = new WorldHandlerDioramaAssetDQB1(this);
				_WorldHandler = worldHandler;
				ResetCamera();
            }

			worldHandler.LoadHeader(header);
			TranslateTerrain(Vector3.Zero);
			
			_UpdateMenuButtons();
		}
		public void UnloadDioramaHeaderAssetDQB1()
		{
			if (_WorldHandler is WorldHandlerDioramaAssetDQB1 worldHandler)
			{
				worldHandler.UnloadHeader();
				UnselectArea();
			
				_UpdateMenuButtons();
			}
		}

		public void LoadDioramaDataAssetDQB1(DioramaDataAssetDQB1 data)
		{
            if (_WorldHandler is not WorldHandlerDioramaAssetDQB1 worldHandler)
            {
                worldHandler = new WorldHandlerDioramaAssetDQB1(this);
				_WorldHandler = worldHandler;
				ResetCamera();
            }

			worldHandler.LoadData(data);
			TranslateTerrain(Vector3.Zero);
			
			_UpdateMenuButtons();
		}
		public void UnloadDioramaDataAssetDQB1()
		{
			if (_WorldHandler is WorldHandlerDioramaAssetDQB1 worldHandler)
			{
				worldHandler.UnloadData();
				UnselectArea();
			
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
		
		public void LoadBlueprintDQB2(Blueprint blueprint)
		{
			if (_WorldHandler is WorldHandlerBlueprintDQB2)
			{
				(_WorldHandler as WorldHandlerBlueprintDQB2).Load(blueprint);
			}
			else
			{
				WorldHandlerBlueprintDQB2 worldHandler = new WorldHandlerBlueprintDQB2(this);
				_WorldHandler = worldHandler;
				worldHandler.Load(blueprint);
				
				ResetCamera();
			}

			TranslateTerrain(Vector3.Zero);
			
			_UpdateMenuButtons();
		}
		public void UnloadBlueprintDQB2()
		{
            if (_WorldHandler is WorldHandlerBlueprintDQB2 worldHandler)
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
			_BGPartsGrid.Position = position;
		}
		#endregion

		#region Display options
		public void ChangeFPSDisplay(bool show)
		{
			ShowFps = show;
			_FPSLabel.Visible = show;
		}
		public void ChangeDebugInfoDisplay(bool show)
		{
			ShowDebugInfo = show;
			_DebugInfoContainer.Visible = show;
		}
		public void ChangeTerrainDisplay(bool show)
		{
			ShowTerrain = show;
			_VoxelTerrain.Visible = show;
			_WorldHandler?.OnTerrainDisplayChanged(show);
		}
		public void ChangePartsBlockDisplay(bool show)
		{
			ShowPropShells = show;
			_WorldHandler?.OnPropShellsDisplayChanged(show);
		}
		public void ChangeBGPartsDisplay(bool show)
		{
			ShowBGParts = show;
			_BGPartsGrid.Visible = show;
			_WorldHandler?.OnPropsDisplayChanged(show);
		}
		public void ChangePlayerDisplay(bool show)
		{
			ShowPlayer = show;
			_PlayerDisplay.Visible = show;
			_WorldHandler?.OnPlayerDisplayChanged(show);
		}
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
					_WorldHandler.DoEraser(result.Position);
					break;

                case BrushType.Pencil:
                    DoPencil(result.PreviousPosition);
                    break;

                case BrushType.Swap:
					switch (BrushObjectMode)
					{
						case BrushObjectModeEnum.Block:
							_WorldHandler.DoSetBlock(result.Position, BrushObject);
							break;
						case BrushObjectModeEnum.BGParts:
							_WorldHandler.DoSetBGParts(result.Position, BrushObject);
							break;
						case BrushObjectModeEnum.Fluid:
							_WorldHandler.DoSetFluid(result.Position, BrushObject);
							break;
					}
					break;

				case BrushType.Eyedropper:
					_WorldHandler.DoEyedropper(result.Position);
					break;
				
				case BrushType.SelectArea:
					DoSelectArea(result.Position);
					break;
			}
		}

        private void DoPencil(Vector3I position)
        {
            switch (BrushObjectMode)
            {
                case BrushObjectModeEnum.Block:
                    _WorldHandler.DoSetBlock(position, BrushObject);
                    break;
                case BrushObjectModeEnum.BGParts:
                    _WorldHandler.DoSetBGParts(position, BrushObject);
                    break;
                case BrushObjectModeEnum.Fluid:
                    _WorldHandler.DoSetFluid(position, BrushObject);
                    break;
            }
        }

        public void DoSelectArea(Vector3I position)
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
		public CopiedDataObject Clipboard;

		public void DoCopy()
		{
			if (_AreaSelectionStart is not Vector3I start)
				return;
			if (_AreaSelectionEnd is not Vector3I end)
				end = start;
			
			Vector3I size = end - start + Vector3I.One;

			Clipboard = new CopiedDataObject(size);
		}
		public void DoCut()
		{
			DoCopy();
			DeleteSelection();
		}

		public class CopiedDataObject(Vector3I size)
		{
			public int DQB1orDQB2Source;

			public Vector3I Size = size;

			public int[] Blocks;
		}
		#endregion
	}
}