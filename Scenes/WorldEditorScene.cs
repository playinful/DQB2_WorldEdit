using EyeOfRubiss.Info;
using EyeOfRubiss.Nodes;
using Gizmo3DPlugin;
using Godot;
using System;
using System.Collections.Generic;
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
		[Export] private VoxelTerrain _VoxelTerrain;
		private VoxelTool _VoxelTool;
		[Export] private VoxelTerrain _VoxelTerrain_PropShells;
		private VoxelTool _VoxelTool_PropShells;
		[Export] private Node3D _ResidentLayer;
		[Export] private PropGridHacky _PropGrid;
		[Export] private NPCSprite _PlayerDisplay;

		[Export] private CameraController _CameraController;

		[Export] private Gizmo3D _Gizmo;

		[Export] private CanvasItem _DebugInfoContainer;
		[Export] private FPSLabel _FPSLabel;
		[Export] private Label _PointedVoxelLabel;
		[Export] private StatusLabel _StatusLabel;
		[Export] private LineEdit _CommandParser;
		[Export] private AnimationPlayer _LoadingAnimationPlayer;

		[ExportGroup("Settings")]
		[Export] public bool AutomaticallyGenerateBedrock = true;

		[Export] public BrushType BrushPrimary = BrushType.Pencil;
		[Export] public BrushType BrushSecondary = BrushType.Erase;
		[Export] public BrushType BrushTertiary = BrushType.Eyedropper;
		[Export] public ushort BrushBlock = 1;
		[Export] public ushort BrushProp = 1;

		private StageData _StageData;

		public bool ShowDebugInfo { get; set; } = true;
		public bool ShowFps { get; set; } = true;
		public bool ShowTerrain { get; set; } = true;
		public bool ShowPropShells { get; set; } = false;
		public bool ShowProps { get; set; } = true;
		public bool ShowNPCs { get; set; } = true;
		public bool ShowPlayer { get; set; } = true;

		private bool _Loaded = false;
		private Thread _LoadingThread;
		private bool _PropsLoaded = false;

		private NPCSprite _SelectedNPCSprite;

		public override void _Ready()
		{
			_VoxelTool = _VoxelTerrain.GetVoxelTool();
			_VoxelTool_PropShells = _VoxelTerrain_PropShells.GetVoxelTool();
		}

		public override void _Process(double delta)
		{

		}
		public override void _PhysicsProcess(double delta)
		{
			UpdatePointedVoxelLabel();
		}

		public VoxelRaycastResult GetPointedVoxel()
		{
			if (!_Loaded)
				return null;

			Vector3 origin = _CameraController.GlobalTransform.Origin;
			Vector3 forward = (Input.MouseMode == Input.MouseModeEnum.Captured) ?
				-_CameraController.Transform.Basis.Z.Normalized() : // Cast directly forward from camera if mouse is captured
				_CameraController.ProjectRayNormal(GetViewport().GetMousePosition()); // Otherwise cast by mouse position
			
			// Old code
			//Vector3 origin = _CameraController.GlobalTransform.Origin;
			//Vector3 forward = -_CameraController.Transform.Basis.Z.Normalized();
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
		private void UpdatePointedVoxelLabel()
		{
			if (_PointedVoxelLabel is null)
				return;

			VoxelRaycastResult result = GetPointedVoxel();
			if (result is not null)
			{
				Vector3I friendlyPos = result.Position;// - new Vector3I(1024, 0, 1024);

				//Vector3I indexPos = StageData.Instance.EuclidPosToIndex(result.Position);

				StageData.BlockInstance block = _StageData.GetBlockAtPosition(result.Position);

				_PointedVoxelLabel.Text =
					$"Targeted block: {(block is not null ? BlockInfo.Get(block.BlockID).Name + $" [{block.BlockID}]" : "UNKNOWN")}\n" +
					$"X: {friendlyPos.X}, Y: {friendlyPos.Y}, Z: {friendlyPos.Z}\n" +
					//$"Chunk: {indexPos.X}, Layer: {indexPos.Y}, Tile: {indexPos.Z}\n" +
					$"Placed by Builder: {block.PlayerPlaced}" +
					$"\nShape: {block.Chisel}";
			}
			else
			{
				_PointedVoxelLabel.Text = "Targeted block: None";
			}
		}



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
		public void ChangeNPCDisplay(bool show)
		{
			ShowNPCs = show;
			_ResidentLayer.Visible = show;
		}
		public void ChangeTerrainDisplay(bool show)
		{
			ShowTerrain = show;
			_VoxelTerrain.Stream = (show && _StageData is not null) ? new VoxelStreamDQB2(_StageData) : null;
			//_VoxelTerrain.Visible = show;
		}
		public void ChangePlayerDisplay(bool show)
		{
			ShowPlayer = show;
			_PlayerDisplay.Visible = show && _StageData is not null && _StageData.IsLoaded && CommonData.HasInstance() && _StageData.IslandID == CommonData.Instance.ToIsland;
		}
		public void ChangePropDisplay(bool show)
		{
			ShowProps = show;
			_PropGrid.Visible = show;
			if (show && _StageData is not null && !_PropsLoaded)
				CreateProps(_StageData);
		}
		public void ChangePropShellDisplay(bool show)
		{
			ShowPropShells = show;
			_VoxelTerrain_PropShells.Stream = (show && _StageData is not null) ? new VoxelStreamDQB2(_StageData, propsOnly: true) : null;
		}
		#endregion

		#region Scene setup
		public void LoadWorld(StageData stageData)
		{
			if (_LoadingThread is not null && _LoadingThread.IsAlive)
			{
				//_LoadingThread.Join();
				return;
			}

			UnloadWorld();

			LoadWorldThreaded(stageData); // TODO make this actually threaded
			//_LoadingThread = new Thread(() => LoadWorldThreaded(stageData));
			//_LoadingThread.Start();

			_LoadingAnimationPlayer.Play("Loading");

			/*
			_StageData = stageData;

			if (ShowTerrain)
				_VoxelTerrain.Stream = new VoxelStreamDQB2(stageData);
			if (ShowPropShells)
				_VoxelTerrain_PropShells.Stream = new VoxelStreamDQB2(stageData, propsOnly: true);
			if (ShowProps)
				CreateProps(stageData);

			if (CommonData.Instance is not null && CommonData.Instance.IsLoaded)
				LoadCommonData(CommonData.Instance);
			*/
		}
		public void UnloadWorld()
		{
			_Loaded = false;
			_VoxelTerrain.Stream = null;
			_VoxelTerrain_PropShells.Stream = null;
			_Gizmo.ClearSelection();
			DestroyProps();
			DestroyResidents();
			_PlayerDisplay.Hide();
			_StageData = null;
		}

		private void LoadWorldThreaded(StageData stageData)
		{
			_StageData = stageData;

			if (ShowProps)
			{
				CreateProps(stageData);
			}
			if (ShowPropShells)
			{
				_VoxelTerrain_PropShells.Stream = new VoxelStreamDQB2(stageData, propsOnly: true);
			}
			if (ShowTerrain)
			{
				_VoxelTerrain.SetDeferred(VoxelNode.PropertyName.Stream, new VoxelStreamDQB2(stageData));
			}

			if (CommonData.Instance is not null && CommonData.Instance.IsLoaded)
			{
				LoadCommonData(CommonData.Instance);
			}

			_Loaded = true;
			_LoadingAnimationPlayer?.CallDeferred(AnimationPlayer.MethodName.Play, "RESET");
		}

		public void LoadCommonData(CommonData commonData)
		{
			if (_StageData is null || !_StageData.IsLoaded)
				return;

			CreateResidents(commonData);

			_PlayerDisplay.SetNPCName(CommonData.Instance.PlayerName);
			_PlayerDisplay.Position = commonData.GetPlayerPosition();
			_PlayerDisplay.Rotation = Vector3.Up * commonData.PlayerRotation;
			_PlayerDisplay.Visible = ShowPlayer && commonData.ToIsland == _StageData.IslandID;
		}
		public void UnloadCommonData()
		{
			DestroyResidents();
			_PlayerDisplay.Hide();
		}

		public void CreateProps(StageData stageData)
		{
			foreach (StageData.Prop prop in stageData.GetProps())
			{
				if (prop.Exists() && prop.GetInfo().MeshID is int meshId)
				{
					_PropGrid.SetCellItemDelegated(prop.GetPosition(), meshId, prop.GetGridMapRotation());
				}
			}
			_PropsLoaded = true;
		}
		public void DestroyProps()
		{
			_PropGrid.Clear();
			_PropGrid.ClearSubGrid();
			_PropsLoaded = false;
		}

		public void CreateResident(CommonData.Resident resident)
		{
			/*
			Sprite3D sprite3D = new Sprite3D();
			GetNode("NPCSpriteLayer").AddChild(sprite3D);
			sprite3D.Texture = ResourceLoader.Load<Texture2D>("res://Graphics/Resident/monster_hammerhood.png");
			sprite3D.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
			sprite3D.FixedSize = true;
			sprite3D.PixelSize = 0.001f;
			sprite3D.Position = new Vector3(resident.PositionX, resident.PositionY + 0.5f, resident.PositionZ);

			Label3D label3D = new Label3D();
			label3D.Text = resident.GetDisplayName();
			sprite3D.AddChild(label3D);
			label3D.Position += Vector3.Up;
			label3D.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
			label3D.FixedSize = true;
			label3D.PixelSize = 0.001f;
			*/
			NPCSprite npcSprite = ResourceLoader.Load<PackedScene>("res://Nodes/NPCSprite.tscn").Instantiate<NPCSprite>();
			npcSprite.SetNPC(resident);
			npcSprite.Position = new Vector3(resident.PositionX, resident.PositionY, resident.PositionZ);
			npcSprite.Rotation = Vector3.Up * resident.Rotation;
			_ResidentLayer.AddChild(npcSprite);
		}
		public void CreateResidents(CommonData commonData)
		{
			if (_StageData is null || !_StageData.IsLoaded)
				return;

			DestroyResidents();
			foreach (CommonData.Resident resident in commonData.GetResidents())
			{
				if (resident.CurrentIsland == _StageData.IslandID)
				{
					CreateResident(resident);
				}
			}
		}
		public void DestroyResidents()
		{
			_ResidentLayer.QueueFreeAllChildren();
		}

		public void Reload()
		{
			if (_StageData is null || !_StageData.IsLoaded)
				return;

			if (ShowTerrain)
				_VoxelTerrain.Stream = new VoxelStreamDQB2(_StageData);
			if (ShowPropShells)
				_VoxelTerrain_PropShells.Stream = new VoxelStreamDQB2(_StageData, propsOnly: true);
			DestroyProps();
			CreateProps(_StageData);
		}
		#endregion

		#region Control handling
		public override void _UnhandledInput(InputEvent @event)
		{
			if (_Gizmo.Editing)
				return;

			if (@event.IsActionPressed(Constants.Controls.RESET_CAMERA))
			{
				_CameraController.Position = Vector3.Up * 96;
				_CameraController.Rotation = Vector3.Zero;
			}

			if (!_Loaded)
				return;

			if (@event.IsActionPressed(Constants.Controls.BRUSH_PRIMARY))
				DoBrush(GetPointedVoxel(), BrushPrimary);
			if (@event.IsActionPressed(Constants.Controls.BRUSH_SECONDARY))
				DoBrush(GetPointedVoxel(), BrushSecondary);
			if (@event.IsActionPressed(Constants.Controls.BRUSH_TERTIARY))
				DoBrush(GetPointedVoxel(), BrushTertiary);
		}

		public void MoveCamera(Vector3 target)
		{
			_CameraController.Position = target;
		}
		#endregion

		#region Stage editing
		public void SetBlock(Vector3I position, ushort blockId, StageData.BlockInstance.ChiselType? chiselType = null, bool? playerPlaced = null, bool destroyProps = true)
		{
			if (_StageData is null || !_StageData.IsLoaded)
				return;

			bool success = _StageData.SetBlockAtPosition(position, blockId, chiselType, playerPlaced, true);
			if (success)
			{
				BlockInfo blockInfo = BlockInfo.Get(blockId);

				if (ShowTerrain)
				{
					ulong voxelId = blockInfo.VoxelID;
					_VoxelTool.SetVoxel(position, voxelId);
				}
				if (ShowPropShells)
				{
					ulong voxelId = (ulong)blockInfo.GetPropShell();
					_VoxelTerrain_PropShells.GetVoxelTool().SetVoxel(position, voxelId);
				}

				if (destroyProps && blockInfo.GetPropShell() == PropShell.None)
				{
					DeleteProp(position);
				}

				if (AutomaticallyGenerateBedrock && blockId != Constants.BLOCK_AIR && position.Y > 0)
				{
					SetBlock(new Vector3I(position.X, 0, position.Z), Constants.BLOCK_BEDROCK, chiselType, playerPlaced);
				}
			}
			else
			{
				_StatusLabel.PrintMessage("Cannot place blocks out of bounds.");
			}
		}
		public void FillCube(Vector3I from, Vector3I to, ushort blockId)
		{
			for (int x = from.X; x < to.X; x++)
			{
				for (int y = from.Y; y < to.Y; y++)
				{
					for (int z = from.Z; z < to.Z; z++)
					{
						SetBlock(new Vector3I(x, y, z), blockId);
					}
				}
			}
		}

		public void FillRecursive(Vector3I position)
		{
			ushort baseBlock = _StageData.GetBlockAtPosition(position).BlockID;
			FillRecursive(position, baseBlock);
		}
		public void FillRecursive(Vector3I position, ushort baseBlock)
		{
			if (_StageData.GetBlockAtPosition(position) is not StageData.BlockInstance blockInstance || blockInstance.BlockID != baseBlock)
				return;

			SetBlock(position, BrushBlock);

			FillRecursive(position + Vector3I.Up, baseBlock);
			FillRecursive(position + Vector3I.Down, baseBlock);
			FillRecursive(position + Vector3I.Left, baseBlock);
			FillRecursive(position + Vector3I.Right, baseBlock);
			FillRecursive(position + Vector3I.Forward, baseBlock);
			FillRecursive(position + Vector3I.Back, baseBlock);

			// TODO this breaks for some reason
		}

		public void CreateProp(Vector3I position, ushort propId)
		{
			if (_StageData is null || !_StageData.IsLoaded)
				return;

			// TODO replace with genuine prop shell
			SetBlock(position, 2047, StageData.BlockInstance.ChiselType.FullBlock, playerPlaced: false);
			_StageData.AddProp(position, propId);

			PropInfo propInfo = PropInfo.Get(propId);
			_PropGrid.SetCellItemDelegated(position, propInfo.MeshID ?? -1);
		}
		public void DeleteProp(Vector3I position)
		{
			foreach (StageData.Prop prop in _StageData.GetOverlappingProps(position))
			{
				(Vector3I start, Vector3I end) = prop.GetBounds();
				_PropGrid.ClearCellItem(prop.GetPosition());
				prop.Clear();

				for (int x = start.X; x <= end.X; x++)
				{
					for (int y = start.Y; y <= end.Y; y++)
					{
						for (int z = start.Z; z <= end.Z; z++)
						{
							Vector3I otherPosition = new(x, y, z);
							if (_StageData.GetOverlappingProp(otherPosition) is StageData.Prop otherProp)
							{
								ChangePropShell(otherPosition, prop.GetInfo().PropShell);
							}
							else
							{
								ChangePropShell(otherPosition, PropShell.None);
							}
						}
					}
				}
			}
		}

		public void ChangePropShell(Vector3I position, PropShell propShell)
		{
			// TEST, TODO
			if (propShell == PropShell.None)
			{
				SetBlock(position, Constants.BLOCK_AIR, destroyProps: false);
			}
		}

		public void Builderize()
		{
			if (_StageData is null || !_StageData.IsLoaded)
				return;

			foreach (StageData.BlockInstance block in _StageData.GetAllBlocks())
			{
				if (block.BlockID != 0)
					block.PlayerPlaced = true; // TODO: what happens if props/liquids are PlayerPlaced?
			}
		}

		public void CopyPaste(Vector3I from, Vector3I bounds, Vector3I to)
		{
			if (!StageData.HasInstance())
				return;

			for (int x = 0; x <= bounds.X; x++)
			{
				for (int y = 0; y <= bounds.Y; y++)
				{
					for (int z = 0; z <= bounds.Z; z++)
					{
						Vector3I fromPos = from + new Vector3I(x, y, z);
						Vector3I toPos = to + new Vector3I(x, y, z);
						StageData.BlockInstance fromBlock = _StageData.GetBlockAtPosition(fromPos);
						SetBlock(toPos, fromBlock.BlockID, fromBlock.Chisel, fromBlock.PlayerPlaced);
					}
				}
			}

		}
		public void MakeSuperflat(List<ushort> layers)
		{
			_StageData.MakeSuperflat(layers);
			Reload();// TODO
		}
		#endregion

		#region Brushes
		public enum BrushType : int
		{
			None = -1,
			Pointer = 8,
			Erase = 0,
			Pencil = 1,
			Fill = 2,
			Swap = 3,
			Eyedropper = 5,
			PropChecker = 6,
			PropMaker = 7
		}

		public void SetBrushPrimary(BrushType brush)
		{
			BrushPrimary = brush;
		}
		public void SetBrushPrimary(int brush)
		{
			SetBrushPrimary((BrushType)brush);
		}
		public void SetBrushBlock(ushort block)
		{
			BrushBlock = block;
		}
		public void SetBrushProp(ushort prop)
		{
			BrushProp = prop;
		}

		public void DoBrush(VoxelRaycastResult result, BrushType brush)
		{
			if (result is null)
				return;

			switch (brush)
			{
				case BrushType.Erase:
					SetBlock(result.Position, Constants.BLOCK_AIR);
					break;
				case BrushType.Pencil:
					SetBlock(result.PreviousPosition, BrushBlock);
					break;
				case BrushType.Swap:
					SetBlock(result.Position, BrushBlock);
					break;
				case BrushType.Eyedropper:
					DoEyedropper(result.Position);
					break;
				case BrushType.PropChecker:
					TEST_DoPropChecker(result.Position);
					break;
				case BrushType.Fill:
					FillRecursive(result.Position);
					break;
				case BrushType.PropMaker:
					CreateProp(result.PreviousPosition, BrushProp);
					break;
				case BrushType.Pointer:
					DoSelect();
					break;
			}
		}
		public void DoSelect()
		{
			Node3D pointedObject = GetPointedObject();
			if (pointedObject is NPCSprite npc)
			{
				SelectNPC(npc);
			}
		}
		public void DoEyedropper(Vector3I position)
		{
			if (_StageData.GetBlockAtPosition(position) is StageData.BlockInstance block)
			{
				SetBrushBlock(block.BlockID);
				GD.Print($"Set brush block to {BlockInfo.Get(block.BlockID).Name} ({block.BlockID})");
			}
		}
		#endregion

		#region Gizmo functions
		public void SelectNPC(NPCSprite npc)
		{
			_Gizmo.ClearSelection();
			_Gizmo.Select(npc);
			_SelectedNPCSprite = npc;
		}

		public void _On_Gizmo3D_TransformEnd()
		{
			if (!CommonData.HasInstance())
				return;

			if (_SelectedNPCSprite == _PlayerDisplay)
			{
				CommonData.Instance.PlayerPositionX = _SelectedNPCSprite.Position.X;
				CommonData.Instance.PlayerPositionY = _SelectedNPCSprite.Position.Y;
				CommonData.Instance.PlayerPositionZ = _SelectedNPCSprite.Position.Z;

				CommonData.Instance.PlayerRotation = _SelectedNPCSprite.Rotation.Y;
			}
			else
			{
				_SelectedNPCSprite.Resident.PositionX = _SelectedNPCSprite.Position.X;
				_SelectedNPCSprite.Resident.PositionY = _SelectedNPCSprite.Position.Y;
				_SelectedNPCSprite.Resident.PositionZ = _SelectedNPCSprite.Position.Z;

				_SelectedNPCSprite.Resident.Rotation = _SelectedNPCSprite.Rotation.Y;
			}
		}
		#endregion

		#region Debug functions
		public void CountBlocks(string path)
		{
			// TODO
		}
		public void CountProps(string path)
		{
			Dictionary<ushort, int> propCounts = [];

			foreach (StageData.Prop prop in _StageData.GetProps())
			{
				if (prop.PropID == 0)
					continue;

				if (propCounts.ContainsKey(prop.PropID))
					propCounts[prop.PropID]++;
				else
					propCounts[prop.PropID] = 1;
			}

			List<string> outlines = [];
			foreach ((ushort id, int count) in propCounts)
			{
				outlines.Add($"{PropInfo.Get(id).Name} [{id}] x {count}");
			}

			using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write);
			file.StoreString(string.Join('\n', outlines.Order()));

			_StatusLabel.PrintMessage($"Wrote prop counts to {path}.");
		}
		public void FindProp(ushort propId)
		{
			if (_StageData.GetProps().FirstOrDefault(prop => prop.PropID == propId) is StageData.Prop prop)
				_StatusLabel.PrintMessage($"Found prop {prop.GetInfo().Name} [{propId}] at ({prop.GetPosition()}).");
			else
				_StatusLabel.PrintMessage($"Could not find prop {PropInfo.Get(propId).Name} [{propId}].");
		}

		public void TEST_DoPropChecker(Vector3I position)
		{
			IEnumerable<StageData.Prop> props = _StageData.GetOverlappingProps(position);
			if (!props.Any())
			{
				_StatusLabel.PrintMessage($"No prop found at ({position})");
				return;
			}

			foreach (StageData.Prop prop in props)
			{
				_StatusLabel.PrintMessage(
					$"Prop {prop.DataIndex} @ [{prop.GetAddress()}] at position ({position}): {prop.GetInfo().Name} [{prop.PropID}] | Rotation: {(prop.Rotation == 0 ? "North" : (prop.Rotation == 1 ? "West" : (prop.Rotation == 2 ? "South" : "East")))}\n" +
					$"Prop position: ({prop.GetPosition()}) | Prop bounds: ({prop.GetBounds().Item1}) - ({prop.GetBounds().Item2})"
				);

				DisplayServer.ClipboardSet(Convert.ToHexString(prop.GetBytes()));
				Window propEditor = GetNode<Window>("%BasicPropEditor");

				if (!propEditor.Visible)
				{
					//ReleaseCursor();
					//propEditor.Show();
				}

				propEditor.GetNode<SpinBox>("VBoxContainer/HBoxContainer1/SpinBox").SetValueNoSignal(prop.PropID);
				propEditor.GetNode<SpinBox>("VBoxContainer/HBoxContainer1/SpinBox2").SetValueNoSignal(prop.Rotation);
				propEditor.GetNode<SpinBox>("VBoxContainer/HBoxContainer2/SpinBoxX").SetValueNoSignal(position.X);
				propEditor.GetNode<SpinBox>("VBoxContainer/HBoxContainer2/SpinBoxY").SetValueNoSignal(position.Y);
				propEditor.GetNode<SpinBox>("VBoxContainer/HBoxContainer2/SpinBoxZ").SetValueNoSignal(position.Z);

				_TEST_selectedprop = prop;
			}
			// old
			/*if (_StageData.GetPropAtPosition(position) is StageData.Prop prop)
			{
				_StatusLabel.PrintMessage($"Prop {prop.DataIndex} @ [{prop.GetAddress()}] at position ({position}): {prop.GetInfo().Name} [{prop.PropID}] | Rotation: {(prop.Rotation == 0 ? "North" : (prop.Rotation == 1 ? "West" : (prop.Rotation == 2 ? "South" : "East")))}");
				DisplayServer.ClipboardSet(Convert.ToHexString(prop.GetBytes()));

				Window propEditor = GetNode<Window>("%BasicPropEditor");

				if (!propEditor.Visible)
				{
					//ReleaseCursor();
					//propEditor.Show();
				}

				propEditor.GetNode<SpinBox>("VBoxContainer/HBoxContainer1/SpinBox").SetValueNoSignal(prop.PropID);
				propEditor.GetNode<SpinBox>("VBoxContainer/HBoxContainer1/SpinBox2").SetValueNoSignal(prop.Rotation);

				propEditor.GetNode<SpinBox>("VBoxContainer/HBoxContainer2/SpinBoxX").SetValueNoSignal(position.X);
				propEditor.GetNode<SpinBox>("VBoxContainer/HBoxContainer2/SpinBoxY").SetValueNoSignal(position.Y);
				propEditor.GetNode<SpinBox>("VBoxContainer/HBoxContainer2/SpinBoxZ").SetValueNoSignal(position.Z);

				_TEST_selectedprop = prop;
			}
			else
				_StatusLabel.PrintMessage($"No prop found at ({position})");*/
		}
		private StageData.Prop _TEST_selectedprop;
		public void DoPropEditor(Vector3I position, ushort propId, byte rotation)
		{
			_TEST_selectedprop.PropID = propId;
			_TEST_selectedprop.Rotation = rotation;

			_TEST_selectedprop.X = (byte)(position.X % 32);
			_TEST_selectedprop.Y = (byte)position.Y;
			_TEST_selectedprop.Z = (byte)(position.Z % 32);

			_TEST_selectedprop.Chunk = (ushort)(position.Z / 32 * 64 + position.X / 32);
		}

		public void TEST_PropData()
		{
			List<string> lines = [];
			for (int i = 0; i < _StageData.PropCount; i++)
			{
				StageData.Prop prop = _StageData.GetProp(i);

				string line = "";

				foreach (byte b in prop.GetBytes())
				{
					line += $"{b:X2} ";
				}

				line += $"{prop.GetInfo().Name} [{prop.PropID}] ({prop.GetPosition()})";

				lines.Add(line);
			}
			
			using var file = Godot.FileAccess.Open("res://propdata.txt", Godot.FileAccess.ModeFlags.Write);
			file.StoreString(string.Join('\n', lines));

		}
		#endregion
	}
}