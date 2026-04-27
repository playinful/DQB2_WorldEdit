using EyeOfRubiss.Scenes;
using Godot;
using System;
using EyeOfRubiss.Info.DQB2;
using EyeOfRubiss.Nodes;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection.Metadata;

namespace EyeOfRubiss
{
    public class WorldHandlerDQB2(WorldEditorScene worldEditorScene) : WorldHandler(worldEditorScene)
    {
		const ushort BLOCK_AIR = 0;

        public CommonData _CommonData { get; private set; }
        public StageData _StageData { get; private set; }

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
				GenerateBGParts(stageData);
			}
			ReloadTerrain();

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
			DestroyBGParts();
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

		public void GenerateBGParts(StageData stageData)
		{
			foreach (StageData.BGParts prop in stageData.GetBGParts())
			{
				if (prop.Exists() && prop.GetInfo().Mesh is int meshId)
				{
					_WorldEditorScene._BGPartsGridManager.AddCellItem(prop.GetPosition(), meshId, prop.GetGridMapRotation());
				}
			}
			_BGPartsLoaded = true;
		}
		public void DestroyBGParts()
		{
			_WorldEditorScene._BGPartsGridManager.Clear();
			_BGPartsLoaded = false;
		}

		public void CreateResident(CommonData.Resident resident)
		{
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
        
		public void ReloadTerrain()
		{
			if (_StageData is not null)
			{
				_WorldEditorScene._VoxelTerrain.Generator = new VoxelGeneratorDQB2(_StageData, showTerrain: _WorldEditorScene.ShowTerrain, showFluid: _WorldEditorScene.ShowFluids);
				_WorldEditorScene._VoxelTerrain_PropShells.Generator = new VoxelGeneratorDQB2(_StageData, showPartsBlock: true);
			}
			else
			{
				_WorldEditorScene._VoxelTerrain.Generator = null;
				_WorldEditorScene._VoxelTerrain_PropShells.Generator = null;
			}
		}
		#endregion

        #region Display changed
        public override void OnTerrainDisplayChanged(bool show)
		{
			ReloadTerrain();
		}
        public override void OnFluidsDisplayChanged(bool show)
        {
			ReloadTerrain();
        }
		public override void OnPlayerDisplayChanged(bool show)
        {
            _WorldEditorScene._PlayerDisplay.Visible = show && _StageData is not null && _CommonData is not null && _StageData.IslandID == _CommonData.ToIsland;
        }
        public override void OnBGPartsDisplayChanged(bool show)
        {
			if (show && _StageData is not null && !_BGPartsLoaded)
				GenerateBGParts(_StageData);
        }
        #endregion

        #region Stage editing
		public void SetBlock(Vector3I position, ushort blockId, bool? playerPlaced = null, ChiselShape? chisel = null, bool destroyProps = true)
		{
			if (_StageData is null)
				return;

			_StageData.SetBlockAtPosition(position, blockId, chisel: chisel, playerPlaced: playerPlaced);
			UpdateBlock(position);

			BlockInfo blockInfo = BlockInfo.Get(blockId);
			if (destroyProps && blockInfo.GetPartsType() == PartsType.None)
			{
				RemoveBGParts(position);
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
		public void SetBGParts(Vector3I position, ushort bgPartsId, byte direction, PartsType? partsBlock = null, bool collision = true, bool effects = true, byte connectingWindowRotation = 0)
		{
			if (_StageData is null)
				return;

			BGPartsInfo partsInfo = BGPartsInfo.Get(bgPartsId);
			
			StageData.BGParts bgParts = _StageData.AddBGParts(position, bgPartsId, direction); // TODO enum for fixed rotations
			bgParts.Effects = effects && partsInfo.Effects;
			bgParts.Collision = collision && partsInfo.Collision;
			bgParts.ConnectingWindowRotation = connectingWindowRotation;
			
			(Vector3I start, Vector3I end) = bgParts.GetBounds();
			for (int x = start.X; x <= end.X; x++)
            {
                for (int y = start.Y; y <= end.Y; y++)
                {
                    for (int z = start.Z; z <= end.Z; z++)
                    {
                        ChangePartsBlock(new Vector3I(x, y, z), partsBlock ?? partsInfo.Block);
                    }
                }
            }

            if (_WorldEditorScene.ShowBGParts || _BGPartsLoaded)
    			_WorldEditorScene._BGPartsGridManager.AddCellItem(position, partsInfo.Mesh ?? -1, bgParts.GetGridMapRotation());
		}

		public void RemoveBGParts(Vector3I position)
		{
			List<StageData.BGParts> props = _StageData.GetAllOverlappingBGParts(position).ToList();
			foreach (StageData.BGParts prop in props)
			{
				(Vector3I start, Vector3I end) = prop.GetBounds();
				_WorldEditorScene._BGPartsGridManager.ClearCellItem(prop.GetPosition());
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

        public override void ReplaceBlock(int replace, int with, Vector3I? from = null, Vector3I? to = null)
        {
            if (_StageData is null)
				return;
			
			if (from is Vector3I _from && to is Vector3I _to)
			{
                for (int x = _from.X; x <= _to.X; x++)
                {
                    for (int y = _from.Y; y <= _to.Y; y++)
                    {
                        for (int z = _from.Z; z <= _to.Z; z++)
                        {
                            Vector3I position = new(x, y, z);
                            if (_StageData.GetBlockAtPosition(position).BlockID == replace)
							{
								SetBlock(position, (ushort)with, false);
							}
                        }
                    }
                }
			}
			else
			{
				foreach (StageData.Chunk chunk in _StageData.GetUsedChunks())
				{
					foreach (StageData.BlockInstance block in chunk.GetAllBlocks())
					{
						if (block.BlockID == replace)
						{
							block.BlockID = (ushort)with;
						}
					}
				}
				ReloadTerrain();
			}
        }

        public override bool CanCopy()
        {
            return _StageData is not null;
        }
        public override EyeOfRubissStructure DoCopy(Vector3I start, Vector3I end)
        {
            if (!CanCopy())
				return null;
			
			return EyeOfRubissStructure.From(_StageData, start, end);
        }

		public void UpdateBlock(Vector3I position)
		{
			if (_StageData is null)
				return;
			
			_WorldEditorScene._VoxelTool.SetVoxel(position, VoxelGeneratorDQB2.GetVoxelAtPosition(_StageData, position, showTerrain: _WorldEditorScene.ShowTerrain, showFluid: _WorldEditorScene.ShowFluids));
			_WorldEditorScene._VoxelTool_PropShells.SetVoxel(position, VoxelGeneratorDQB2.GetVoxelAtPosition(_StageData, position, showPartsBlock: true));
		}
		#endregion

        #region Brush methods
        public override void DoSetBlock(Vector3I position, int block)
        {
			if (_StageData is null)
				return;
			
			if (!StageData.PositionIsInBounds(position))
			{
				_WorldEditorScene._StatusLabel.PrintMessage("Cannot place blocks out of bounds.");
				return;
			}

            if (block >= ushort.MinValue && block <= ushort.MaxValue)
				SetBlock(position, (ushort)block);
        }
        public override void DoSetBGParts(Vector3I position, int bgParts, PartsType? partsBlock = null, bool collision = true, bool effects = true)
        {
            if (bgParts >= ushort.MinValue && bgParts <= ushort.MaxValue)
				SetBGParts(position, (ushort)bgParts, _WorldEditorScene.GetBGPartsPlacementDirection(), partsBlock: partsBlock, collision: collision, effects: effects);
        }
        public override void DoSetFluid(Vector3I position, int fluidType, int fluidLevel)
        {
        	if (_StageData is null)
        	    return;
	
        	StageData.BlockInstance block = _StageData.GetBlockAtPosition(position);
        	BlockInfo blockInfo = block.GetInfo();

        	ushort newBlock = FluidConverter.Convert((FluidType)fluidType, (FluidLevel)fluidLevel, blockInfo.GetPartsType());
        	SetBlock(position, newBlock, false);
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

        public override void DoPaste(Vector3I position, EyeOfRubissStructure clipboard, bool pasteAir)
        {
            if (_StageData is null)
				return;
			
			if (clipboard.SourceGame == 1)
			{
				if (pasteAir)
				{
					for (int x = 0; x < clipboard.SizeX; x++)
					{
						for (int y = 0; y < clipboard.SizeY; y++)
						{
							for (int z = 0; z < clipboard.SizeZ; z++)
							{
								Vector3I clipboardPosition = new(x, y, z);
								SetBlock(position + clipboardPosition, Info.DQB1.BlockInfo.Get((byte)clipboard.GetBlock(clipboardPosition)).DQB2Block);
							}
						}
					}
				}
				else
				{
                	foreach ((Vector3I clipboardPosition, ushort block) in clipboard.GetAllBlocks())
                	{
						ushort blockId = Info.DQB1.BlockInfo.Get((byte)block).DQB2Block;
						if (blockId != Constants.BLOCK_AIR)
						{
							SetBlock(position + clipboardPosition, blockId);
						}
                	}
				}
				foreach (EyeOfRubissStructure.BGPartsData bgParts in clipboard.GetBGParts())
				{
					ushort bgPartsId = Info.DQB1.BGPartsInfo.Get(bgParts.BGPartsID).DQB2BGParts;
					if (bgPartsId != 0)
					{
						SetBGParts(position + bgParts.GetPosition(), bgPartsId, bgParts.Direction, collision: bgParts.Collision, effects: bgParts.Effects);
					}
				}
			}
			else if (clipboard.SourceGame == 2)
			{
				if (pasteAir)
				{
					for (int x = 0; x < clipboard.SizeX; x++)
					{
						for (int y = 0; y < clipboard.SizeY; y++)
						{
							for (int z = 0; z < clipboard.SizeZ; z++)
							{
								Vector3I clipboardPosition = new(x, y, z);
								ushort block = clipboard.GetBlock(clipboardPosition);
								SetBlock(position + clipboardPosition, block.GetBlockID(), playerPlaced: block.GetPlayerPlaced(), chisel: block.GetChiselShape());
							}
						}
					}
				}
				else
				{
                	foreach ((Vector3I clipboardPosition, ushort block) in clipboard.GetAllBlocks())
                	{
                	    SetBlock(position + clipboardPosition, block.GetBlockID(), playerPlaced: block.GetPlayerPlaced(), chisel: block.GetChiselShape());
                	}	
				}
				foreach (EyeOfRubissStructure.BGPartsData bgParts in clipboard.GetBGParts())
				{
					SetBGParts(position + bgParts.GetPosition(), bgParts.BGPartsID, bgParts.Direction, collision: bgParts.Collision, effects: bgParts.Effects, connectingWindowRotation: bgParts.ConnectingWindowRotation);
				}
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