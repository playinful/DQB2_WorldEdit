using EyeOfRubiss;
using EyeOfRubiss.Info;
using EyeOfRubiss.Integration;
using EyeOfRubiss.Nodes;
using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Cloned from <see cref="EyeOfRubiss.Scenes.WorldEditorScene"/> and hacked
/// </summary>
public partial class IntegrationWorldEditor : Node3D
{
	private CameraController _CameraController;
	private VoxelTerrain _VoxelTerrain;
	private VoxelTool _VoxelTool;
	private Label _PointedVoxelLabel;
	private IWorld _StageData;

	public bool ShowDebugInfo { get; set; } = true;
	public bool ShowFps { get; set; } = true;
	public bool ShowTerrain { get; set; } = true;

	public override void _Ready()
	{
		_CameraController = GetNode<CameraController>("%Camera3D");
		_VoxelTerrain = GetNode<VoxelTerrain>("%VoxelTerrain");
		_VoxelTool = _VoxelTerrain.GetVoxelTool();
		_PointedVoxelLabel = GetNode<Label>("%PointedVoxelLabel");
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
		Vector3 origin = _CameraController.GlobalTransform.Origin;
		Vector3 forward = -_CameraController.Transform.Basis.Z.Normalized();
		VoxelRaycastResult hit = _VoxelTool.Raycast(origin, forward, 4096);
		return hit;
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

			var block = _StageData.GetBlockAtPosition(result.Position);

			_PointedVoxelLabel.Text =
				$"Targeted block: {(block.BlockId > 0 ? BlockInfo.Get(block.BlockId).Name + $" [{block.BlockId}]" : "UNKNOWN")}\n" +
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

	#region Scene setup
	internal void LoadWorld(IWorld stageData)
	{
		if (_StageData == null)
		{
			_CameraController.Position = stageData.InitialCameraPosition;
		}
		_StageData = stageData;

		if (ShowTerrain)
			_VoxelTerrain.Generator = new IWorldVoxelGenerator(stageData);
	}
	public void UnloadWorld()
	{
		_VoxelTerrain.Generator = null;
	}

	public void Reload()
	{
		if (_StageData is null)
			return;

		LoadWorld(_StageData);
	}
	#endregion

	#region Control handling
	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed(Constants.Controls.CURSOR_RELEASE))
			ReleaseCursor();

		if (@event.IsPressed() && @event is InputEventMouseButton mouseButtonEvent2 && mouseButtonEvent2.ButtonIndex == MouseButton.Left) // TODO probably change this to action
			CaptureCursor();

		if (@event.IsActionPressed(Constants.Controls.RESET_CAMERA))
		{
			_CameraController.Position = Vector3.Up * 96;
			_CameraController.Rotation = Vector3.Zero;
		}
	}

	public void CaptureCursor()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;
		_CameraController.Enabled = true;
	}
	public void ReleaseCursor()
	{
		Input.MouseMode = Input.MouseModeEnum.Visible;
		_CameraController.Enabled = false;
	}

	public void MoveCamera(Vector3 target)
	{
		_CameraController.Position = target;
	}
	#endregion
}
