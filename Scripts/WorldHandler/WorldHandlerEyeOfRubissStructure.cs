using System;
using EyeOfRubiss;
using Godot;
using EyeOfRubiss.Scenes;
using System.Runtime.CompilerServices;
using Microsoft.VisualBasic;
using System.Text;
using System.Reflection;
using System.ComponentModel;
using EyeOfRubiss.Info;
using EyeOfRubiss.Info.DQB1;
using EyeOfRubiss.Info.DQB2;
using System.Collections.Generic;

public class WorldHandlerEyeOfRubissStructure(WorldEditorScene worldEditorScene) : WorldHandler(worldEditorScene)
{
    public EyeOfRubissStructure Structure;

    private bool _BGPartsLoaded = false;

    public override string GetDebugInfo(Vector3I position)
    {
        if (Structure is null)
            return "Targeted block: None";
        
        if (Structure.SourceGame == 1)
        {
            byte block = (byte)Structure.GetBlock(position);

            EyeOfRubiss.Info.DQB1.BlockInfo blockInfo = EyeOfRubiss.Info.DQB1.BlockInfo.Get(block);

            string output = $"Targeted block: {blockInfo.Name} [{blockInfo.ID}]" +
                $"\nX: {position.X}, Y: {position.Y}, Z: {position.Z}";

            if (Structure.GetOverlappingBGParts(position) is EyeOfRubissStructure.BGPartsData bgparts)
            {
                EyeOfRubiss.Info.DQB1.BGPartsInfo partsInfo = EyeOfRubiss.Info.DQB1.BGPartsInfo.Get(bgparts.BGPartsID);

                output += $"\nTargeted object: {partsInfo.Name} [{partsInfo.ID}]";
                output += $"\nDirection: {Util.DirectionToString(bgparts.Direction)}";
            }

            return output.ToString();
        }
        else if (Structure.SourceGame == 2)
        {
            ushort block = Structure.GetBlock(position);

            EyeOfRubiss.Info.DQB2.BlockInfo blockInfo = EyeOfRubiss.Info.DQB2.BlockInfo.Get(block.GetBlockID());

            string output = $"Targeted block: {blockInfo.Name} [{blockInfo.ID}]" + 
                $"\nX: {position.X}, Y: {position.Y}, Z: {position.Z}" + 
                $"\nPlaced by Builder: {block.GetPlayerPlaced()}" +
                $"\nShape: {block.GetChiselShape()}";

            if (Structure.GetOverlappingBGParts(position) is EyeOfRubissStructure.BGPartsData bgparts)
            {
                EyeOfRubiss.Info.DQB2.BGPartsInfo partsInfo = EyeOfRubiss.Info.DQB2.BGPartsInfo.Get(bgparts.BGPartsID);

                output += $"\nTargeted object: {partsInfo.Name} [{partsInfo.ID}]";
                output += $"\nDirection: {Util.DirectionToString(bgparts.Direction)}";
            }

            return output.ToString();
        }

        return "Targeted block: UNKNOWN";
    }

    #region Scene setup
    public void Load(EyeOfRubissStructure structure)
    {
        Structure = structure;
        ReloadTerrain();
        if (_WorldEditorScene.ShowBGParts)
            GenerateBGParts();
    }
    public void Unload()
    {
        Structure = null;
        _WorldEditorScene._VoxelTerrain.Generator = null;
        DestroyBGParts();
    }

    public void GenerateBGParts()
    {
        DestroyBGParts();
        if (Structure.SourceGame == 1)
        {
		    foreach (EyeOfRubissStructure.BGPartsData parts in Structure.GetBGParts())
		    {
                EyeOfRubiss.Info.DQB1.BGPartsInfo partsInfo = EyeOfRubiss.Info.DQB1.BGPartsInfo.Get(parts.BGPartsID);
		    	if (partsInfo.Mesh is int meshId)
		    	{
		    		_WorldEditorScene._BGPartsGridManager.AddCellItem(parts.GetPosition(), meshId, Util.GridMapRotationFromDirection(parts.Direction));
		    	}
		    }
        }
        else if (Structure.SourceGame == 2)
        {
		    foreach (EyeOfRubissStructure.BGPartsData parts in Structure.GetBGParts())
		    {
                EyeOfRubiss.Info.DQB2.BGPartsInfo partsInfo = EyeOfRubiss.Info.DQB2.BGPartsInfo.Get(parts.BGPartsID);
		    	if (partsInfo.Mesh is int meshId)
		    	{
		    		_WorldEditorScene._BGPartsGridManager.AddCellItem(parts.GetPosition(), meshId, Util.GridMapRotationFromDirection(parts.Direction, parts.ConnectingWindowRotation));
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
    
    public void ReloadTerrain()
    {
        if (Structure is not null)
        {
            _WorldEditorScene._VoxelTerrain.Generator = new VoxelGeneratorEyeOfRubissStructure(Structure, showTerrain: _WorldEditorScene.ShowTerrain, showFluid: _WorldEditorScene.ShowFluids);
            _WorldEditorScene._VoxelTerrain_PropShells.Generator = new VoxelGeneratorEyeOfRubissStructure(Structure, showPartsBlock: true);
        }
        else
        {
            _WorldEditorScene._VoxelTerrain.Generator = null;
            _WorldEditorScene._VoxelTerrain_PropShells.Generator = null;
        }
    }
    #endregion

    #region Display options
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
            GenerateBGParts();
    }
    #endregion
    
    #region Stage editing
    public void RemoveBGParts(EyeOfRubissStructure.BGPartsData bgParts)
    {
        (Vector3I start, Vector3I end) = bgParts.GetBounds();
        Structure.RemoveBGParts(bgParts);
        for (int x = start.X; x <= end.X; x++)
        {
            for (int y = start.Y; y <= end.Y; y++)
            {
                for (int z = start.Z; z <= end.Z; z++)
                {
                    Vector3I position = new(x, y, z);
                    Structure.SetBlock(position, 0);
                    UpdateBlock(position);
                }
            }
        }
    }

	public void ChangePartsBlock(Vector3I position, PartsType propShell)
	{
        if (Structure.SourceGame != 2)
            return;

		EyeOfRubiss.Info.DQB2.BlockInfo blockInfo = EyeOfRubiss.Info.DQB2.BlockInfo.Get(Structure.GetBlock(position).GetBlockID());
        if (blockInfo.GetPartsType() != propShell)
        {
		    Structure.SetBlock(position, FluidConverter.Convert(blockInfo.FluidType, blockInfo.FluidLevel, propShell));
            UpdateBlock(position);   
        }
	}

    public override void ReplaceBlock(int replace, int with, Vector3I? from = null, Vector3I? to = null)
    {
        if (Structure is null)
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
                        if (Structure.GetBlock(position).GetBlockID() == replace)
                        {
                            Structure.SetBlock(position, (ushort)with);
                            UpdateBlock(position);
                        }
                    }
                }
            }
        }
        else
        {
            foreach ((Vector3I position, ushort block) in Structure.GetAllBlocks())
            {
                if (block.GetBlockID() == replace)
                {
                    Structure.SetBlock(position, (ushort)with);
                }
            }
            ReloadTerrain();
        }
    }

    public override bool CanCopy()
    {
        return Structure is not null;
    }
    public override EyeOfRubissStructure DoCopy(Vector3I start, Vector3I end)
    {
        if (!CanCopy())
            return null;
        
        return EyeOfRubissStructure.From(Structure, start, end);
    }
    
    public void UpdateBlock(Vector3I position)
    {
        if (Structure is null)
            return;
        
        _WorldEditorScene._VoxelTool.SetVoxel(position, VoxelGeneratorEyeOfRubissStructure.GetVoxelAtPosition(Structure, position, showTerrain: _WorldEditorScene.ShowTerrain, showFluid: _WorldEditorScene.ShowFluids));
        _WorldEditorScene._VoxelTool_PropShells.SetVoxel(position, VoxelGeneratorEyeOfRubissStructure.GetVoxelAtPosition(Structure, position, showPartsBlock: true));

        if (Structure.SourceGame == 1)
        {
            _WorldEditorScene._BGPartsGridManager.ClearCellItem(position);
            foreach (EyeOfRubissStructure.BGPartsData bgParts in Structure.GetAllBGPartsAtPosition(position))
            {
                EyeOfRubiss.Info.DQB1.BGPartsInfo bgPartsInfo = EyeOfRubiss.Info.DQB1.BGPartsInfo.Get(bgParts.BGPartsID);
		    	if (bgPartsInfo.Mesh is int meshId)
		    	{
		    		_WorldEditorScene._BGPartsGridManager.AddCellItem(bgParts.GetPosition(), meshId, Util.GridMapRotationFromDirection(bgParts.Direction));
		    	}
            }
        }
        else if (Structure.SourceGame == 2)
        {
            _WorldEditorScene._BGPartsGridManager.ClearCellItem(position);
            foreach (EyeOfRubissStructure.BGPartsData bgParts in Structure.GetAllBGPartsAtPosition(position))
            {
                EyeOfRubiss.Info.DQB2.BGPartsInfo bgPartsInfo = EyeOfRubiss.Info.DQB2.BGPartsInfo.Get(bgParts.BGPartsID);
		    	if (bgPartsInfo.Mesh is int meshId)
		    	{
		    		_WorldEditorScene._BGPartsGridManager.AddCellItem(bgParts.GetPosition(), meshId, Util.GridMapRotationFromDirection(bgParts.Direction, bgParts.ConnectingWindowRotation));
		    	}
            }
        }
    }
    #endregion

    #region Brush methods
    public override void DoSetBlock(Vector3I position, int block)
    {
        if (Structure is null)
            return;
        
        if (
            (Structure.SourceGame == 1 && EyeOfRubiss.Info.DQB1.BlockInfo.Get((byte)block).PartsType == PartsType.None) ||
            (Structure.SourceGame == 2 && EyeOfRubiss.Info.DQB2.BlockInfo.Get((ushort)block).GetPartsType() == PartsType.None)
        )
        {
            List<EyeOfRubissStructure.BGPartsData> overlapping = Structure.GetAllOverlappingBGParts(position);
            foreach (EyeOfRubissStructure.BGPartsData bgParts in overlapping)
            {
                RemoveBGParts(bgParts);
            }
        }

        Structure.SetBlock(position, (ushort) block);
        UpdateBlock(position);
    }
    public override void DoSetBGParts(Vector3I position, int bgPartsId, PartsType? partsBlock = null, bool collision = true, bool effects = true, bool unbreakable = false, byte size = 0)
    {
        if (Structure is null)
            return;

        // Set prop blocks
        if (Structure.SourceGame == 1)
        {
            EyeOfRubiss.Info.DQB1.BGPartsInfo bgPartsInfo = EyeOfRubiss.Info.DQB1.BGPartsInfo.Get((ushort)bgPartsId);

            EyeOfRubissStructure.BGPartsData bgParts = Structure.AddBGParts(
                position,
                (ushort)bgPartsId,
                _WorldEditorScene.GetBGPartsPlacementDirection(),
                bgPartsInfo.Collision && collision,
                bgPartsInfo.Effects && effects,
                unbreakable
            );

            byte block;
            if (partsBlock is PartsType partsType)
            {
                block = EyeOfRubiss.Info.DQB1.BGPartsInfo.GetPartsBlockID(partsType);
            }
            else
            {
                block = EyeOfRubiss.Info.DQB1.BGPartsInfo.Get((ushort)bgPartsId).GetPartsBlockID();
            }

            (Vector3I start, Vector3I end) = bgParts.GetBounds();
            for (int x = start.X; x <= end.X; x++)
            {
                for (int y = start.Y; y <= end.Y; y++)
                {
                    for (int z = start.Z; z <= end.Z; z++)
                    {
                        Vector3I positionB = new(x, y, z);
                        Structure.SetBlock(positionB, block);
                        UpdateBlock(positionB);
                    }
                }
            }
        }
        else if (Structure.SourceGame == 2)
        {
            EyeOfRubiss.Info.DQB2.BGPartsInfo bgPartsInfo = EyeOfRubiss.Info.DQB2.BGPartsInfo.Get((ushort)bgPartsId);

            EyeOfRubissStructure.BGPartsData bgParts = Structure.AddBGParts(
                position,
                (ushort)bgPartsId,
                _WorldEditorScene.GetBGPartsPlacementDirection(),
                bgPartsInfo.Collision && collision,
                bgPartsInfo.Effects && effects,
                unbreakable,
                size
            );

            EyeOfRubiss.Info.DQB2.BGPartsInfo partsInfo = EyeOfRubiss.Info.DQB2.BGPartsInfo.Get((ushort)bgPartsId);
            (Vector3I start, Vector3I end) = bgParts.GetBounds();
            for (int x = start.X; x <= end.X; x++)
            {
                for (int y = start.Y; y <= end.Y; y++)
                {
                    for (int z = start.Z; z <= end.Z; z++)
                    {
                        Vector3I positionB = new(x, y, z);
                        ChangePartsBlock(positionB, partsBlock ?? partsInfo.Block);
                    }
                }
            }
        }

        UpdateBlock(position);
    }
    public override void DoSetFluid(Vector3I position, int fluidType, int fluidLevel)
    {
        if (Structure is null || Structure.SourceGame != 2)
            return;
        
        ushort block = Structure.GetBlock(position);
        EyeOfRubiss.Info.DQB2.BlockInfo blockInfo = EyeOfRubiss.Info.DQB2.BlockInfo.Get(block.GetBlockID());

        ushort newBlock = FluidConverter.Convert((FluidType)fluidType, (FluidLevel)fluidLevel, blockInfo.GetPartsType());
        Structure.SetBlock(position, newBlock);
        UpdateBlock(position);
    }

    public override void DoEraser(Vector3I position)
    {
        DoSetBlock(position, 0);
    }

    public override void DoChisel(Vector3I position, ChiselShape shape)
    {
        if (Structure is null || Structure.SourceGame != 2)
            return;
        
        ushort block = Structure.GetBlock(position);
        Structure.SetBlock(position, block.SetChiselShape(shape));
    }

    public override void DoPaste(Vector3I position, EyeOfRubissStructure clipboard, bool pasteAir)
    {
        if (Structure is null)
            return;
        
        if (Structure.SourceGame == clipboard.SourceGame)
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
                            DoSetBlock(position + clipboardPosition, clipboard.GetBlock(clipboardPosition));
                        }
                    }
                }
            }
            else
            {
                foreach ((Vector3I clipboardPosition, ushort block) in clipboard.GetAllBlocks())
                {
                    DoSetBlock(position + clipboardPosition, block);
                }   
            }
            foreach (EyeOfRubissStructure.BGPartsData bgParts in clipboard.GetBGParts())
            {
                Structure.AddBGParts(position + bgParts.GetPosition(), bgParts.BGPartsID, bgParts.Direction, bgParts.Collision, bgParts.Effects, bgParts.Unbreakable, bgParts.Size, bgParts.ConnectingWindowRotation);
                UpdateBlock(position + bgParts.GetPosition());
            }
        }
        else if (Structure.SourceGame == 2 && clipboard.SourceGame == 1)
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
                            DoSetBlock(position + clipboardPosition, EyeOfRubiss.Info.DQB1.BlockInfo.Get((byte)clipboard.GetBlock(clipboardPosition)).DQB2Block);
                        }
                    }
                }
            }
            else
            {
                foreach ((Vector3I clipboardPosition, ushort block) in clipboard.GetAllBlocks())
                {
                    ushort blockId = EyeOfRubiss.Info.DQB1.BlockInfo.Get((byte)block).DQB2Block;
                    if (blockId != 0)
                    {
                        DoSetBlock(position + clipboardPosition, blockId);
                    }
                }   
            }
            foreach (EyeOfRubissStructure.BGPartsData bgParts in clipboard.GetBGParts())
            {
                ushort bgPartsId = EyeOfRubiss.Info.DQB1.BGPartsInfo.Get(bgParts.BGPartsID).DQB2BGParts;
                if (bgPartsId != 0)
                {
                    Structure.AddBGParts(position + bgParts.GetPosition(), bgPartsId, bgParts.Direction, bgParts.Collision, bgParts.Effects);
                    UpdateBlock(position + bgParts.GetPosition());
                }
            }
        }
        else if (Structure.SourceGame == 1 && clipboard.SourceGame == 2)
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
                            DoSetBlock(position + clipboardPosition, EyeOfRubiss.Info.DQB2.BlockInfo.Get(clipboard.GetBlock(clipboardPosition).GetBlockID()).DQB1Block);
                        }
                    }
                }
            }
            else
            {
                foreach ((Vector3I clipboardPosition, ushort block) in clipboard.GetAllBlocks())
                {
                    ushort blockId = EyeOfRubiss.Info.DQB2.BlockInfo.Get(block.GetBlockID()).DQB1Block;
                    if (blockId != 0)
                    {
                        DoSetBlock(position + clipboardPosition, blockId);
                    }
                }
            }
            foreach (EyeOfRubissStructure.BGPartsData bgParts in clipboard.GetBGParts())
            {
                ushort bgPartsId = EyeOfRubiss.Info.DQB2.BGPartsInfo.Get(bgParts.BGPartsID).DQB1BGParts;
                if (bgPartsId != 0)
                {
                    Structure.AddBGParts(position + bgParts.GetPosition(), bgPartsId, bgParts.Direction, bgParts.Collision, bgParts.Effects);
                    UpdateBlock(position + bgParts.GetPosition());
                }
            }
        }
    }
    #endregion

    #region Tools
    public override void DeleteAllBGParts()
    {
        if (Structure is null)
            return;
        
        HashSet<Vector3I> partsBlocks = [];
        foreach (EyeOfRubissStructure.BGPartsData bgParts in Structure.GetAllBGParts())
        {
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
            Structure.RemoveBGParts(bgParts);
        }
        foreach (Vector3I position in partsBlocks)
        {
            ChangePartsBlock(position, PartsType.None);
        }

        _WorldEditorScene._BGPartsGridManager.Clear();
    }

    public override void FixPropShells()
    {
        if (Structure is null)
            return;
        
        if (Structure.SourceGame == 1)
        {
            foreach (EyeOfRubissStructure.BGPartsData bgParts in Structure.GetAllBGParts())
            {
                (Vector3I start, Vector3I end) = bgParts.GetBounds();
                for (int x = start.X; x <= end.X; x++)
                {
                    for (int y = start.Y; y <= end.Y; y++)
                    {
                        for (int z = start.Z; z <= end.Z; z++)
                        {
                            Vector3I position = new(x, y, z);
                            EyeOfRubiss.Info.DQB1.BlockInfo blockInfo = EyeOfRubiss.Info.DQB1.BlockInfo.Get((byte)Structure.GetBlock(position));
                            if (blockInfo.PartsType == PartsType.None)
                            {
                                Structure.SetBlock(position, EyeOfRubiss.Info.DQB1.BGPartsInfo.Get(bgParts.BGPartsID).GetPartsBlockID());
                                UpdateBlock(position);
                            }
                        }
                    }
                }
            }   
        }
        else if (Structure.SourceGame == 2)
        {
            foreach (EyeOfRubissStructure.BGPartsData bgParts in Structure.GetAllBGParts())
            {
                (Vector3I start, Vector3I end) = bgParts.GetBounds();
                for (int x = start.X; x <= end.X; x++)
                {
                    for (int y = start.Y; y <= end.Y; y++)
                    {
                        for (int z = start.Z; z <= end.Z; z++)
                        {
                            Vector3I position = new(x, y, z);
                            EyeOfRubiss.Info.DQB2.BlockInfo blockInfo = EyeOfRubiss.Info.DQB2.BlockInfo.Get(Structure.GetBlock(position).GetBlockID());
                            if (blockInfo.GetPartsType() == PartsType.None)
                            {
                                ChangePartsBlock(position, EyeOfRubiss.Info.DQB2.BGPartsInfo.Get(bgParts.BGPartsID).Block);
                            }
                        }
                    }
                }
            }
        }
    }

    public override void FixFakeBlocks()
    {
        if (Structure is null || Structure.SourceGame != 2)
            return;
        
        foreach (EyeOfRubissStructure.BGPartsData bgParts in Structure.GetAllBGParts())
        {
            EyeOfRubiss.Info.DQB2.BGPartsInfo bgPartsInfo = EyeOfRubiss.Info.DQB2.BGPartsInfo.Get(bgParts.BGPartsID);
            if (bgPartsInfo.IsFakeBlock())
            {
                Vector3I position = bgParts.GetPosition();
                Structure.SetBlock(position, bgPartsInfo.GetFakeBlockID());
                Structure.RemoveBGParts(bgParts);
                UpdateBlock(position);
            }
        }
    }
    #endregion
}