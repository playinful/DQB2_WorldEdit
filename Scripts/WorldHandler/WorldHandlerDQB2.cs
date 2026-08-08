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
				result += $"\nChunk ID: {_StageData.GetChunk(dataPosition.X).BlockDataIndex}";
	    		result += $"\nPlaced by Builder: {block.PlayerPlaced}";
	    		result += $"\nShape: {block.Chisel}";

				StageData.BiomeMapData biome = _StageData.GetBiomeMapData(position);
				result += $"\nBiome: {biome.Biome}, Area: {biome.Area}, Diorama: {biome.Diorama}";

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
            _CommonData = commonData;

			if (_StageData is null)
				return;

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
		public void SetBGParts(Vector3I position, ushort bgPartsId, byte direction, PartsType? partsBlock = null, bool collision = true, bool effects = true, bool unbreakable = false, byte size = 0, byte connectingWindowRotation = 0)
		{
			if (_StageData is null)
				return;

			BGPartsInfo partsInfo = BGPartsInfo.Get(bgPartsId);
			
			StageData.BGParts bgParts = _StageData.AddBGParts(position, bgPartsId, direction); // TODO enum for fixed rotations
			bgParts.Effects = effects && partsInfo.Effects;
			bgParts.Collision = collision && partsInfo.Collision;
			bgParts.Unbreakable = unbreakable;
			bgParts.Size = size;
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
				_StageData.ClearBlockEntitiesAtPosition(prop.GetPosition());
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
			StageData.BlockInstance block = _StageData.GetBlockAtPosition(position);
			if (block is null)
				return;
			
			BlockInfo blockInfo = block.GetInfo();
			if (blockInfo.GetPartsType() != propShell)
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
				StatusLabel.PrintMessage("Cannot place blocks out of bounds.");
				return;
			}

            if (block >= ushort.MinValue && block <= ushort.MaxValue)
				SetBlock(position, (ushort)block);
        }
        public override void DoSetBGParts(Vector3I position, int bgParts, PartsType? partsBlock = null, bool collision = true, bool effects = true, bool unbreakable = false, byte size = 0)
        {
            if (bgParts >= ushort.MinValue && bgParts <= ushort.MaxValue)
				SetBGParts(position, (ushort)bgParts, _WorldEditorScene.GetBGPartsPlacementDirection(), partsBlock: partsBlock, collision: collision, effects: effects, unbreakable: unbreakable, size: size);
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

        public override void DoChisel(Vector3I position, ChiselShape shape)
        {
            if (_StageData is null)
				return;
			
			if (_StageData.GetBlockAtPosition(position) is not StageData.BlockInstance block)
				return;
			
			block.Chisel = shape;
        }

		public override void DoPointer(Vector3I position)
		{
			if (_StageData is null)
				return;

			foreach (StageData.BGParts bgParts in _StageData.GetAllOverlappingBGParts(position))
			{
				Vector3I partsPosition = bgParts.GetPosition();
				if (_StageData.GetStorageAtPosition(partsPosition) is StageData.Storage storage)
				{
					_WorldEditorScene._StorageEditor.Popup(storage);
					return;
				}
				if (_StageData.GetItemDisplayAtPosition(partsPosition) is StageData.ItemDisplay display)
				{
					_WorldEditorScene._ItemDisplayEditor.Popup(display);
					return;
				}
				if (_StageData.GetSignpostAtPosition(partsPosition) is StageData.Signpost signpost)
				{
					_WorldEditorScene._SignpostEditor.Popup(signpost);
					return;
				}
				if (_StageData.GetSalutationStationAtPosition(partsPosition) is StageData.SalutationStation station)
				{
					_WorldEditorScene._SalutationStationEditor.Popup(station);
					return;
				}
				if (_StageData.GetInstrumentAtPosition(partsPosition) is StageData.Instrument instrument)
				{
					_WorldEditorScene._InstrumentEditor.Popup(instrument);
					return;
				}
				if (_StageData.GetMagneticBlockAtPosition(partsPosition) is StageData.MagneticBlock block)
				{
					_WorldEditorScene._MagneticBlockEditor.Popup(block);
					return;
				}
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

        #region Tools
        public override void FillInChunks()
        {
            if (_StageData is null)
				return;
			
			int seaLevel = _StageData.GetSeaLevel(_CommonData);
			
			foreach (StageData.Chunk chunk in _StageData.GetUsedChunks())
			{
				for (int x = 0; x < StageData.CHUNK_SIZE; x++)
				{
					for (int z = 0; z < StageData.CHUNK_SIZE; z++)
					{
						Vector3I position = new(x, 0, z);
						StageData.BlockInstance block = chunk.GetBlock(position);
						if (block is not null && block.BlockID == Constants.BLOCK_AIR)
						{
							block.BlockID = Constants.BLOCK_BEDROCK;
							block.Chisel = ChiselShape.FullBlock;
							block.PlayerPlaced = false;

							for (int y = 1; y < seaLevel; y++)
							{
								chunk.SetBlock(new Vector3I(x, y, z), 341);
							}

							if (seaLevel > 0)
								chunk.SetBlock(new Vector3I(x, seaLevel, z), 349);	
						}
					}
				}
			}

			ReloadTerrain();
        }

        public override void DeleteAllBGParts()
        {
            if (_StageData is null)
				return;
			
			HashSet<Vector3I> partsBlocks = [];
			foreach (StageData.BGParts bgParts in _StageData.GetBGParts())
			{
				if (!bgParts.Exists())
					continue;

				(Vector3I start, Vector3I end) = bgParts.GetBounds();
				for (int x = start.X; x <= end.X; x++)
				{
					for (int y = start.Y; y <= end.Y; y++)
					{
						for (int z = start.Z; z <= end.Z; z++)
						{
							partsBlocks.Add(new Vector3I(x, y, z));
						}
					}
				}
				bgParts.Clear();
			}
			foreach (Vector3I position in partsBlocks)
			{
				ChangePartsBlock(position, PartsType.None);
			}

			_StageData.ClearAllBlockEntities();
			_StageData.PropCount = 0;
			_WorldEditorScene._BGPartsGridManager.Clear();
        }

        public override void FixPropShells()
        {
            if (_StageData is null)
				return;
			
			foreach (StageData.BGParts bgParts in _StageData.GetBGParts())
			{
				(Vector3I start, Vector3I end) = bgParts.GetBounds();
				for (int x = start.X; x <= end.X; x++)
				{
					for (int y = start.Y; y <= end.Y; y++)
					{
						for (int z = start.Z; z <= end.Z; z++)
						{
							Vector3I position = new(x, y, z);
							if (_StageData.GetBlockAtPosition(position) is StageData.BlockInstance block && block.GetInfo().GetPartsType() == PartsType.None)
							{
								ChangePartsBlock(position, bgParts.GetInfo().Block);
							}
						}
					}
				}
			}
        }

        public override void FixFakeBlocks()
        {
            if (_StageData is null)
			 	return;
			
			foreach (StageData.BGParts bgParts in _StageData.GetBGParts())
			{
				BGPartsInfo info = bgParts.GetInfo();
				if (info.IsFakeBlock())
				{
					Vector3I position = bgParts.GetPosition();
					bgParts.Clear();
					_WorldEditorScene._BGPartsGridManager.ClearCellItem(position);
					_StageData.ClearBlockEntitiesAtPosition(position);

					SetBlock(position, info.GetFakeBlockID(), destroyProps: false);
				}
			}
        }

        public override void ClearOrphanedBlockEntities()
        {
            if (_StageData is null)
				return;
			
			foreach (StageData.Storage storage in _StageData.GetAllStorage())
			{
				if (storage.Enabled && _StageData.GetBGPartsAtPosition(storage.GetPosition()) is null)
					storage.Clear();
			}
			
			foreach (StageData.ItemDisplay display in _StageData.GetAllItemDisplays())
			{
				if (display.Enabled && _StageData.GetBGPartsAtPosition(display.GetPosition()) is null)
					display.Clear();
			}
			
			foreach (StageData.CraftingStation station in _StageData.GetCraftingStations())
			{
				if (_StageData.GetBGPartsAtPosition(station.GetPosition()) is null)
					station.Clear();
			}
			
			foreach (StageData.Signpost signpost in _StageData.GetSignposts())
			{
				if (signpost.Enabled && _StageData.GetBGPartsAtPosition(signpost.GetPosition()) is null)
					signpost.Clear();
			}
			
			foreach (StageData.SalutationStation station in _StageData.GetSalutationStations())
			{
				if (station.Enabled && _StageData.GetBGPartsAtPosition(station.GetPosition()) is null)
					station.Clear();
			}
			
			foreach (StageData.Crop crop in _StageData.GetCrops())
			{
				if (_StageData.GetBGPartsAtPosition(crop.GetPosition()) is null)
					crop.Clear();
			}
			
			foreach (StageData.Scarecrow scarecrow in _StageData.GetScarecrows())
			{
				if (scarecrow.Enabled && _StageData.GetBGPartsAtPosition(scarecrow.GetPosition()) is null)
					scarecrow.Clear();
			}
			
			foreach (StageData.Instrument instrument in _StageData.GetInstruments())
			{
				if (_StageData.GetBGPartsAtPosition(instrument.GetPosition()) is null)
					instrument.Clear();
			}
			
			foreach (StageData.MagneticBlock block in _StageData.GetMagneticBlocks())
			{
				if (block.Enabled && _StageData.GetBGPartsAtPosition(block.GetPosition()) is null)
					block.Clear();
			}
			
			foreach (StageData.MagicPencil pencil in _StageData.GetMagicPencils())
			{
				if (pencil.Enabled && _StageData.GetBGPartsAtPosition(pencil.GetPosition()) is null)
					pencil.Clear();
			}
			
			foreach (StageData.FireworkCannon cannon in _StageData.GetFireworkCannons())
			{
				if (cannon.Enabled && _StageData.GetBGPartsAtPosition(cannon.GetPosition()) is null)
					cannon.Clear();
			}
			
			foreach (StageData.PictureFrame frame in _StageData.GetPictureFrames())
			{
				if (frame.Enabled && _StageData.GetBGPartsAtPosition(frame.GetPosition()) is null)
					frame.Clear();
			}
			
			foreach (StageData.Watchfire watchfire in _StageData.GetWatchfires())
			{
				if (watchfire.Enabled && _StageData.GetBGPartsAtPosition(watchfire.GetPosition()) is null)
					watchfire.Clear();
			}
			
			foreach (StageData.WardOfErdrick ward in _StageData.GetWardsOfErdrick())
			{
				if (ward.Enabled && _StageData.GetBGPartsAtPosition(ward.GetPosition()) is null)
					ward.Clear();
			}
			
			foreach (StageData.Buggy buggy in _StageData.GetBuggies())
			{
				if (buggy.Enabled && _StageData.GetBGPartsAtPosition(buggy.GetPosition()) is null)
					buggy.Clear();
			}
			
			foreach (StageData.Toilet toilet in _StageData.GetToilets())
			{
				if (toilet.Enabled && _StageData.GetBGPartsAtPosition(toilet.GetPosition()) is null)
					toilet.Clear();
			}
        }

        public override void CreateWaterCeiling()
        {
            if (_StageData is null)
				return;
			
			int worldTop = StageData.WORLD_HEIGHT_BLOCKS - 1;
			int floodDataCount = 0;
			foreach (StageData.Chunk chunk in _StageData.GetUsedChunks())
			{
				for (int x = 0; x < StageData.CHUNK_SIZE; x++)
				{
					for (int z = 0; z < StageData.CHUNK_SIZE; z++)
					{
						if (x % 96 != 0 && z % 96 != 0 &&
							chunk.GetBlock(new Vector3I(x, 0, z)) is StageData.BlockInstance bottom && bottom.BlockID != 0 &&
							chunk.GetBlock(new Vector3I(x, worldTop, z)) is StageData.BlockInstance top && top.BlockID == 0)
						{
							chunk.SetBlock(new Vector3I(x, worldTop, z), 145);
						}
					}
				}

				_StageData.SetUInt16(0xC15AB + floodDataCount * 12 + 4, (ushort)(chunk.GetOrigin().X + 31));
				_StageData.SetUInt16(0xC15AB + floodDataCount * 12 + 6, (ushort)(chunk.GetOrigin().Z + 31));
				_StageData.SetByte(0xC15AB + floodDataCount * 12 + 8, (byte)worldTop);
				floodDataCount++;
			}
			_StageData.SetUInt16(0xC15A7, (ushort)floodDataCount);

			ReloadTerrain();
        }
		#endregion

        #region NPC editing
        public override void OnGizmo3DTransformEnd(NPCSprite npcSprite)
        {
            if (_CommonData is null)
				return;

			if (npcSprite.ResidentID == -1)
			{
				_CommonData.PlayerPositionX = npcSprite.Position.X;
				_CommonData.PlayerPositionY = npcSprite.Position.Y;
				_CommonData.PlayerPositionZ = npcSprite.Position.Z;

				_CommonData.PlayerRotation = npcSprite.Rotation.Y;
			}
			else
			{
				CommonData.Resident resident = _CommonData.GetResident(npcSprite.ResidentID);

				resident.PositionX = npcSprite.Position.X;
				resident.PositionY = npcSprite.Position.Y;
				resident.PositionZ = npcSprite.Position.Z;

				resident.Rotation = npcSprite.Rotation.Y;
			}
        }
        #endregion
    }
}