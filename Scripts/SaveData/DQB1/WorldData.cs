using System;
using Godot;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Linq;
using System.Runtime.CompilerServices;
using EyeOfRubiss.Info.DQB1;

namespace EyeOfRubiss
{
    public class WorldData : SaveData
    {
        public const int HEADER_LENGTH = 0;

        public const int CHUNK_SIZE = 32;

        public const int WORLD_SIZE_CHUNKS = 48;
        public const int WORLD_SIZE_BLOCKS = WORLD_SIZE_CHUNKS * CHUNK_SIZE;
        public const int WORLD_HEIGHT_BLOCKS = 32;

        public static bool TryLoad(string path, out WorldData result)
        {
            result = null;
            WorldData worldData = new();
            if (worldData._TryLoad(path, HEADER_LENGTH))
            {
                result = worldData;
                worldData.CreateBGPartsPositionDictionary();
                worldData.CreateBGPartsOverlapDictionary();
                return true;
            }
            else return false;
        }

        public static bool PositionIsInBounds(Vector3I position)
        {
            if (position.Y < 0 || position.Y >= WORLD_HEIGHT_BLOCKS)
                return false;
            if (position.Z < 0 || position.Z >= WORLD_SIZE_BLOCKS || position.X < 0 || position.X >= WORLD_SIZE_BLOCKS)
                return false;

            return true;
        }
        public static Vector3I PositionToDataPosition(Vector3I position)
        {
            int chunkIndex = PositionToChunkIndex(position);
            int layer = position.Y;
            int tile = position.X % CHUNK_SIZE + (position.Z % CHUNK_SIZE * CHUNK_SIZE);

            return new Vector3I(chunkIndex, layer, tile);
        }

        public static ushort PositionToChunkIndex(Vector3I position)
        {
            int x = position.X / CHUNK_SIZE;
            int z = position.Z / CHUNK_SIZE;
            return (ushort)(x + (z * WORLD_SIZE_CHUNKS));
        }
        public static Vector3I ChunkIndexToPosition(int chunkIndex)
        {
            return new(chunkIndex % WORLD_SIZE_CHUNKS * CHUNK_SIZE, 0, chunkIndex / WORLD_SIZE_CHUNKS * CHUNK_SIZE);
        }

        public byte GetBlockAtPosition(Vector3I position)
        {
            if (!PositionIsInBounds(position))
                return 0;
                
            int chunkPosX = position.X % CHUNK_SIZE;
            int chunkPosY = position.Y;
            int chunkPosZ = position.Z % CHUNK_SIZE;

            int tile = (chunkPosY * CHUNK_SIZE * CHUNK_SIZE) + (chunkPosZ * CHUNK_SIZE) + (chunkPosX);

            Chunk chunk = GetChunk(PositionToChunkIndex(position));

            if (chunk is null || !chunk.IsUsed())
                return 0;

            return chunk.GetBlock(tile);
        }
        public bool SetBlockAtPosition(Vector3I position, byte blockId)
        {
            if (!PositionIsInBounds(position))
                return false;

            Chunk chunk = GetChunk(PositionToChunkIndex(position));

            // TODO create chunks

            int chunkPosX = position.X % CHUNK_SIZE;
            int chunkPosY = position.Y;
            int chunkPosZ = position.Z % CHUNK_SIZE;

            chunk.SetBlock((chunkPosY * CHUNK_SIZE * CHUNK_SIZE) + (chunkPosZ * CHUNK_SIZE) + (chunkPosX), blockId);

            return true;
        }
        
        public BGParts AddBGParts(Vector3I position, ushort bgPartsId, byte direction = 0, bool collision = true, bool effects = true)
        {
            if (!PositionIsInBounds(position))
                return null;

            Chunk chunk = GetChunk(PositionToChunkIndex(position));

            if (!chunk.IsUsed())
                return null;
            
            if (chunk.GetFirstUnusedBGParts() is not BGParts bgParts)
                return null;

            int chunkPosX = position.X % CHUNK_SIZE;
            int chunkPosY = position.Y;
            int chunkPosZ = position.Z % CHUNK_SIZE;

            bgParts.X = (byte)chunkPosX;
            bgParts.Y = (byte)chunkPosY;
            bgParts.Z = (byte)chunkPosZ;
            bgParts.Direction = direction;
            bgParts.BGPartsID = bgPartsId;

            return bgParts;
        }

        public FluidType GetFluidAtPosition(Vector3I position)
        {
            if (position.Y < 3)
                return GetChunk(PositionToChunkIndex(position)).GetFluid(new Vector2I(position.X % 32, position.Z % 32));
            else
                return FluidType.Air;
        }
        
        public Chunk GetChunk(int index)
        {
            if (index < 0 || index >= Chunk.MAXIMUM)
                throw new IndexOutOfRangeException();

            return new Chunk(this, index);
        }
        public IEnumerable<Chunk> GetChunks(int index = 0, int count = Chunk.MAXIMUM)
        {
            if (index < 0 || index >= Chunk.MAXIMUM)
                throw new IndexOutOfRangeException();

            for (int i = 0; i < count && i + index < Chunk.MAXIMUM; i++)
                yield return GetChunk(i + index);
        }
        public IEnumerable<Chunk> GetUsedChunks()
        {
            return GetChunks().Where(chunk => chunk.IsUsed());
        }

        public class Chunk(WorldData saveData, int index)
        {
            public const int START_ADDRESS_METADATA = 0;
            public const int START_ADDRESS_BLOCKDATA = 0x120C;
            public const int START_ADDRESS_BGPARTSDATA = 0x169020C;
            public const int START_ADDRESS_FLUIDDATA = 0x15E_140C;
            public const int LENGTH_BGPARTSDATA = BGParts.LENGTH * BGParts.MAXIMUM + 4;
            public const int LENGTH_METADATA = 2;
            public const int MAXIMUM = WORLD_SIZE_CHUNKS * WORLD_SIZE_CHUNKS;

            public const int LENGTH_BLOCKDATA = LAYER_LENGTH * WORLD_HEIGHT_BLOCKS;
            public const int LENGTH_FLUIDDATA = LAYER_LENGTH;
            public const int LAYER_LENGTH = CHUNK_SIZE * CHUNK_SIZE;

            public WorldData SaveData { get; set; } = saveData;
            public int Index { get; set; } = index;

            public ushort ChunkID { get => SaveData.GetUInt16(START_ADDRESS_METADATA + Index * LENGTH_METADATA); set => SaveData.SetUInt16(START_ADDRESS_METADATA + Index * LENGTH_METADATA, value); }// => SaveData.GetChunkIdByIndex(Index);
            public int BGPartsCount { get => SaveData.GetInt32(GetBGPartsAddress() - 4); set => SaveData.SetInt32(GetBGPartsAddress() - 4, value); }

            public Vector3I GetOrigin() => ChunkIndexToPosition(Index);

            public bool IsUsed() => ChunkID != ushort.MaxValue;

            public int GetMetadataAddress() => START_ADDRESS_METADATA + LENGTH_METADATA * Index;
            public int GetBlockAddress() => START_ADDRESS_BLOCKDATA + LENGTH_BLOCKDATA * ChunkID;
            public int GetBGPartsAddress() => START_ADDRESS_BGPARTSDATA + LENGTH_BGPARTSDATA * ChunkID + 4;
            public int GetFluidAddress() => START_ADDRESS_FLUIDDATA + LENGTH_FLUIDDATA * ChunkID;

            public Span<byte> GetData() => SaveData.GetBytes(GetBlockAddress(), LENGTH_BLOCKDATA);

            public byte GetBlock(int tile)
            {
                return IsUsed() ? SaveData.GetByte(GetBlockAddress() + tile) : (byte)0;
            }
            public byte GetBlock(Vector3I position)
            {
                if (position.X < 0 || position.X >= CHUNK_SIZE || position.Y < 0 || position.Y > WORLD_HEIGHT_BLOCKS || position.Z < 0 || position.Z > CHUNK_SIZE)
                    return 0;

                int tile = (position.Y * CHUNK_SIZE * CHUNK_SIZE) + (position.Z * CHUNK_SIZE) + position.X;
                return GetBlock(tile);
            }
            public IEnumerable<byte> GetAllBlocks()
            {
                for (int i = 0; i < LENGTH_BLOCKDATA; i++)
                    yield return GetBlock(i);
            }

            public void SetBlock(int tile, byte blockId)
            {
                if (!IsUsed())
                    return;

                SaveData.SetByte(GetBlockAddress() + tile, blockId);
            }

            public void SetLayer(int layer, byte block)
            {
                for (int i = 0; i < LAYER_LENGTH; i++)
                {
                    //SetBlock(layer, i, block); TODO
                }
            }

            public BGParts GetBGParts(int index)
            {
                if (!IsUsed())
                    return null;
                
                if (index < 0 || index >= BGParts.MAXIMUM)
                    return null;
                
                return new BGParts(SaveData, this, index);
            }
            public IEnumerable<BGParts> GetAllBGParts()
            {
                if (!IsUsed())
                    throw new Exception("Can't get BGParts for an unused chunk!");
                
                for (int i = 0; i < BGParts.MAXIMUM && i < BGPartsCount; i++)
                {
                    yield return GetBGParts(i);
                }
            }

            public BGParts GetFirstUnusedBGParts()
            {
                if (GetAllBGParts().FirstOrDefault(bgParts => !bgParts.Exists()) is BGParts firstUnusedBgParts)
                    return firstUnusedBgParts;

                if (BGPartsCount < BGParts.MAXIMUM)
                {
                    BGPartsCount++;
                    return GetBGParts(BGPartsCount - 1);
                }

                return null;
            }

            public FluidType GetFluid(Vector2I position)
            {
                if (!IsUsed())
                    return FluidType.Air;

                int address = GetFluidAddress() + ((position.X + position.Y * 32) / 2);
                int nibble = position.X % 2;

                uint fluidType = SaveData.GetNumberBitwise(address, nibble * 4, 4);
                return fluidType switch
                {
                    1 => FluidType.Water,
                    2 => FluidType.HotWater,
                    3 => FluidType.Poison,
                    4 => FluidType.Lava,
                    _ => FluidType.Air
                };
            }

            public void Clear()
            {
                for (int i = GetBlockAddress(); i < GetBlockAddress() + LENGTH_BLOCKDATA; i++)
                {
                    SaveData.SetByte(i, 0); // TODO replace with fill
                }
            }
        }
        
        public class BGParts(WorldData saveData, Chunk chunk, int index)
        {
            public const int LENGTH = 4;
            public const int MAXIMUM = 1024;

            public readonly WorldData SaveData = saveData;
            public readonly Chunk Chunk = chunk;
            public readonly int Index = index;
            
            public int GetAddress() => Chunk.GetBGPartsAddress() + LENGTH * Index;

            public ushort BGPartsID 
            { 
                get => (ushort)SaveData.GetNumberBitwise(GetAddress(), 0, 9);
                set
                {
                    SaveData.RemoveBGPartsFromPositionDictionary(this);
                    SaveData.RemoveBGPartsFromOverlapDictionary(this);
                    SaveData.SetNumberBitwise(GetAddress(), 0, 9, value);
                    SaveData.AddBGPartsToPositionDictionary(this);
                    SaveData.AddBGPartsToOverlapDictionary(this);
                }
            }
            public byte X 
            { 
                get => (byte)SaveData.GetNumberBitwise(GetAddress() + 1, 1, 5);
                set
                {
                    SaveData.RemoveBGPartsFromPositionDictionary(this);
                    SaveData.RemoveBGPartsFromOverlapDictionary(this);
                    SaveData.SetNumberBitwise(GetAddress() + 1, 1, 5, value);
                    SaveData.AddBGPartsToPositionDictionary(this);
                    SaveData.AddBGPartsToOverlapDictionary(this);
                }
            }
            public byte Y 
            { 
                get => (byte)SaveData.GetNumberBitwise(GetAddress() + 1, 6, 5);
                set
                {
                    SaveData.RemoveBGPartsFromPositionDictionary(this);
                    SaveData.RemoveBGPartsFromOverlapDictionary(this);
                    SaveData.SetNumberBitwise(GetAddress() + 1, 6, 5, value);
                    SaveData.AddBGPartsToPositionDictionary(this);
                    SaveData.AddBGPartsToOverlapDictionary(this);
                }
            }
            public byte Z
            { 
                get => (byte)SaveData.GetNumberBitwise(GetAddress() + 2, 3, 5);
                set
                {
                    SaveData.RemoveBGPartsFromPositionDictionary(this);
                    SaveData.RemoveBGPartsFromOverlapDictionary(this);
                    SaveData.SetNumberBitwise(GetAddress() + 2, 3, 5, value);
                    SaveData.AddBGPartsToPositionDictionary(this);
                    SaveData.AddBGPartsToOverlapDictionary(this);
                }
            }
            public byte Direction
            {
                get => (byte)SaveData.GetNumberBitwise(GetAddress() + 3, 0, 2); 
                set
                {
                    SaveData.RemoveBGPartsFromPositionDictionary(this);
                    SaveData.RemoveBGPartsFromOverlapDictionary(this);
                    SaveData.SetNumberBitwise(GetAddress() + 3, 0, 2, value);
                    SaveData.AddBGPartsToPositionDictionary(this);
                    SaveData.AddBGPartsToOverlapDictionary(this);
                }
            }

            public bool Collision { get => SaveData.GetBit(GetAddress() + 3, 4); set => SaveData.SetBit(GetAddress() + 3, 4, value);  }
            public bool Effects { get => SaveData.GetBit(GetAddress() + 3, 2); set => SaveData.SetBit(GetAddress() + 3, 2, value); }

            public Vector3I GetLocalPosition() => new Vector3I(X, Y, Z);
            public Vector3I GetPosition() => GetLocalPosition() + Chunk.GetOrigin();

            public BGPartsInfo GetInfo() => BGPartsInfo.Get(BGPartsID);

            public bool Exists() => BGPartsID != 0;

            public Tuple<Vector3I, Vector3I> GetBounds()
            {
                Vector3I dimensions = Vector3I.Zero;
                if (GetInfo() is BGPartsInfo info)
                    dimensions = info.GetDimensions() - Vector3I.One;

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

            public void Clear()
            {
                SaveData.RemoveBGPartsFromPositionDictionary(this);
                SaveData.RemoveBGPartsFromOverlapDictionary(this);
                SaveData.Fill(0, GetAddress(), LENGTH);
            }
        }
    
        private Dictionary<Vector3I, List<Tuple<int, int>>> _BGPartsPositionDictionary;
        private void CreateBGPartsPositionDictionary()
        {
            _BGPartsPositionDictionary = [];
            foreach (Chunk chunk in GetUsedChunks())
            {
                foreach (BGParts bgParts in chunk.GetAllBGParts())
                {
                    AddBGPartsToPositionDictionary(bgParts);
                }
            }
        }
        private void AddBGPartsToPositionDictionary(BGParts bgParts)
        {
            if (!bgParts.Exists())
            {
                return;
            }

            Vector3I position = bgParts.GetPosition();
            if (_BGPartsPositionDictionary.TryGetValue(position, out List<Tuple<int, int>> propList))
            {
                propList.Add(new Tuple<int, int>(bgParts.Chunk.Index, bgParts.Index));
            }
            else
            {
                _BGPartsPositionDictionary.Add(position, [new Tuple<int, int>(bgParts.Chunk.Index, bgParts.Index)]);
            }
        }
        public void RemoveBGPartsFromPositionDictionary(BGParts prop)
        {
            Tuple<int, int> tuple = new Tuple<int, int>(prop.Chunk.Index, prop.Index);
            if (_BGPartsPositionDictionary.TryGetValue(prop.GetPosition(), out List<Tuple<int, int>> propIdxs) && propIdxs.Contains(tuple))
            {
                propIdxs.Remove(tuple);
            }
        }
        public BGParts GetBGPartsAtPosition(Vector3I position)
        {
            return GetAllBGPartsAtPosition(position).FirstOrDefault();
        }
        public IEnumerable<BGParts> GetAllBGPartsAtPosition(Vector3I position)
        {
            if (_BGPartsPositionDictionary.TryGetValue(position, out List<Tuple<int, int>> propIdxs))
            {
                foreach ((int chunkIdx, int propIdx) in propIdxs)
                {
                    yield return GetChunk(chunkIdx).GetBGParts(propIdx);
                }
            }
        }
        
        private Dictionary<Vector3I, List<Tuple<int, int>>> _BGPartsOverlapDictionary;
        private void CreateBGPartsOverlapDictionary()
        {
            _BGPartsOverlapDictionary = [];
            foreach (Chunk chunk in GetUsedChunks())
            {
                foreach (BGParts bgParts in chunk.GetAllBGParts())
                {
                    AddBGPartsToOverlapDictionary(bgParts);
                }
            }
        }
        private void AddBGPartsToOverlapDictionary(BGParts bgParts)
        {
            if (!bgParts.Exists())
            {
                return;
            }

            (Vector3I start, Vector3I end) = bgParts.GetBounds();
            for (int x = start.X; x <= end.X; x++)
            {
                for (int y = start.Y; y <= end.Y; y++)
                {
                    for (int z = start.Z; z <= end.Z; z++)
                    {
                        Vector3I position = new(x, y, z);
                        if (_BGPartsOverlapDictionary.TryGetValue(position, out List<Tuple<int, int>> propList))
                        {
                            propList.Add(new Tuple<int, int>(bgParts.Chunk.Index, bgParts.Index));
                        }
                        else
                        {
                            _BGPartsOverlapDictionary.Add(position, [new Tuple<int, int>(bgParts.Chunk.Index, bgParts.Index)]);
                        }
                    }
                }
            }
        }
        public void RemoveBGPartsFromOverlapDictionary(BGParts prop)
        {
            (Vector3I start, Vector3I end) = prop.GetBounds();
            for (int x = start.X; x <= end.X; x++)
            {
                for (int y = start.Y; y <= end.Y; y++)
                {
                    for (int z = start.Z; z <= end.Z; z++)
                    {
                        Tuple<int, int> tuple = new Tuple<int, int>(prop.Chunk.Index, prop.Index);
                        if (_BGPartsOverlapDictionary.TryGetValue(new Vector3I(x, y, z), out List<Tuple<int, int>> propIdxs) && propIdxs.Contains(tuple))
                        {
                            propIdxs.Remove(tuple);
                        }
                    }
                }
            }
        }
        public BGParts GetOverlappingBGParts(Vector3I position)
        {
            return GetAllOverlappingBGParts(position).FirstOrDefault();
        }
        public IEnumerable<BGParts> GetAllOverlappingBGParts(Vector3I position)
        {
            if (_BGPartsOverlapDictionary.TryGetValue(position, out List<Tuple<int, int>> propIdxs))
            {
                foreach ((int chunkIdx, int propIdx) in propIdxs)
                {
                    yield return GetChunk(chunkIdx).GetBGParts(propIdx);
                }
            }
        }
    }
}