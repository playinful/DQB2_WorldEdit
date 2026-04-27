using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using EyeOfRubiss;
using EyeOfRubiss.Nodes;
using Godot;

[Serializable]
public class EyeOfRubissStructure()
{
    [JsonIgnore] public string Filename { get; set; }

    public byte SourceGame { get; set; }

    private Dictionary<Vector3I, ushort> _Blocks { get; set; } = [];
    private List<BGPartsData> _BGParts { get; set; } = [];

    // Used only for clipboard
    public int SizeX;
    public int SizeY;
    public int SizeZ;
    
    public Vector3I GetSize()
    {
        (Vector3I min, Vector3I max) = GetBounds();
        return max - min + Vector3I.One;
    }
    public Tuple<Vector3I, Vector3I> GetBounds()
    {
        Vector3I min = Vector3I.Zero;
        Vector3I max = Vector3I.Zero;

        foreach ((Vector3I position, ushort _) in _Blocks)
        {
            min = min.Min(position);
            max = max.Max(position);
        }
        foreach (BGPartsData bgParts in _BGParts)
        {
            Vector3I position = bgParts.GetPosition();
            min = min.Min(position);
            max = max.Max(position);
        }

        return new(min, max);
    }

    public static EyeOfRubissStructure From(BlueprintAssetDQB1 blueprint)
    {
        EyeOfRubissStructure structure = new()
        {
            SourceGame = 1,
        };

        foreach (BlueprintAssetDQB1.ObjectStruct obj in blueprint.Objects)
        {
            Vector3I position = new(obj.X, obj.Y, obj.Z);

            structure._Blocks[position] = obj.Data.Block;
            if (obj.Data.BGParts != 0)
            {
                EyeOfRubiss.Info.DQB1.BGPartsInfo bgPartsInfo = EyeOfRubiss.Info.DQB1.BGPartsInfo.Get(obj.Data.BGParts);
                structure._BGParts.Add(new BGPartsData(structure, obj.Data.BGParts, obj.X, obj.Y, obj.Z, obj.Data.Direction, bgPartsInfo.Collision, bgPartsInfo.Effects));
                if (obj.Data.Block == 0)
                    structure._Blocks[position] = bgPartsInfo.GetPartsBlockID();
            }
        }
        
        structure.CreateBGPartsPositionDictionary();
        structure.CreateBGPartsOverlapDictionary();

        return structure;
    }
    public static EyeOfRubissStructure From(DioramaHeaderAssetDQB1 header, DioramaDataAssetDQB1 data)
    {
        EyeOfRubissStructure structure = new()
        {
            SourceGame = 1,
        };

        List<ushort> blocks = [];
        for (int i = 0; i + 1 < data.Blocks.Length; i += 2)
        {
            if (data.Blocks[i] == 0 && data.Blocks[i + 1] == 0)
            {
                blocks = [];
                continue;
            }
            for (int j = 1; j <= data.Blocks[i]; j++)
            {
                blocks.Add((ushort)data.Blocks[i + 1]);
            }
        }

        for (int x = 0; x < header.SizeX; x++)
        {
            for (int y = 0; y < header.SizeY; y++)
            {
                for (int z = 0; z < header.SizeZ; z++)
                {
                    int index = z * header.SizeX * header.SizeY + x * header.SizeY + y;
                    if (index < blocks.Count && blocks[index] != 0)
                        structure._Blocks[new Vector3I(x, y, z)] = blocks[index];
                }
            }
        }

        foreach (DioramaDataAssetDQB1.BGPartsStruct bgparts in data.BGParts)
        {
            structure._BGParts.Add(new BGPartsData(structure, bgparts.BGPartsID, bgparts.X, bgparts.Y, bgparts.Z, bgparts.Direction));
        }
        
        structure.CreateBGPartsPositionDictionary();
        structure.CreateBGPartsOverlapDictionary();

        return structure;
    }
    public static EyeOfRubissStructure From(Blueprint blueprint)
    {
        EyeOfRubissStructure structure = new()
        {
            SourceGame = 2,
        };

        for (int x = 0; x < blueprint.SizeX; x++)
        {
            for (int y = 0; y < blueprint.SizeY; y++)
            {
                for (int z = 0; z < blueprint.SizeZ; z++)
                {
                    Blueprint.BlueprintBlockInstance block = blueprint.GetBlock(new Vector3I(x, y, z));

                    structure._Blocks[new Vector3I(x, y, z)] = block.BlockID.SetChiselShape(block.Chisel);

                    if (block.BGPartsID != 0)
                    {
                        structure._BGParts.Add(new BGPartsData(structure, block.BGPartsID, x, y, z, block.Direction));
                    }
                }
            }
        }
        
        structure.CreateBGPartsPositionDictionary();
        structure.CreateBGPartsOverlapDictionary();

        return structure;
    }
    public static EyeOfRubissStructure From(WorldData worldData)
    {
        EyeOfRubissStructure structure = new()
        {
            SourceGame = 1
        };

        foreach (WorldData.Chunk chunk in worldData.GetUsedChunks())
        {
            for (int x = 0; x < WorldData.CHUNK_SIZE; x++)
            {
                for (int y = 0; y < WorldData.WORLD_HEIGHT_BLOCKS; y++)
                {
                    for (int z = 0; z < WorldData.CHUNK_SIZE; z++)
                    {
                        Vector3I chunkPosition = new(x, y, z);
                        byte block = chunk.GetBlock(chunkPosition);
                        if (block != Constants.BLOCK_AIR)
                        {
                            structure._Blocks.Add(chunk.GetOrigin() + chunkPosition, block);
                        }
                    }
                }
            }
            foreach (WorldData.BGParts bgParts in chunk.GetAllBGParts())
            {
                if (bgParts.BGPartsID != 0)
                {
                    Vector3I position = bgParts.GetPosition();
                    structure._BGParts.Add(new BGPartsData(structure, bgParts.BGPartsID, position.X, position.Y, position.Z, bgParts.Direction, bgParts.Collision, bgParts.Effects));
                }
            }
        }

        return structure;
    }
    public static EyeOfRubissStructure From(WorldData worldData, Vector3I start, Vector3I end)
    {
        EyeOfRubissStructure structure = new()
        {
            SourceGame = 1
        };

        for (int x = 0; x <= end.X - start.X; x++)
        {
            for (int y = 0; y <= end.Y - start.Y; y++)
            {
                for (int z = 0; z <= end.Z - start.Z; z++)
                {
                    Vector3I position = new(x, y, z);
                    byte block = worldData.GetBlockAtPosition(position + start);
                    if (block != 0)
                    {
                        structure._Blocks[position] = block;
                    }
                    foreach (WorldData.BGParts bgParts in worldData.GetAllBGPartsAtPosition(position + start))
                    {
                        structure._BGParts.Add(new BGPartsData(structure, bgParts.BGPartsID, position.X, position.Y, position.Z, bgParts.Direction, bgParts.Collision, bgParts.Effects));
                    }
                }
            }
        }
        // TODO fluid
        
        structure.CreateBGPartsPositionDictionary();
        structure.CreateBGPartsOverlapDictionary();

        return structure;
    }
    public static EyeOfRubissStructure From(StageData stageData)
    {
        EyeOfRubissStructure structure = new()
        {
            SourceGame = 2
        };

        foreach (StageData.Chunk chunk in stageData.GetUsedChunks())
        {
            for (int x = 0; x < StageData.CHUNK_SIZE; x++)
            {
                for (int y = 0; y < StageData.WORLD_HEIGHT_BLOCKS; y++)
                {
                    for (int z = 0; z < StageData.CHUNK_SIZE; z++)
                    {
                        Vector3I chunkPosition = new(x, y, z);
                        StageData.BlockInstance block = chunk.GetBlock(chunkPosition);
                        if (block is not null && block.BlockID != Constants.BLOCK_AIR)
                        {
                            structure._Blocks.Add(chunk.GetOrigin() + chunkPosition, block.Value);
                        }
                    }
                }
            }
        }
        foreach (StageData.BGParts bgParts in stageData.GetBGParts())
        {
            if (bgParts.Exists())
            {
                Vector3I position = bgParts.GetPosition() - new Vector3I(1024, 0, 1024);
                structure._BGParts.Add(new BGPartsData(structure, bgParts.BGPartsID, position.X, position.Y, position.Z, bgParts.Direction, bgParts.Collision, bgParts.Effects, bgParts.ConnectingWindowRotation));
            }
        }

        return structure;
    }
    public static EyeOfRubissStructure From(StageData stageData, Vector3I start, Vector3I end)
    {
        EyeOfRubissStructure structure = new()
        {
            SourceGame = 2
        };

        for (int x = 0; x <= end.X - start.X; x++)
        {
            for (int y = 0; y <= end.Y - start.Y; y++)
            {
                for (int z = 0; z <= end.Z - start.Z; z++)
                {
                    Vector3I position = new(x, y, z);
                    ushort block = stageData.GetBlockAtPosition(position + start).Value;
                    if (block.GetBlockID() != 0)
                    {
                        structure._Blocks[position] = block;
                    }

                    foreach (StageData.BGParts bgParts in stageData.GetAllBGPartsAtPosition(position + start))
                    {
                        structure._BGParts.Add(new BGPartsData(structure, bgParts.BGPartsID, position.X, position.Y, position.Z, bgParts.Direction, bgParts.Collision, bgParts.Effects, bgParts.ConnectingWindowRotation));
                    }
                }
            }
        }
        
        structure.CreateBGPartsPositionDictionary();
        structure.CreateBGPartsOverlapDictionary();

        return structure;
    }
    public static EyeOfRubissStructure From(EyeOfRubissStructure structure)
    {
        EyeOfRubissStructure newStructure = new()
        {
            SourceGame = structure.SourceGame,
            _Blocks = structure.GetAllBlocks()
        };

        foreach (BGPartsData bgPartsData in structure._BGParts)
        {
            newStructure._BGParts.Add(new(newStructure, bgPartsData.BGPartsID, bgPartsData.X, bgPartsData.Y, bgPartsData.Z, bgPartsData.Direction, bgPartsData.Collision, bgPartsData.Effects, bgPartsData.ConnectingWindowRotation));
        }

        newStructure.CreateBGPartsPositionDictionary();
        structure.CreateBGPartsOverlapDictionary();

        return newStructure;
    }
    public static EyeOfRubissStructure From(EyeOfRubissStructure structure, Vector3I start, Vector3I end)
    {
        EyeOfRubissStructure newStructure = new()
        {
            SourceGame = structure.SourceGame
        };

        for (int x = 0; x <= end.X - start.X; x++)
        {
            for (int y = 0; y <= end.Y - start.Y; y++)
            {
                for (int z = 0; z <= end.Z - start.Z; z++)
                {
                    Vector3I position = new(x, y, z);
                    ushort block = structure.GetBlock(position + start);
                    if (block != 0)
                    {
                        newStructure._Blocks[position] = block;
                    }

                    foreach (BGPartsData bgParts in structure.GetAllBGPartsAtPosition(position + start))
                    {
                        newStructure._BGParts.Add(new BGPartsData(structure, bgParts.BGPartsID, position.X, position.Y, position.Z, bgParts.Direction, bgParts.Collision, bgParts.Effects, bgParts.ConnectingWindowRotation));
                    }
                }
            }
        }

        newStructure.CreateBGPartsPositionDictionary();
        structure.CreateBGPartsOverlapDictionary();

        return newStructure;
    }
    public static EyeOfRubissStructure From(EyeOfRubissStructureSerializable serializable)
    {
        EyeOfRubissStructure structure = new()
        {
            SourceGame = serializable.SourceGame
        };

        int x = 0;
        int y = 0;
        int z = 0;
        foreach (int[] item in serializable.Blocks)
        {
            ushort block = (ushort)item[0];

            for (int i = 0; i < item[1]; i++)
            {
                structure._Blocks[new(x, y, z)] = block;

                z++;
                if (z >= serializable.SizeZ)
                {
                    z = 0;
                    y++;
                    if (y >= serializable.SizeY)
                    {
                        y = 0;
                        x++;
                        if (x >= serializable.SizeX)
                        {
                            break;
                        }
                    }
                }
            }

            if (x >= serializable.SizeX)
            {
                break;
            }
        }

        foreach (BGPartsDataSerializable bgParts in serializable.BGParts)
        {
            structure._BGParts.Add(new(structure, bgParts));
        }

        structure.CreateBGPartsPositionDictionary();
        structure.CreateBGPartsOverlapDictionary();

        return structure;
    }

    public void Save(string path = null)
    {
        path ??= Filename;
        Filename = path;
        if (string.IsNullOrEmpty(path))
            return;
        
        using FileAccess fileAccess = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        {
            EyeOfRubissStructureSerializable serializable = new(this);

            fileAccess.StoreString(JsonSerializer.Serialize(serializable));
        }
    }
    public static EyeOfRubissStructure Load(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;
        if (!FileAccess.FileExists(path))
            return null;

        EyeOfRubissStructureSerializable serializable = JsonSerializer.Deserialize<EyeOfRubissStructureSerializable>(FileAccess.GetFileAsString(path));
        EyeOfRubissStructure structure = From(serializable);

        structure.Filename = path;
        return structure;
    }

    public BlueprintFileDQB2 ToBlueprint()
    {
        (Vector3I min, Vector3I max) = GetBounds();
        Vector3I size = max - min + Vector3I.One;
        if ((size.X * size.Y * size.Z) > 0x8000)
        {
            return null;
        }

        BlueprintFileDQB2 blueprintFile = BlueprintFileDQB2.CreateNew();
        Blueprint blueprint = blueprintFile.Blueprint;

        blueprint.Exists = true;
        blueprint.SizeX = (ushort)size.X;
        blueprint.SizeY = (ushort)size.Y;
        blueprint.SizeZ = (ushort)size.Z;

        if (SourceGame == 1)
        {
            foreach ((Vector3I position, ushort block) in _Blocks)
            {
                Vector3I adjustedPosition = position - min;
                Blueprint.BlueprintBlockInstance blockInstance = blueprint.GetBlock(adjustedPosition);
                blockInstance.BlockID = EyeOfRubiss.Info.DQB1.BlockInfo.Get((byte)block).DQB2Block;
            }
            foreach (BGPartsData bgParts in _BGParts)
            {
                Vector3I position = bgParts.GetPosition();
                Vector3I adjustedPosition = position - min;
                Blueprint.BlueprintBlockInstance blockInstance = blueprint.GetBlock(adjustedPosition);
                blockInstance.BGPartsID = EyeOfRubiss.Info.DQB1.BGPartsInfo.Get(bgParts.BGPartsID).DQB2BGParts;
                blockInstance.Direction = bgParts.Direction;
            }
        }
        else if (SourceGame == 2)
        {
            foreach ((Vector3I position, ushort block) in _Blocks)
            {
                Vector3I adjustedPosition = position - min;
                Blueprint.BlueprintBlockInstance blockInstance = blueprint.GetBlock(adjustedPosition);
                blockInstance.BlockID = block.GetBlockID();
                blockInstance.Chisel = block.GetChiselShape();
            }
            foreach (BGPartsData bgParts in _BGParts)
            {
                Vector3I position = bgParts.GetPosition();
                Vector3I adjustedPosition = position - min;
                Blueprint.BlueprintBlockInstance blockInstance = blueprint.GetBlock(adjustedPosition);
                blockInstance.BGPartsID = bgParts.BGPartsID;
                blockInstance.Direction = bgParts.Direction;
            }
        }
        else return null;

        return blueprintFile;
    }

    public ushort GetBlock(Vector3I position)
    {
        if (_Blocks.TryGetValue(position, out ushort block))
            return block;
        
        return 0;
    }
    public void SetBlock(Vector3I position, ushort block, bool? playerPlaced = null, ChiselShape? chiselShape = null)
    {
        if (block.GetBlockID() == 0)
        {
            _Blocks.Remove(position);
            return;
        }

        if (SourceGame == 2 && playerPlaced is bool _playerPlaced)
            block = block.SetPlayerPlaced(_playerPlaced);
        if (SourceGame == 2 && chiselShape is ChiselShape _chiselShape)
            block = block.SetChiselShape(_chiselShape);

        _Blocks[position] = block;
            
    }

    public Dictionary<Vector3I, ushort> GetAllBlocks()
    {
        return new Dictionary<Vector3I, ushort>(_Blocks);
    }

    public BGPartsData GetBGPartsAtPosition(Vector3I position)
    {
        if (_BGPartsPositionDictionary is null)
            CreateBGPartsPositionDictionary();
        
        return GetAllBGPartsAtPosition(position).FirstOrDefault();
    }
    public List<BGPartsData> GetAllBGPartsAtPosition(Vector3I position)
    {
        if (_BGPartsPositionDictionary is null)
            CreateBGPartsPositionDictionary();
        
        if (_BGPartsPositionDictionary.TryGetValue(position, out List<BGPartsData> value))
            return value;
        
        return [];
    }
    public BGPartsData GetOverlappingBGParts(Vector3I position)
    {
        if (_BGPartsOverlapDictionary is null)
            CreateBGPartsOverlapDictionary();
        
        return GetAllOverlappingBGParts(position).FirstOrDefault();
    }
    public List<BGPartsData> GetAllOverlappingBGParts(Vector3I position)
    {
        if (_BGPartsOverlapDictionary is null)
            CreateBGPartsOverlapDictionary();
        
        if (_BGPartsOverlapDictionary.TryGetValue(position, out List<BGPartsData> value))
            return value;
        
        return [];
    }
    public BGPartsData[] GetBGParts()
    {
        BGPartsData[] parts = new BGPartsData[_BGParts.Count];
        _BGParts.CopyTo(parts);
        return parts;
    }

    public BGPartsData AddBGParts(Vector3I position, ushort bgPartsId, byte direction, bool collision = false, bool effects = false, byte connectingWindowRotation = 0)
    {
        if (_BGPartsPositionDictionary is null)
            CreateBGPartsPositionDictionary();
        
        BGPartsData bgParts = new(this, bgPartsId, position.X, position.Y, position.Z, direction, collision, effects, connectingWindowRotation);
        _BGParts.Add(bgParts);
        AddBGPartsToPositionDictionary(bgParts);
        AddBGPartsToOverlapDictionary(bgParts);
        return bgParts;
    }
    public void RemoveBGParts(BGPartsData bgParts)
    {
        if (_BGParts.Remove(bgParts))
        {
            _BGPartsPositionDictionary.Remove(bgParts.GetPosition());
            RemoveBGPartsFromPositionDictionary(bgParts);
            RemoveBGPartsFromOverlapDictionary(bgParts);
        }
    }

    private Dictionary<Vector3I, List<BGPartsData>> _BGPartsPositionDictionary;
    protected void CreateBGPartsPositionDictionary()
    {
        _BGPartsPositionDictionary = [];

        foreach (BGPartsData bgParts in _BGParts)
        {
            AddBGPartsToPositionDictionary(bgParts);
        }
    }
    protected void AddBGPartsToPositionDictionary(BGPartsData bgParts)
    {
        if (_BGPartsPositionDictionary.TryGetValue(bgParts.GetPosition(), out List<BGPartsData> partsList))
        {
            partsList.Add(bgParts);
        }
        else
        {
            _BGPartsPositionDictionary.Add(bgParts.GetPosition(), [bgParts]);
        }
    }
    protected void RemoveBGPartsFromPositionDictionary(BGPartsData bgParts)
    {
        if (_BGPartsPositionDictionary.TryGetValue(bgParts.GetPosition(), out List<BGPartsData> list))
            list.Remove(bgParts);
    }
    private Dictionary<Vector3I, List<BGPartsData>> _BGPartsOverlapDictionary;
    protected void CreateBGPartsOverlapDictionary()
    {
        _BGPartsOverlapDictionary = [];
        foreach (BGPartsData bgParts in _BGParts)
        {
            AddBGPartsToOverlapDictionary(bgParts);
        }
    }
    protected void AddBGPartsToOverlapDictionary(BGPartsData bgParts)
    {
        (Vector3I start, Vector3I end) = bgParts.GetBounds();
        for (int x = start.X; x <= end.X; x++)
        {
            for (int y = start.Y; y <= end.Y; y++)
            {
                for (int z = start.Z; z <= end.Z; z++)
                {
                    Vector3I position = new(x, y, z);
                    if (_BGPartsOverlapDictionary.TryGetValue(position, out List<BGPartsData> propList))
                    {
                        propList.Add(bgParts);
                    }
                    else
                    {
                        _BGPartsOverlapDictionary.Add(position, [bgParts]);
                    }
                }
            }
        }
    }
    protected void RemoveBGPartsFromOverlapDictionary(BGPartsData bgParts)
    {
        (Vector3I start, Vector3I end) = bgParts.GetBounds();
        for (int x = start.X; x <= end.X; x++)
        {
            for (int y = start.Y; y <= end.Y; y++)
            {
                for (int z = start.Z; z <= end.Z; z++)
                {
                    if (_BGPartsOverlapDictionary.TryGetValue(new Vector3I(x, y, z), out List<BGPartsData> partsList) && partsList.Contains(bgParts))
                    {
                        partsList.Remove(bgParts);
                    }
                }
            }
        }
    }

    public class BGPartsData
    {
        [JsonIgnore] public readonly EyeOfRubissStructure Structure;
        private ushort _bgPartsID;
        public ushort BGPartsID
        {
            get => _bgPartsID;
            set
            {
                Structure.RemoveBGPartsFromOverlapDictionary(this);
                _bgPartsID = value;
                Structure.AddBGPartsToOverlapDictionary(this);
            }
        }
        private int _x;
        public int X
        {
            get => _x;
            set
            {
                Structure.RemoveBGPartsFromPositionDictionary(this);
                Structure.RemoveBGPartsFromOverlapDictionary(this);
                _x = value;
                Structure.AddBGPartsToPositionDictionary(this);
                Structure.AddBGPartsToOverlapDictionary(this);
            }
        }
        private int _y;
        public int Y
        {
            get => _y;
            set
            {
                Structure.RemoveBGPartsFromPositionDictionary(this);
                Structure.RemoveBGPartsFromOverlapDictionary(this);
                _y = value;
                Structure.AddBGPartsToPositionDictionary(this);
                Structure.AddBGPartsToOverlapDictionary(this);
            }
        }
        private int _z;
        public int Z
        {
            get => _z;
            set
            {
                Structure.RemoveBGPartsFromPositionDictionary(this);
                Structure.RemoveBGPartsFromOverlapDictionary(this);
                _z = value;
                Structure.AddBGPartsToPositionDictionary(this);
                Structure.AddBGPartsToOverlapDictionary(this);
            }
        }
        private byte _direction;
        public byte Direction
        {
            get => _direction;
            set
            {
                Structure.RemoveBGPartsFromOverlapDictionary(this);
                _direction = value;
                Structure.AddBGPartsToOverlapDictionary(this);
            }
        }

        public Vector3I GetPosition() => new(X, Y, Z);

        public bool Collision { get; set; }
        public bool Effects { get; set; }

        public byte ConnectingWindowRotation { get; set; } = 0;

        public Tuple<Vector3I, Vector3I> GetBounds()
        {
            Vector3I dimensions = Vector3I.Zero;

            if (Structure.SourceGame == 1)
            {
                dimensions = EyeOfRubiss.Info.DQB1.BGPartsInfo.Get(BGPartsID).GetDimensions() - Vector3I.One;
            }
            else if (Structure.SourceGame == 2)
            {
                dimensions = EyeOfRubiss.Info.DQB2.BGPartsInfo.Get(BGPartsID).GetDimensions() - Vector3I.One;
            }

            Vector3I position = GetPosition();
            int x1 = position.X;
            int y1 = position.Y;
            int z1 = position.Z;
            int y2 = y1 + dimensions.Y;
            int x2 = x1;
            int z2 = z1;
            switch (Direction)
            {
                case 0:
                    x2 += dimensions.X;
                    z2 -= dimensions.Z;
                    break;
                case 1:
                    x2 -= dimensions.Z;
                    z2 -= dimensions.X;
                    break;
                case 2:
                    x2 -= dimensions.X;
                    z2 += dimensions.Z;
                    break;
                case 3:
                    x2 += dimensions.Z;
                    z2 += dimensions.X;
                    break;
            }

            return new(
                new Vector3I(Math.Min(x1, x2), Math.Min(y1, y2), Math.Min(z1, z2)),
                new Vector3I(Math.Max(x1, x2), Math.Max(y1, y2), Math.Max(z1, z2))
            );
        }
    
        public BGPartsData(EyeOfRubissStructure structure, ushort bgPartsId, int x, int y, int z, byte direction = 0, bool collision = false, bool effects = false, byte connectingWindowRotation = 0)
        {
            Structure = structure;
            _bgPartsID = bgPartsId;
            _x = x;
            _y = y;
            _z = z;
            _direction = direction;
            Collision = collision;
            Effects = effects;
            ConnectingWindowRotation = connectingWindowRotation;
        }
        public BGPartsData(EyeOfRubissStructure structure, BGPartsDataSerializable serializable)
        {
            Structure = structure;
            _bgPartsID = serializable.BGPartsID;
            _x = serializable.X;
            _y = serializable.Y;
            _z = serializable.Z;
            _direction = serializable.Direction;
            Collision = serializable.Collision;
            Effects = serializable.Effects;
            ConnectingWindowRotation = serializable.ConnectingWindowRotation;
        }
    }

    public class EyeOfRubissStructureSerializable
    {
        public string Version { get; set; } = "1.0";

        public byte SourceGame { get; set; }
        public int SizeX { get; set; }
        public int SizeY { get; set; }
        public int SizeZ { get; set; }
        public List<int[]> Blocks { get; set; }
        public List<BGPartsDataSerializable> BGParts { get; set; }
        
        [JsonConstructor] public EyeOfRubissStructureSerializable() {}
        public EyeOfRubissStructureSerializable(EyeOfRubissStructure structure)
        {
            SourceGame = structure.SourceGame;
            
            (Vector3I min, Vector3I max) = structure.GetBounds();
            
            SizeX = max.X - min.X + 1;
            SizeY = max.Y - min.Y + 1;
            SizeZ = max.Z - min.Z + 1;
            
            // Blocks = new ushort[SizeX * SizeY * SizeZ];
            // for (int x = min.X; x <= max.X; x++)
            // {
            //     for (int y = min.Y; y <= max.Y; y++)
            //     {
            //         for (int z = min.Z; z <= max.Z; z++)
            //         {
            //             ushort block = structure.GetBlock(new Vector3I(x, y, z));
            //             Blocks[(x - min.X) + ((y - min.Y) * SizeX) + ((z - min.Z) * SizeX * SizeY)] = block;
            //         }
            //     }
            // }

            Blocks = [];
            int[] lastBlock = null;
            for (int x = min.X; x <= max.X; x++)
            {
                for (int y = min.Y; y <= max.Y; y++)
                {
                    for (int z = min.Z; z <= max.Z; z++)
                    {
                        ushort block = structure.GetBlock(new Vector3I(x, y, z));
                        if (lastBlock is null || lastBlock[0] != block)
                        {
                            lastBlock = [block, 1];
                            Blocks.Add(lastBlock);
                        }
                        else
                        {
                            lastBlock[1] = lastBlock[1] + 1;
                        }
                    }
                }
            }

            BGParts = [];
            foreach (BGPartsData bgParts in structure._BGParts)
            {
                BGParts.Add(new BGPartsDataSerializable(bgParts, min));
            }
        }
    }
    public class BGPartsDataSerializable
    {
        [JsonPropertyName("ID")] public ushort BGPartsID { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        [JsonPropertyName("Dir")] public byte Direction { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)][JsonPropertyName("Col")]
        public bool Collision { get; set; } = false;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)][JsonPropertyName("Eff")]
        public bool Effects { get; set; } = false;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)][JsonPropertyName("Rot")]
        public byte ConnectingWindowRotation { get; set; } = 0;

        [JsonConstructor] public BGPartsDataSerializable() {}
        public BGPartsDataSerializable(BGPartsData bgParts, Vector3I offset)
        {
            BGPartsID = bgParts.BGPartsID;
            X = bgParts.X - offset.X;
            Y = bgParts.Y - offset.Y;
            Z = bgParts.Z - offset.Z;
            Direction = bgParts.Direction;
            Collision = bgParts.Collision;
            Effects = bgParts.Effects;
            ConnectingWindowRotation = bgParts.ConnectingWindowRotation;
        }
    }
}