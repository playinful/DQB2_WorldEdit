using EyeOfRubiss.Scenes;
using Godot;
using System;
using EyeOfRubiss.Nodes;
using EyeOfRubiss.Info.DQB1;
using System.Formats.Tar;
using System.Linq;
using System.ComponentModel;

namespace EyeOfRubiss
{
    public class WorldHandlerDQB1(WorldEditorScene worldEditorScene) : WorldHandler(worldEditorScene)
    {
        public WorldData _WorldData;
        public ParamData _ParamData;

        private bool _BGPartsLoaded = false;
        private bool _ResidentsLoaded = false;

        public override string GetDebugInfo(Vector3I position)
        {
            if (_WorldData is null)
                return "Targeted Block: Unknown";

            byte block = _WorldData.GetBlockAtPosition(position);
            Vector3I dataPosition = WorldData.PositionToDataPosition(position);

            string displayString = 
      			$"Targeted block: {BlockInfo.Get(block).Name + $" [{block}]"}\n" +
      			$"X: {position.X}, Y: {position.Y}, Z: {position.Z}\n" +
                $"Chunk: {dataPosition.X}, ID: {_WorldData.GetChunk(dataPosition.X).ChunkID}, Layer: {dataPosition.Y}, Tile: {dataPosition.Z}";
            
            displayString += $"\nTargeted fluid: {_WorldData.GetFluidAtPosition(position)}";
            
            if (_WorldData.GetOverlappingBGParts(position) is WorldData.BGParts parts)
            {
                BGPartsInfo partsInfo = BGPartsInfo.Get(parts.BGPartsID);
                displayString += $"\nTargeted object: {partsInfo.Name} [{parts.BGPartsID}]";
                displayString += $"\nDirection: {Util.DirectionToString(parts.Direction)}";
            }

            return displayString;
        }

        #region Scene setup
        public void LoadWorldData(WorldData worldData)
        {
            _WorldData = worldData;
            
            ReloadTerrain();
            // TODO parts block
            if (_WorldEditorScene.ShowBGParts)
                GenerateBGParts();
        }
        public void UnloadWorldData()
        {
            _WorldData = null;
            _WorldEditorScene._VoxelTerrain.Generator = null;
            _WorldEditorScene._VoxelTerrain_PropShells.Generator = null;
            DestroyBGParts();
        }

        public void LoadParamData(ParamData paramData)
        {
            _ParamData = paramData;

			_WorldEditorScene._PlayerDisplay.SetNPCName("Player");
			_WorldEditorScene._PlayerDisplay.Position = _ParamData.GetPlayerPosition();
			//_WorldEditorScene._PlayerDisplay.Rotation = Vector3.Up * _CommonData.PlayerRotation; TODO
			_WorldEditorScene._PlayerDisplay.Visible = _WorldEditorScene.ShowPlayer;

            if (_WorldEditorScene.ShowNPCs)
                GenerateResidents();
        }
        public void UnloadParamData()
        {
			_WorldEditorScene._PlayerDisplay.Hide();
            DestroyResidents();
            _ParamData = null;
        }

        public void GenerateBGParts()
        {
            if (_WorldData is null)
                return;

            DestroyBGParts();

            foreach (WorldData.Chunk chunk in _WorldData.GetUsedChunks())
            {
                foreach (WorldData.BGParts bgParts in chunk.GetAllBGParts())
                {
                    BGPartsInfo bgPartsInfo = BGPartsInfo.Get(bgParts.BGPartsID);
                    if (bgPartsInfo.Mesh is int mesh)
                    {
                        Vector3I position = chunk.GetOrigin() + new Vector3I(bgParts.X, bgParts.Y, bgParts.Z);
                        _WorldEditorScene._BGPartsGridManager.AddCellItem(position, mesh, Util.GridMapRotationFromDirection(bgParts.Direction));
                    }
                }
            }

            _BGPartsLoaded = true;
        }
        public void DestroyBGParts()
        {
			_WorldEditorScene._BGPartsGridManager.Clear();
            _BGPartsLoaded = false;
        }
        
        public void GenerateResidents()
        {
            if (_ParamData is null)
                return;

            DestroyResidents();
            
            foreach (ParamData.Resident resident in _ParamData.GetResidents())
            {
                GenerateResident(resident);
            }

            _ResidentsLoaded = true;
        }
        public void GenerateResident(ParamData.Resident resident)
        {
            NPCSprite npcSprite = ResourceLoader.Load<PackedScene>("res://Nodes/NPCSprite.tscn").Instantiate<NPCSprite>();
            npcSprite.SetNPCName("TODO");
            npcSprite.Position = resident.GetPosition();
			_WorldEditorScene._ResidentLayer.AddChild(npcSprite);
        }
        public void DestroyResidents()
        {
            _WorldEditorScene._ResidentLayer.QueueFreeAllChildren();
            _ResidentsLoaded = false;
        }
       
        public void ReloadTerrain()
        {
            if (_WorldData is not null)
            {
                _WorldEditorScene._VoxelTerrain.Generator = new VoxelGeneratorDQB1(_WorldData, showTerrain: _WorldEditorScene.ShowTerrain, showFluid: _WorldEditorScene.ShowFluids);
                _WorldEditorScene._VoxelTerrain_PropShells.Generator = new VoxelGeneratorDQB1(_WorldData, showPartsBlock: true);
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
        public override void OnBGPartsDisplayChanged(bool show)
        {
            if (show && !_BGPartsLoaded)
            {
                GenerateBGParts();
            }
        }
        public override void OnPartsBlockDisplayChanged(bool show)
        {
        }
        public override void OnPlayerDisplayChanged(bool show)
        {
            _WorldEditorScene._PlayerDisplay.Visible = (_ParamData is not null) && show;
        }
        public override void OnNPCDisplayChanged(bool show)
        {
            if (show && !_ResidentsLoaded)
            {
                GenerateResidents();
            }
        }
        #endregion
       
        #region Stage editing
        public void SetBlock(Vector3I position, byte block)
        {
            
        }

        public bool AddBGParts(Vector3I position, int bgPartsId, byte direction, PartsType? partsBlock = null, bool collision = true, bool effects = true)
        {
            if (_WorldData.AddBGParts(position, (ushort)bgPartsId, direction, collision, effects) is not WorldData.BGParts bgParts)
                return false;

            BGPartsInfo bgPartsInfo = BGPartsInfo.Get((ushort)bgPartsId);
            byte blockId;
            if (partsBlock is PartsType partsType)
            {
                blockId = BGPartsInfo.GetPartsBlockID(partsType);
            }
            else
            {
                blockId = bgPartsInfo.GetPartsBlockID();
            }
            

            (Vector3I start, Vector3I end) = bgParts.GetBounds();
            for (int x = start.X; x <= end.X; x++)
            {
                for (int y = start.Y; y <= end.Y; y++)
                {
                    for (int z = start.Z; z <= end.Z; z++)
                    {
                        Vector3I positionB = new(x, y, z);
                        _WorldData.SetBlockAtPosition(positionB, blockId);
                        UpdateBlock(positionB);
                    }
                }
            }

            if (bgPartsInfo.Mesh is int mesh)
                _WorldEditorScene._BGPartsGridManager.AddCellItem(position, mesh, Util.GridMapRotationFromDirection(bgParts.Direction));
            
            return true;
        }
        public void RemoveBGParts(WorldData.BGParts bgParts)
        {
            (Vector3I start, Vector3I end) = bgParts.GetBounds();
            for (int x = start.X; x <= end.X; x++)
            {
                for (int y = start.Y; y <= end.Y; y++)
                {
                    for (int z = start.Z; z <= end.Z; z++)
                    {
                        Vector3I position = new(x, y, z);
                        _WorldData.SetBlockAtPosition(position, 0);
                        UpdateBlock(position);
                    }
                }
            }

            _WorldEditorScene._BGPartsGridManager.ClearCellItem(bgParts.GetPosition()); // TODO handle if more are at position
            bgParts.Clear();
        }

        public override void ReplaceBlock(int replace, int with, Vector3I? from = null, Vector3I? to = null)
        {
            if (_WorldData is null)
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
                            if (_WorldData.GetBlockAtPosition(position) == replace)
                            {
                                _WorldData.SetBlockAtPosition(position, (byte)with);
                                UpdateBlock(position);
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (WorldData.Chunk chunk in _WorldData.GetUsedChunks())
                {
                    for (int i = 0; i < WorldData.Chunk.LENGTH_BLOCKDATA; i++)
                    {
                        if (chunk.GetBlock(i) == replace)
                        {
                            chunk.SetBlock(i, (byte)with);
                        }
                    }
                }
                ReloadTerrain();
            }
        }

        public override bool CanCopy()
        {
            return _WorldData is not null;
        }
        public override EyeOfRubissStructure DoCopy(Vector3I start, Vector3I end)
        {
            if (!CanCopy())
                return null;
            
            return EyeOfRubissStructure.From(_WorldData, start, end);
        }
        #endregion

        #region Brush methods
        public override void DoSetBlock(Vector3I position, int block)
        {
            if (_WorldData is null)
                return;

			if (!WorldData.PositionIsInBounds(position))
			{
				_WorldEditorScene._StatusLabel.PrintMessage("Cannot place blocks out of bounds.");
				return;
			}

            WorldData.BGParts[] overlappingBGParts = [.. _WorldData.GetAllOverlappingBGParts(position)];
            foreach (WorldData.BGParts parts in overlappingBGParts)
            {
                RemoveBGParts(parts);
            }
            
            _WorldData.SetBlockAtPosition(position, (byte)block);
            UpdateBlock(position);
        }
        public override void DoSetBGParts(Vector3I position, int bgParts, PartsType? partsBlock = null, bool collision = true, bool effects = true)
        {
            if (_WorldData is null)
                return;

			if (!WorldData.PositionIsInBounds(position))
			{
				_WorldEditorScene._StatusLabel.PrintMessage("Cannot place blocks out of bounds.");
				return;
			}

            if (!AddBGParts(position, bgParts, _WorldEditorScene.GetBGPartsPlacementDirection(), partsBlock, collision, effects))
            {
				_WorldEditorScene._StatusLabel.PrintMessage("Failed to place object.");
            }
        }
        public override void DoEraser(Vector3I position)
        {
            DoSetBlock(position, 0);
        }

        public override void DoPaste(Vector3I position, EyeOfRubissStructure clipboard, bool pasteAir)
        {
            if (_WorldData is null)
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
                                _WorldData.SetBlockAtPosition(position + clipboardPosition, (byte)clipboard.GetBlock(clipboardPosition));
                                UpdateBlock(position + clipboardPosition);
                            }
                        }
                    }
                }
                else
                {
                    foreach ((Vector3I clipboardPosition, ushort block) in clipboard.GetAllBlocks())
                    {
                        _WorldData.SetBlockAtPosition(position + clipboardPosition, (byte)block);
                        UpdateBlock(position + clipboardPosition);
                    }   
                }
                foreach (EyeOfRubissStructure.BGPartsData bgParts in clipboard.GetBGParts())
                {
                    AddBGParts(position + bgParts.GetPosition(), bgParts.BGPartsID, bgParts.Direction, collision: bgParts.Collision, effects: bgParts.Effects);
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
                                _WorldData.SetBlockAtPosition(position + clipboardPosition, Info.DQB2.BlockInfo.Get(clipboard.GetBlock(clipboardPosition).GetBlockID()).DQB1Block);
                                UpdateBlock(position + clipboardPosition);
                            }
                        }
                    }
                }
                else
                {
                    foreach ((Vector3I clipboardPosition, ushort block) in clipboard.GetAllBlocks())
                    {
                        byte blockId = Info.DQB2.BlockInfo.Get(block.GetBlockID()).DQB1Block;
                        if (blockId != Constants.BLOCK_AIR)
                        {
                            _WorldData.SetBlockAtPosition(position + clipboardPosition, blockId);
                            UpdateBlock(position + clipboardPosition);   
                        }
                    }
                }
                foreach (EyeOfRubissStructure.BGPartsData bgParts in clipboard.GetBGParts())
                {
                    ushort bgPartsId = Info.DQB2.BGPartsInfo.Get(bgParts.BGPartsID).DQB1BGParts;
                    if (bgPartsId != 0)
                    {
                        AddBGParts(position + bgParts.GetPosition(), bgParts.BGPartsID, bgParts.Direction, collision: bgParts.Collision, effects: bgParts.Effects);
                    }
                }
            }
        }
        #endregion

        public void UpdateBlock(Vector3I position)
        {
            if (_WorldData is null)
                return;

            _WorldEditorScene._VoxelTool.SetVoxel(position, VoxelGeneratorDQB1.GetVoxelAtPosition(_WorldData, position, _WorldEditorScene.ShowTerrain, _WorldEditorScene.ShowFluids));
            _WorldEditorScene._VoxelTool_PropShells.SetVoxel(position, VoxelGeneratorDQB1.GetVoxelAtPosition(_WorldData, position, showPartsBlock: true));
        }
    }
}