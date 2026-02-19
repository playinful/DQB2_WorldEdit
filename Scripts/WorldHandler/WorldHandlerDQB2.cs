using EyeOfRubiss.Scenes;
using Godot;
using System;
using EyeOfRubiss.Info.DQB2;
using EyeOfRubiss.Nodes;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EyeOfRubiss
{
    public class WorldHandlerDQB2(WorldEditorScene worldEditorScene) : WorldHandler(worldEditorScene)
    {
		const ushort BLOCK_AIR = 0;

        private CommonData _CommonData;
        private StageData _StageData;

        private bool _Loaded = false;
        private bool _BGPartsLoaded = false;

        private NPCSprite _SelectedNPCSprite;

        public override string GetDebugInfo(Vector3I position)
        {
            StageData.BlockInstance block = _StageData.GetBlockAtPosition(position);
            Vector3I dataPosition = StageData.PositionToDataPosition(position);

            if (block is not null)
			{
				string result = "";
				result += $"Targeted block: {BlockInfo.Get(block.BlockID).Name} [{block.BlockID}]";
				result += $"\nX: {position.X}, Y: {position.Y}, Z: {position.Z}";
	    		result += $"\nChunk: {dataPosition.X}, Layer: {dataPosition.Y}, Tile: {dataPosition.Z}";
	    		result += $"\nPlaced by Builder: {block.PlayerPlaced}";
	    		result += $"\nShape: {block.Chisel}";

				if (_StageData.GetOverlappingBGParts(position) is StageData.BGParts bgParts)
				{
					result += $"\nTargeted prop: {bgParts.GetInfo().Name} [{bgParts.BGPartsID}]";
					result += $"\nDirection: {Util.DirectionToString(bgParts.Direction)}";
				}

				return result.ToString();
			}
            else
                return
                    "Targeted block: Error\n" +
                    $"X: {position.X}, Y: {position.Y}, Z: {position.Z}";
        }

        #region Scene Setup
        public void LoadStageData(StageData stageData)
		{
            GD.Print("Loading");
			UnloadStageData();

			_StageData = stageData;

			if (_WorldEditorScene.ShowBGParts)
			{
				CreateProps(stageData);
			}
			if (_WorldEditorScene.ShowPropShells)
			{
				_WorldEditorScene._VoxelTerrain_PropShells.Generator = new VoxelGeneratorDQB2(stageData, showPartsBlock: true);
			}
			if (_WorldEditorScene.ShowTerrain)
			{
				_WorldEditorScene._VoxelTerrain.SetDeferred(VoxelNode.PropertyName.Generator, new VoxelGeneratorDQB2(stageData));
			}

			if (_CommonData is not null)
			{
				LoadCommonData(_CommonData);
			}

			_Loaded = true;
		}
        public void UnloadStageData()
		{
			_Loaded = false;
			_WorldEditorScene._VoxelTerrain.Generator = null;
			_WorldEditorScene._VoxelTerrain_PropShells.Generator = null;
			_WorldEditorScene._Gizmo.ClearSelection();
			DestroyProps();
			DestroyResidents();
			_WorldEditorScene._PlayerDisplay.Hide();
			_StageData = null;
		}
		
		public void LoadCommonData(CommonData commonData)
		{
			if (_StageData is null)
				return;

            _CommonData = commonData;
			CreateResidents(_CommonData);

			_WorldEditorScene._PlayerDisplay.SetNPCName(_CommonData.PlayerName);
			_WorldEditorScene._PlayerDisplay.Position = _CommonData.GetPlayerPosition();
			_WorldEditorScene._PlayerDisplay.Rotation = Vector3.Up * _CommonData.PlayerRotation;
			_WorldEditorScene._PlayerDisplay.Visible = _WorldEditorScene.ShowPlayer && _CommonData.ToIsland == _StageData.IslandID;
		}
		public void UnloadCommonData()
		{
			DestroyResidents();
			_WorldEditorScene._PlayerDisplay.Hide();
            _CommonData = null;
		}

		public void CreateProps(StageData stageData)
		{
			foreach (StageData.BGParts prop in stageData.GetProps())
			{
				if (prop.Exists() && prop.GetInfo().Mesh is int meshId)
				{
					_WorldEditorScene._BGPartsGrid.SetCellItemDelegated(prop.GetPosition(), meshId, prop.GetGridMapRotation());
				}
			}
			_BGPartsLoaded = true;
		}
		public void DestroyProps()
		{
			_WorldEditorScene._BGPartsGrid.Clear();
			_WorldEditorScene._BGPartsGrid.ClearSubGrid();
			_BGPartsLoaded = false;
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
			_WorldEditorScene._ResidentLayer.AddChild(npcSprite);
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
			_WorldEditorScene._ResidentLayer.QueueFreeAllChildren();
		}
        #endregion

        #region Stage editing
		public void SetBlock(Vector3I position, ushort blockId, bool destroyProps = true)
		{
			if (_StageData is null)
				return;

			if (!StageData.PositionIsInBounds(position))
			{
				_WorldEditorScene._StatusLabel.PrintMessage("Cannot place blocks out of bounds.");
				return;
			}

			_StageData.SetBlockAtPosition(position, blockId);
			UpdateTerrain(position);

			BlockInfo blockInfo = BlockInfo.Get(blockId);
			if (destroyProps && blockInfo.GetPartsType() == PartsType.None)
			{
				DestroyBGParts(position);
			}

			// 	if (_WorldEditorScene.AutomaticallyGenerateBedrock && blockId != Constants.BLOCK_AIR && position.Y > 0)
			// 	{
			// 		SetBlock(new Vector3I(position.X, 0, position.Z), Constants.BLOCK_BEDROCK, chiselType, playerPlaced);
			// 	}
			// }
			// else
			// {
			// 	_WorldEditorScene._StatusLabel.PrintMessage("Cannot place blocks out of bounds.");
			// }
		}
		public void SetBGParts(Vector3I position, ushort bgPartsId)
		{
			if (_StageData is null)
				return;

			BGPartsInfo partsInfo = BGPartsInfo.Get(bgPartsId);
			
			StageData.BGParts bgParts = _StageData.AddBGParts(position, bgPartsId, _WorldEditorScene.GetBGPartsPlacementDirection()); // TODO enum for fixed rotations
			
			(Vector3I start, Vector3I end) = bgParts.GetBounds();
			for (int x = start.X; x <= end.X; x++)
            {
                for (int y = start.Y; y <= end.Y; y++)
                {
                    for (int z = start.Z; z <= end.Z; z++)
                    {
                        ChangePartsBlock(new Vector3I(x, y, z), partsInfo.Block);
                    }
                }
            }

            if (_WorldEditorScene.ShowBGParts || _BGPartsLoaded)
    			_WorldEditorScene._BGPartsGrid.SetCellItemDelegated(position, partsInfo.Mesh ?? -1, bgParts.GetGridMapRotation());
		}
		public void SetFluid()
		{
			
		}

		public void DestroyBGParts(Vector3I position)
		{
			List<StageData.BGParts> props = _StageData.GetAllOverlappingProps(position).ToList();
			foreach (StageData.BGParts prop in props)
			{
				(Vector3I start, Vector3I end) = prop.GetBounds();
				_WorldEditorScene._BGPartsGrid.ClearCellItem(prop.GetPosition());
				prop.Clear();

				for (int x = start.X; x <= end.X; x++)
				{
					for (int y = start.Y; y <= end.Y; y++)
					{
						for (int z = start.Z; z <= end.Z; z++)
						{
							Vector3I otherPosition = new(x, y, z);
							if (_StageData.GetOverlappingBGParts(otherPosition) is StageData.BGParts otherProp)
							{
								ChangePartsBlock(otherPosition, otherProp.GetInfo().Block);
							}
							else
							{
								ChangePartsBlock(otherPosition, PartsType.None);
							}
						}
					}
				}
			}
		}

		public void ChangePartsBlock(Vector3I position, PartsType propShell)
		{
			BlockInfo blockInfo = _StageData.GetBlockAtPosition(position).GetInfo();
			SetBlock(position, FluidConverter.Convert(blockInfo.FluidType, blockInfo.FluidLevel, propShell));
		}
		
		public void UpdateTerrain(Vector3I position)
		{
			StageData.BlockInstance block = _StageData.GetBlockAtPosition(position);
			if (block is null)
				return;
			
			BlockInfo blockInfo = block.GetInfo();

			if (_WorldEditorScene.ShowTerrain)
			{
				ulong voxelId = blockInfo.Voxel;
				_WorldEditorScene._VoxelTool.SetVoxel(position, voxelId);
			}
			if (_WorldEditorScene.ShowPropShells)
			{
				ulong voxelId = (ulong)blockInfo.GetPartsType();
				_WorldEditorScene._VoxelTool_PropShells.SetVoxel(position, voxelId);
			}
		}
		#endregion

        #region Display changed
        public override void OnTerrainDisplayChanged(bool show)
        {
			_WorldEditorScene._VoxelTerrain.Generator = (show && _StageData is not null) ? new VoxelGeneratorDQB2(_StageData) : null;
        }
        public override void OnPropShellsDisplayChanged(bool show)
        {
			_WorldEditorScene._VoxelTerrain_PropShells.Generator = (show && _StageData is not null) ? new VoxelGeneratorDQB2(_StageData, showPartsBlock: true) : null;
        }
        public override void OnPlayerDisplayChanged(bool show)
        {
            _WorldEditorScene._PlayerDisplay.Visible = show && _StageData is not null && _CommonData is not null && _StageData.IslandID == _CommonData.ToIsland;
        }
        public override void OnPropsDisplayChanged(bool show)
        {
			if (show && _StageData is not null && !_BGPartsLoaded)
				CreateProps(_StageData);
        }
        #endregion

        #region Brush methods
        public override void DoSetBlock(Vector3I position, int block)
        {
            if (block >= ushort.MinValue && block <= ushort.MaxValue)
				SetBlock(position, (ushort)block);
        }
        public override void DoSetBGParts(Vector3I position, int bgParts)
        {
            if (bgParts >= ushort.MinValue && bgParts <= ushort.MaxValue)
				SetBGParts(position, (ushort)bgParts);
        }
        public override void DoSetFluid(Vector3I position, int fluid)
        {
            
        }

        public override void DoEraser(Vector3I position)
        {
            SetBlock(position, BLOCK_AIR);
        }

		public override void DoPointer()
		{
			Node3D pointedObject = _WorldEditorScene.GetPointedObject();
			if (pointedObject is NPCSprite npc)
			{
				SelectNPC(npc);
			}
		}

        public override void DoEyedropper(Vector3I position)
        {
            if (_StageData is null)
				return;
			
			if (_StageData.GetBlockAtPosition(position) is StageData.BlockInstance block)
			{
				BlockInfo blockInfo = block.GetInfo();

				if (blockInfo.GetPartsType() == PartsType.None)
				{
					_WorldEditorScene.SetBrushBlock(block.BlockID);
				}
				else
				{
					if (_StageData.GetOverlappingBGParts(position) is StageData.BGParts parts)
					{
						_WorldEditorScene.SetBrushBGParts(parts.BGPartsID);
					}
					else
					{
						_WorldEditorScene.SetBrushBlock(block.BlockID);
					}
				}
			}
        }
        #endregion
		
        #region NPC editing
		public void SelectNPC(NPCSprite npc)
		{
			_WorldEditorScene._Gizmo.ClearSelection();
			_WorldEditorScene._Gizmo.Select(npc);
			_SelectedNPCSprite = npc;
		}
        public override void OnGizmo3DTransformEnd()
        {
            if (_CommonData is null)
				return;

			if (_SelectedNPCSprite == _WorldEditorScene._PlayerDisplay)
			{
				_CommonData.PlayerPositionX = _SelectedNPCSprite.Position.X;
				_CommonData.PlayerPositionY = _SelectedNPCSprite.Position.Y;
				_CommonData.PlayerPositionZ = _SelectedNPCSprite.Position.Z;

				_CommonData.PlayerRotation = _SelectedNPCSprite.Rotation.Y;
			}
			else
			{
				_SelectedNPCSprite.Resident.PositionX = _SelectedNPCSprite.Position.X; // TODO rework with IDs
				_SelectedNPCSprite.Resident.PositionY = _SelectedNPCSprite.Position.Y;
				_SelectedNPCSprite.Resident.PositionZ = _SelectedNPCSprite.Position.Z;

				_SelectedNPCSprite.Resident.Rotation = _SelectedNPCSprite.Rotation.Y;
			}
        }
        #endregion
    }
}