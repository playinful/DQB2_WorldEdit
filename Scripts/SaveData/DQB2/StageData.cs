using System;
using System.Linq;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Godot;
using System.Dynamic;
using System.Reflection;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Security.Cryptography.X509Certificates;
using EyeOfRubiss.Info.DQB2;
using System.Data.SqlTypes;
using System.Runtime.CompilerServices;
using System.Net.Mail;
using System.ComponentModel;

namespace EyeOfRubiss
{
    public class StageData : SaveData
    {
        /*
            Here follows a general map of the buffer's layout.
    
            Chunk Grid: 0x24C7C1 - 0x24E7C0
            ???: 0x24E7C1 - 0x24E7D0
            Props: 0x24E7D1 - 0x150E7D0
            Blocks: 0x183FEF0 - end?
            Item Count? (Prop count?) : 0x24E7CD (short)
        */

        public const int HEADER_LENGTH = 0x110;

        /*
            The chunk grid is a 64x64 grid of chunk IDs. Each chunk ID is a 16-bit integer. The grid is laid out from left to right, and then from top to bottom.
            Each integer ID starting with 0 points to the position of the chunk's blocks in the data. So, the formula to find out the position of a chunk's block data is as follows:
                BlockAddress + (ChunkSize * {ID})
            For chunks without block data, their ID is instead set to 0xFFFF.
        */

        public const int CHUNK_SIZE = 32;

        public const int WORLD_SIZE_CHUNKS = 64;
        public const int WORLD_SIZE_BLOCKS = WORLD_SIZE_CHUNKS * CHUNK_SIZE;
        public const int WORLD_HEIGHT_BLOCKS = 96;

        /*
            Blocks are a series of 16-bit values laid out in order, broken up by chunk.
            Within a single chunk, blocks are laid out first from west to east, then from north to south, and finally from bottom to top.
            Each chunk of blocks is 32x32x96 blocks in size (96 blocks high).
        */

        /*
            The "prop list" is a list of prop IDs and the chunk IDs to which they belong. Each entry is a 32-bit value.
            The first 12 bits of the value point to the chunk ID, and the remaining 20 bits point to the prop ID.
            So, if the value was 0xDEADBEEF, then the prop with the ID of 0xDBEEF would belong to chunk ID 0xDEA.
        */

        public byte IslandID { get => GetByte(0xC0ED6); set => SetByte(0xC0ED6, value); }
        public int Gratitude { get => GetInt32(0xC0ECC); set => SetInt32(0xC0ECC, value); }
        public float Time { get => GetSingle(0xC0F50); set => SetSingle(0xC0F50, value); } // Floating point number between 0 and 1200
        public byte Weather { get => GetByte(0xC0F54); set => SetByte(0xC0F54, value); }

        public ushort ChunkCount { get => GetUInt16(0x1451AF); set => SetUInt16(0x1451AF, value); }
        public int PropCount { get => GetInt32(0x24E7CD); set => SetInt32(0x24E7CD, value); }
        
        public static bool TryLoad(string path, out StageData result)
        {
            result = null;
            StageData stageData = new();
            if (stageData._TryLoad(path, HEADER_LENGTH))
            {
                stageData.CreateBGPartsPositionDictionary();
                stageData.CreateBGPartsOverlapDictionary();
                result = stageData;
                return true;
            }
            else return false;
        }

        public override void Save(string path = null)
        {
            DefragmentChunks();
            DefragmentCrops();
            base.Save(path);
        }

        #region General methods
        public static bool PositionIsInBounds(Vector3I position)
        {
            if (position.Y < 0 || position.Y >= 96)
                return false;
            if (position.Z < 0 || position.Z >= 2048 || position.X < 0 || position.X >= 2048)
                return false;

            return true;
        }
        public static Vector3I PositionToDataPosition(Vector3I position)
        {
            int chunkIndex = PositionToChunkIndex(position);
            int layer = position.Y;
            int tile = position.X % 32 + (position.Z % 32 * 32);

            return new Vector3I(chunkIndex, layer, tile);
        }
        public static ushort PositionToChunkIndex(Vector3I position)
        {
            int x = position.X / 32;
            int z = position.Z / 32;
            return (ushort)(x + (z * 64));
        }
        public static Vector3I ChunkIndexToPosition(int chunkIndex)
        {
            return new((chunkIndex % 64 * 32) - 1024, 0, (chunkIndex / 64 * 32) - 1024);
        }

        public BlockInstance GetBlockAtPosition(Vector3I position)
        {
            if (!PositionIsInBounds(position))
                return null;

            Chunk chunk = GetChunkAtPosition(position);

            if (chunk is null || !chunk.IsUsed())
                return null;
                
            int chunkPosX = position.X % CHUNK_SIZE;
            int chunkPosY = position.Y;
            int chunkPosZ = position.Z % CHUNK_SIZE;

            int tile = (chunkPosY * CHUNK_SIZE * CHUNK_SIZE) + (chunkPosZ * CHUNK_SIZE) + (chunkPosX);

            return chunk.GetBlock(tile);
        }
        public bool SetBlockAtPosition(Vector3I position, ushort blockId, ChiselShape? chisel = null, bool? playerPlaced = null, bool createChunk = false)
        {
            if (!PositionIsInBounds(position))
                return false;

            Chunk chunk = GetChunkAtPosition(position);

            if (chunk is null || !chunk.IsUsed())
            {
                if (createChunk)
                {
                    AddChunk(chunk.Index);
                }
                else return false;
            }

            BlockInstance block = GetBlockAtPosition(position);
            block.BlockID = blockId;
            if (chisel is ChiselShape _chisel)
                block.Chisel = _chisel;
            if (playerPlaced is bool _playerPlaced)
                block.PlayerPlaced = _playerPlaced;

            return true;
        }
        public bool SetBlockAtPosition(Vector3I position, BlockInstance block, bool createChunk = false)
        {
            return SetBlockAtPosition(position, block.BlockID, block.Chisel, block.PlayerPlaced, createChunk);
        }

        public int GetSeaLevel(CommonData commonData = null)
        {
            if (IslandID == 12 || IslandID == 13 || IslandID == 16)
            {
                if (commonData is null)
                {
                    GD.Print("commonData is null");
                    return -1;
                }

                var buildertopiaType = IslandID switch
                {
                    12 => commonData.Buildertopia1Type,
                    13 => commonData.Buildertopia2Type,
                    16 => commonData.Buildertopia3Type,
                    _ => 0,
                };
                GD.Print($"buildertopiaType: {buildertopiaType}");

                return buildertopiaType switch
                {
                    0 => -1, // Invalid
                    1 => 31, // Blossom Bay
                    2 => 31, // Soggy Skerry
                    3 => 65, // Sunny Sands
                    4 => 11, // Iridiscent Island
                    5 => 73, // Coral Cay
                    6 => 31, // Rimey Reef
                    7 => 31, // Laguna Perfuma
                    8 => 31, // Unholy Holm
                    9 => 31, // Defiled Isle
                    10 => 31, // Bamboo Bluffs
                    11 => 73, // Gurgling Lagoon
                    _ => -1,
                };
            }

            return IslandID switch
            {
                1 => 31, // Isle of Awakening
                2 => 31, // Furrowfield
                3 => 65, // Khrumbul-Dun
                4 => 21, // Moonbrooke
                5 => -1, // Malhalla
                9 => 31, // Angler's Isle
                10 => 11, // Skelkatraz
                14 => 31, // Battle Atoll
                _ => -1,
            };
        }
        
        public InventoryItem GetBagItem(int index)
        {
            if (index < 0 || index >= 435)
                throw new IndexOutOfRangeException();

            return new InventoryItem(this, 0x24C0B4 + index * 4);
        }
        public IEnumerable<InventoryItem> GetBagItems(int index = 0, int count = 435)
        {
            if (index < 0 || index >= 435)
                throw new IndexOutOfRangeException();

            for (int i = 0; i < count - index; i++)
                yield return GetBagItem(index + i);
        }
        #endregion

        #region Chunks
        public class Chunk(StageData saveData, int index)
        {
            public const int START_ADDRESS_METADATA = 0x24C7C1;
            public const int START_ADDRESS_BLOCKDATA = 0x183FEF0;
            public const int LENGTH_METADATA = 2;
            public const int LENGTH_BLOCKDATA = 0x30000;
            public const int LAYER_LENGTH = 0x800;
            public const int MAXIMUM = 0x1000;

            public StageData SaveData { get; set; } = saveData;
            public int Index { get; set; } = index;

            public ushort BlockDataIndex { get => SaveData.GetUInt16(START_ADDRESS_METADATA + Index * 2); set => SaveData.SetUInt16(START_ADDRESS_METADATA + Index * 2, value); }// => SaveData.GetChunkIdByIndex(Index);

            public Vector3I GetOrigin() => ChunkIndexToPosition(Index);

            public bool IsUsed() => BlockDataIndex != ushort.MaxValue;

            public int GetMetadataAddress() => START_ADDRESS_METADATA + LENGTH_METADATA * Index;
            public int GetBlockAddress() => START_ADDRESS_BLOCKDATA + LENGTH_BLOCKDATA * BlockDataIndex;

            public Span<byte> GetData() => SaveData.GetBytes(GetBlockAddress(), LENGTH_BLOCKDATA);

            public BlockInstance GetBlock(int tile)
            {
                return IsUsed() ? new BlockInstance(SaveData, GetBlockAddress() + tile * BlockInstance.LENGTH) : null;
            }
            public BlockInstance GetBlock(Vector3I position)
            {
                if (!IsUsed())
                    return null;

                if (position.X < 0 || position.X >= CHUNK_SIZE || position.Y < 0 || position.Y >= WORLD_HEIGHT_BLOCKS || position.Z < 0 || position.Z >= CHUNK_SIZE)
                    return null;

                int tile = (position.Y * CHUNK_SIZE * CHUNK_SIZE) + (position.Z * CHUNK_SIZE) + position.X;
                return GetBlock(tile);
            }
            public IEnumerable<BlockInstance> GetAllBlocks()
            {
                for (int i = 0; i < BlockInstance.COUNT; i++)
                    yield return GetBlock(i);
            }

            public void SetBlock(int tile, ushort blockId, bool? playerPlaced = null, ChiselShape? chisel = null)
            {
                if (!IsUsed())
                    return;

                BlockInstance block = GetBlock(tile);
                block.BlockID = blockId;
                if (playerPlaced is not null)
                    block.PlayerPlaced = (bool)playerPlaced;
                if (chisel is not null)
                    block.Chisel = (ChiselShape)chisel;
            }
            public void SetBlock(Vector3I position, ushort blockId, bool? playerPlaced = null, ChiselShape? chisel = null)
            {
                if (!IsUsed())
                    return;

                if (position.X < 0 || position.X >= CHUNK_SIZE || position.Y < 0 || position.Y >= WORLD_HEIGHT_BLOCKS || position.Z < 0 || position.Z >= CHUNK_SIZE)
                    return;

                int tile = (position.Y * CHUNK_SIZE * CHUNK_SIZE) + (position.Z * CHUNK_SIZE) + position.X;
                SetBlock(tile, blockId, playerPlaced, chisel);
            }

            public void Clear()
            {
                for (int i = GetBlockAddress(); i < GetBlockAddress() + LENGTH_BLOCKDATA; i++)
                {
                    SaveData.SetByte(i, 0); // TODO replace with fill
                }
            }
        }
        public class BlockInstance(StageData saveData, int address)
        {
            public const int LENGTH = 2;
            public const int COUNT = 32 * 32 * 96;

            public readonly StageData SaveData = saveData;
            public readonly int Address = address;

            public ushort Value { get => SaveData.GetUInt16(Address); set => SaveData.SetUInt16(Address, value); }
            public ushort BlockID { get => (ushort)SaveData.GetNumberBitwise(Address, 0, 11); set => SaveData.SetNumberBitwise(Address, 0, 11, value); }
            public bool PlayerPlaced { get => SaveData.GetBit(Address + 1, 3); set => SaveData.SetBit(Address + 1, 3, value); }
            public ChiselShape Chisel { get => (ChiselShape)SaveData.GetNumberBitwise(Address + 1, 4, 4); set => SaveData.SetNumberBitwise(Address + 1, 4, 4, (byte)value); }

            public BlockInfo GetInfo() => BlockInfo.Get(BlockID);
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

        public Chunk GetChunkAtPosition(Vector3I position)
        {
            return GetChunk(PositionToChunkIndex(position));
        }

        public Chunk AddChunk(int index, ushort? id = null)
        {
            if (index >= Chunk.MAXIMUM || index < 0)
                return null;

            Chunk chunk = GetChunk(index);

            if (chunk.IsUsed())
                return null;

            chunk.BlockDataIndex = id ?? GetLowestUnusedBlockDataIndex();

            if (Chunk.START_ADDRESS_BLOCKDATA + Chunk.LENGTH_BLOCKDATA * (chunk.BlockDataIndex + 1) > GetBufferSize())
            {
                Extend(Chunk.START_ADDRESS_BLOCKDATA + Chunk.LENGTH_BLOCKDATA * (chunk.BlockDataIndex + 1));
            }

            GD.Print($"Created a new chunk {chunk.BlockDataIndex} at index {index}.");
            return chunk;
            // FIXME ~ 699 chunks may be the max?
        }
        public void RemoveChunk(int index)
        {
            if (index >= Chunk.MAXIMUM || index < 0)
                return;

            Chunk chunk = GetChunk(index);

            chunk.Clear();
            chunk.BlockDataIndex = 0xFFFF;
        }

        public void DefragmentChunks()
        {
            IOrderedEnumerable<Chunk> chunks = GetUsedChunks().OrderBy(chunk => chunk.Index);

            // Check to see if the enumerable is already sorted
            bool sorted = true;
            for (int i = 0; i < chunks.Count() - 1; i++)
            {
                Chunk previous = chunks.ElementAt(i);
                Chunk next = chunks.ElementAt(i + 1);

                if (previous.BlockDataIndex > next.BlockDataIndex)
                {
                    sorted = false;
                    break;
                }
            }
            if (sorted)
                return;

            byte[] chunkData = [];

            ushort index = 0; // TODO: Can this be done inside the sort function?
            foreach (Chunk chunk in chunks)
            {
                GD.Print($"Shifting chunk {chunk.BlockDataIndex} to {index}.");

                chunkData = [.. chunkData, .. chunk.GetData().ToArray()];
                chunk.BlockDataIndex = index;

                index++;
            }

            Fill(0, Chunk.START_ADDRESS_BLOCKDATA); // Zeroes out the block data
            SetBytes(Chunk.START_ADDRESS_BLOCKDATA, chunkData); // Copies the reserved chunk data

            return; /*

            List<byte> _buffer = [.. _Buffer[..BlockAddress]];

            IOrderedEnumerable<Chunk> chunks = GetUsedChunks().OrderBy(chunk => chunk.Index);
            List<byte[]> chunkData = [];
            ushort index = 0;
            foreach (Chunk chunk in chunks)
            {
                _buffer = [.. _buffer, .. chunk.GetData().ToArray()];
                GD.Print($"Shifting chunk {chunk.ID} to {index}.");
                chunk.ID = index;
                index++;
            }

            SetBuffer([.. _buffer]); // FIXME*/
        }

        private ushort GetLowestUnusedBlockDataIndex()
        {
            IOrderedEnumerable<ushort> ordered = GetChunks().Select(chunk => chunk.BlockDataIndex).Order();

            if (!ordered.Any() || ordered.First() > 0)
                return 0;

            for (int i = 0; i < ordered.Count() - 1; i++)
            {
                ushort a = ordered.ElementAt(i);
                ushort b = ordered.ElementAt(i + 1);

                if (b - a > 1)
                    return (ushort)(a + 1);
                if (b == 0xFFFF)
                    return (ushort)(a + 1);
            }

            return (ushort)(ordered.Last() + 1);
        }

        public IEnumerable<BlockInstance> GetAllBlocks()
        {
            foreach (Chunk chunk in GetUsedChunks())
            {
                foreach (BlockInstance block in chunk.GetAllBlocks())
                    yield return block;
            }
        }
        #endregion

        #region BGParts
        public class BGParts(StageData saveData, int index)
        {
            // NOTE: seems like first (0th) prop is unused
            // TODO: Rewrite this so it's more clear where the info and data is catalogued

            public const int START_ADDRESS = 0x24E7D1;
            public const int START_ADDRESS_METADATA = 0x150E7D1;
            public const int LENGTH = 24;
            public const int LENGTH_METADATA = 4;
            public const int MAXIMUM = 0xC8000;

            public StageData SaveData { get; set; } = saveData;
            public int Index { get; set; } = index;

            public int GetAddress() => START_ADDRESS + (int)DataIndex * LENGTH;
            public int GetMetadataAddress() => START_ADDRESS_METADATA + Index * LENGTH_METADATA;

            public Span<byte> GetBytes() => SaveData.GetBytes(GetAddress(), LENGTH);

            public ushort Chunk { get { return (ushort)SaveData.GetNumberBitwise(GetMetadataAddress(), 0, 12); } set { SaveData.SetNumberBitwise(GetMetadataAddress(), 0, 12, value); } }
            public uint DataIndex { get { return SaveData.GetNumberBitwise(GetMetadataAddress(), 12, 20); } set { SaveData.SetNumberBitwise(GetMetadataAddress(), 12, 20, value); } }

            public ushort BGPartsID
            {
                get { return (ushort)SaveData.GetNumberBitwise(GetAddress() + 8, 0, 13); }
                set
                {
                    SaveData.RemoveBGPartsFromPositionDictionary(this);
                    SaveData.RemoveBGPartsFromOverlapDictionary(this);
                    SaveData.SetNumberBitwise(GetAddress() + 8, 0, 13, value);
                    SaveData.AddBGPartsToPositionDictionary(this);
                    SaveData.AddBGPartsToOverlapDictionary(this);
                }
            }

            public byte X
            {
                get { return (byte)SaveData.GetNumberBitwise(GetAddress() + 9, 5, 5); }
                set
                {
                    SaveData.RemoveBGPartsFromPositionDictionary(this);
                    SaveData.RemoveBGPartsFromOverlapDictionary(this);
                    SaveData.SetNumberBitwise(GetAddress() + 9, 5, 5, value);
                    SaveData.AddBGPartsToPositionDictionary(this);
                    SaveData.AddBGPartsToOverlapDictionary(this);
                }
            }
            public byte Y
            {
                get { return (byte)SaveData.GetNumberBitwise(GetAddress() + 10, 2, 7); }
                set
                {
                    SaveData.RemoveBGPartsFromPositionDictionary(this);
                    SaveData.RemoveBGPartsFromOverlapDictionary(this);
                    SaveData.SetNumberBitwise(GetAddress() + 10, 2, 7, value);
                    SaveData.AddBGPartsToPositionDictionary(this);
                    SaveData.AddBGPartsToOverlapDictionary(this);
                }
            }
            public byte Z
            {
                get { return (byte)SaveData.GetNumberBitwise(GetAddress() + 11, 1, 5); }
                set
                {
                    SaveData.RemoveBGPartsFromPositionDictionary(this);
                    SaveData.RemoveBGPartsFromOverlapDictionary(this);
                    SaveData.SetNumberBitwise(GetAddress() + 11, 1, 5, value);
                    SaveData.AddBGPartsToPositionDictionary(this);
                    SaveData.AddBGPartsToOverlapDictionary(this);
                }
            }
            // 0 = North | 1 = West | 2 = South | 3 = East
            public byte Direction
            {
                get { return (byte)SaveData.GetNumberBitwise(GetAddress() + 11, 6, 2); }
                set
                {
                    SaveData.RemoveBGPartsFromPositionDictionary(this);
                    SaveData.RemoveBGPartsFromOverlapDictionary(this);
                    SaveData.SetNumberBitwise(GetAddress() + 11, 6, 2, value);
                    SaveData.AddBGPartsToPositionDictionary(this);
                    SaveData.AddBGPartsToOverlapDictionary(this);
                }
            }

            public bool Collision { get { return SaveData.GetBit(GetAddress() + 0xC, 1); } set { SaveData.SetBit(GetAddress() + 0xC, 1, value); } }
            public bool Unbreakable { get { return SaveData.GetBit(GetAddress() + 0xC, 0); } set { SaveData.SetBit(GetAddress() + 0xC, 0, value); } }
            public bool Effects { get { return SaveData.GetBit(GetAddress() + 0xF, 7); } set { SaveData.SetBit(GetAddress() + 0xF, 7, value); } }
            public byte Size { get { return (byte)SaveData.GetNumberBitwise(GetAddress() + 0xF, 4, 2); } set { SaveData.SetNumberBitwise(GetAddress() + 0xF, 4, 2, value); } }

            public byte ConnectingWindowRotation { get { return (byte)SaveData.GetNumberBitwise(GetAddress() + 15, 2, 2); } set { SaveData.SetNumberBitwise(GetAddress() + 15, 2, 2, value); } }

            public Vector3I GetPosition() => new(X + Chunk % 64 * 32, Y, Z + Chunk / 64 * 32);

            /// <summary> It's unclear what this is for. It must always be set to (Index * 16). </summary>
            private uint MagicNumberSixteen { get { return SaveData.GetNumberBitwise(GetAddress() + 12, 0, 24); } set { SaveData.SetNumberBitwise(GetAddress() + 12, 0, 24, value); } }

            public BGPartsInfo GetInfo() => BGPartsInfo.Get(BGPartsID);

            public bool Exists() => Chunk != 0xFFF && BGPartsID != 0 && DataIndex < SaveData.PropCount;

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

            public int GetGridMapRotation() => Util.GridMapRotationFromDirection(Direction, ConnectingWindowRotation);

            public void Clear()
            {
                SaveData.RemoveBGPartsFromPositionDictionary(this);
                SaveData.RemoveBGPartsFromOverlapDictionary(this);
                Chunk = 0xFFF;
                SaveData.Fill(0, GetAddress(), LENGTH);
                MagicNumberSixteen = DataIndex * 16;
            }
        }

        private Dictionary<Vector3I, List<int>> _BGPartsPositionDictionary;
        public void CreateBGPartsPositionDictionary()
        {
            _BGPartsPositionDictionary = [];
            foreach (BGParts bgParts in GetBGParts())
            {
                AddBGPartsToPositionDictionary(bgParts);
            }
        }
        public void AddBGPartsToPositionDictionary(BGParts bgParts)
        {
            if (!bgParts.Exists())
                return;

            Vector3I position = bgParts.GetPosition();
            if (_BGPartsPositionDictionary.TryGetValue(position, out List<int> propList))
            {
                propList.Add(bgParts.Index);
            }
            else
            {
                _BGPartsPositionDictionary.Add(position, [bgParts.Index]);
            }
        }
        public void RemoveBGPartsFromPositionDictionary(BGParts bgParts)
        {
            if (_BGPartsPositionDictionary.TryGetValue(bgParts.GetPosition(), out List<int> propIdxs))
            {
                propIdxs.Remove(bgParts.Index);
            }
        }
        public BGParts GetBGPartsAtPosition(Vector3I position)
        {
            return GetAllBGPartsAtPosition(position).FirstOrDefault();
        }
        public IEnumerable<BGParts> GetAllBGPartsAtPosition(Vector3I position)
        {
            if (_BGPartsPositionDictionary.TryGetValue(position, out List<int> propIdxs))
            {
                foreach (int propIdx in propIdxs)
                {
                    yield return GetProp(propIdx);
                }
            }
        }

        private Dictionary<Vector3I, List<int>> _BGPartsOverlapDictionary;
        public void CreateBGPartsOverlapDictionary()
        {
            _BGPartsOverlapDictionary = [];
            foreach (BGParts prop in GetBGParts())
            {
                AddBGPartsToOverlapDictionary(prop);
            }
        }
        public void AddBGPartsToOverlapDictionary(BGParts bgParts)
        {
            if (!bgParts.Exists())
                return;

            (Vector3I start, Vector3I end) = bgParts.GetBounds();
            for (int x = start.X; x <= end.X; x++)
            {
                for (int y = start.Y; y <= end.Y; y++)
                {
                    for (int z = start.Z; z <= end.Z; z++)
                    {
                        Vector3I position = new(x, y, z);
                        if (_BGPartsOverlapDictionary.ContainsKey(position))
                        {
                            List<int> propList = _BGPartsOverlapDictionary[position];
                            propList.Add(bgParts.Index);
                        }
                        else
                        {
                            _BGPartsOverlapDictionary.Add(position, [bgParts.Index]);
                        }
                    }
                }
            }
        }
        public void RemoveBGPartsFromOverlapDictionary(BGParts bgParts)
        {
            (Vector3I start, Vector3I end) = bgParts.GetBounds();
            for (int x = start.X; x <= end.X; x++)
            {
                for (int y = start.Y; y <= end.Y; y++)
                {
                    for (int z = start.Z; z <= end.Z; z++)
                    {
                        if (_BGPartsOverlapDictionary.TryGetValue(new Vector3I(x, y, z), out List<int> propIdxs))
                        {
                            propIdxs.Remove(bgParts.Index);
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
            if (_BGPartsOverlapDictionary.TryGetValue(position, out List<int> propIdxs))
            {
                foreach (int propIdx in propIdxs)
                {
                    yield return GetProp(propIdx);
                }
            }
        }

        public BGParts GetProp(int index)
        {
            if (index < 0 || index >= BGParts.MAXIMUM)
                throw new IndexOutOfRangeException();

            return new BGParts(this, index);
        }
        public IEnumerable<BGParts> GetBGParts(int index = 0, int count = BGParts.MAXIMUM)
        {
            if (index < 0 || index >= BGParts.MAXIMUM)
                throw new IndexOutOfRangeException();

            for (int i = 0; i < count && i + index < BGParts.MAXIMUM && i + index < PropCount; i++)
                yield return GetProp(i + index);
        }

        public BGParts AddBGParts(Vector3I position, ushort propId, byte rotation = 0)
        {
            BGParts prop = GetFirstUnusedBGParts();

            if (prop.DataIndex >= PropCount)
                PropCount = (int)prop.DataIndex + 1;
            
            prop.Clear();
            prop.Chunk = PositionToChunkIndex(position);
            prop.X = (byte)(position.X % 32);
            prop.Y = (byte)position.Y;
            prop.Z = (byte)(position.Z % 32);
            prop.Direction = rotation;
            prop.BGPartsID = propId;

            return prop;
        }
        public BGParts GetFirstUnusedBGParts()
        {
            BGParts prop = GetBGParts().FirstOrDefault(prop => prop.Index > 0 && !prop.Exists());
            if (prop is null)
            {
                PropCount++;
                return GetProp(PropCount - 1);
            }
            else return prop;
        }

        public void DeleteAllBGParts()
        {
            SetNumberBitwise(0x24E7CD, 0, 12, 1); // I don't know why this is required.

            foreach (BGParts prop in GetBGParts())
            {
                prop.Clear();
            }
        }
        #endregion

        #region Block Entities
        public abstract class BlockEntity(StageData saveData)
        {
            public readonly StageData SaveData = saveData;

            abstract public ushort X { get; set; }
            abstract public byte Y { get; set; }
            abstract public ushort Z { get; set; }

            public virtual Vector3I GetPosition() => new(X, Y, Z);

            public virtual void Clear() {}
        }
        public void ClearBlockEntitiesAtPosition(Vector3I position)
        {
            foreach (Storage storage in GetAllStorage())
                if (storage.Enabled && storage.GetPosition() == position)
                    storage.Clear();
            foreach (ItemDisplay display in GetAllItemDisplays())
                if (display.Enabled && display.GetPosition() == position)
                    display.Clear();
            foreach (CraftingStation station in GetCraftingStations())
                if (station.GetPosition() == position)
                    station.Clear();
            foreach (Signpost signpost in GetSignposts())
                if (signpost.Enabled && signpost.GetPosition() == position)
                    signpost.Clear();
            foreach (SalutationStation station in GetSalutationStations())
                if (station.Enabled && station.GetPosition() == position)
                    station.Clear();
            foreach (Crop crop in GetCrops())
                if (crop.GetPosition() == position)
                    crop.Clear();
            foreach (Scarecrow scarecrow in GetScarecrows())
                if (scarecrow.Enabled && scarecrow.GetPosition() == position)
                    scarecrow.Clear();
            foreach (Instrument instrument in GetInstruments())
                if (instrument.GetPosition() == position)
                    instrument.Clear();
            foreach (MagneticBlock block in GetMagneticBlocks())
                if (block.Enabled && block.GetPosition() == position)
                    block.Clear();
            foreach (MagicPencil pencil in GetMagicPencils())
                if (pencil.Enabled && pencil.GetPosition() == position)
                    pencil.Clear();
            foreach (FireworkCannon cannon in GetFireworkCannons())
                if (cannon.Enabled && cannon.GetPosition() == position)
                    cannon.Clear();
            foreach (PictureFrame frame in GetPictureFrames())
                if (frame.Enabled && frame.GetPosition() == position)
                    frame.Clear();
            foreach (Watchfire watchfire in GetWatchfires())
                if (watchfire.Enabled && watchfire.GetPosition() == position)
                    watchfire.Clear();
            foreach (WardOfErdrick ward in GetWardsOfErdrick())
                if (ward.Enabled && ward.GetPosition() == position)
                    ward.Clear();
            foreach (Buggy buggy in GetBuggies())
                if (buggy.Enabled && buggy.GetPosition() == position)
                    buggy.Clear();
            foreach (Toilet toilet in GetToilets())
                if (toilet.Enabled && toilet.GetPosition() == position)
                    toilet.Clear();
        }
        public void ClearAllBlockEntities()
        {
            foreach (Storage storage in GetAllStorage())
                storage.Clear();
            foreach (ItemDisplay display in GetAllItemDisplays())
                display.Clear();
            foreach (CraftingStation station in GetCraftingStations())
                station.Clear();
            foreach (Signpost signpost in GetSignposts())
                signpost.Clear();
            foreach (SalutationStation station in GetSalutationStations())
                station.Clear();
            //foreach (Crop crop in GetCrops())
            //    crop.Clear();
            CropCount = 0;
            foreach (Scarecrow scarecrow in GetScarecrows())
                scarecrow.Clear();
            foreach (Instrument instrument in GetInstruments())
                instrument.Clear();
            foreach (MagneticBlock block in GetMagneticBlocks())
                block.Clear();
            foreach (MagicPencil pencil in GetMagicPencils())
                pencil.Clear();
            foreach (FireworkCannon cannon in GetFireworkCannons())
                cannon.Clear();
            foreach (PictureFrame frame in GetPictureFrames())
                frame.Clear();
            foreach (Watchfire watchfire in GetWatchfires())
                watchfire.Clear();
            foreach (WardOfErdrick ward in GetWardsOfErdrick())
                ward.Clear();
            foreach (Buggy buggy in GetBuggies())
                buggy.Clear();
            foreach (Toilet toilet in GetToilets())
                toilet.Clear();
        }

        public class Storage(StageData saveData, int metadataAddress, int contentsAddress) : BlockEntity(saveData)
        {
            public int MetadataAddress = metadataAddress;
            public int ContentsAddress = contentsAddress;

            public const int LENGTH_METADATA = 8;
            public const int LENGTH_CONTENTS = ITEM_COUNT * 4;
            public const int ITEM_COUNT = 30;

            public const int START_ADDRESS_CHEST_METADATA = 0xF565;
            public const int START_ADDRESS_CHEST_CONTENTS = 0x2467CC;
            public const int CHEST_MAXIMUM = 50;
            public const byte CHEST_TYPE = 1;

            public const int START_ADDRESS_STORAGE_TYPE_2_METADATA = 0xF6F5;
            public const int START_ADDRESS_STORAGE_TYPE_2_CONTENTS = 0x247F3C;
            public const int STORAGE_TYPE_2_MAXIMUM = 8;
            public const byte STORAGE_TYPE_2_TYPE = 2;

            public const int START_ADDRESS_STORAGE_TYPE_3_METADATA = 0xF735;
            public const int START_ADDRESS_STORAGE_TYPE_3_CONTENTS = 0x2482FC;
            public const int STORAGE_TYPE_3_MAXIMUM = 8;
            public const byte STORAGE_TYPE_3_TYPE = 3;
            
            public const int START_ADDRESS_WARDROBE_METADATA = 0xFF75;
            public const int START_ADDRESS_WARDROBE_CONTENTS = 0x248ABC;
            public const int WARDROBE_MAXIMUM = 64;
            public const byte WARDROBE_TYPE = 9;

            public const int START_ADDRESS_MULTIPLAYER_CHEST_METADATA = 0x10275;
            public const int START_ADDRESS_MULTIPLAYER_CHEST_CONTENTS = 0x24A93C;
            public const int MULTIPLAYER_CHEST_MAXIMUM = 1;
            public const byte MULTIPLAYER_CHEST_TYPE = 0xB;

            public const int START_ADDRESS_CUPBOARD_METADATA = 0x1027D;
            public const int START_ADDRESS_CUPBOARD_CONTENTS = 0x24A9B0;
            public const int CUPBOARD_MAXIMUM = 16;
            public const byte CUPBOARD_TYPE = 0xC;

            public const int START_ADDRESS_DRAWER_METADATA = 0x102FD;
            public const int START_ADDRESS_DRAWER_CONTENTS = 0x24B134;
            public const int DRAWER_MAXIMUM = 32;
            public const byte DRAWER_TYPE = 0xD;

            public override ushort X { get => SaveData.GetUInt16(MetadataAddress); set => SaveData.SetUInt16(MetadataAddress, value); }
            public override byte Y { get => SaveData.GetByte(MetadataAddress + 2); set => SaveData.SetUInt16(MetadataAddress + 2, value); }
            public override ushort Z { get => SaveData.GetUInt16(MetadataAddress + 4); set => SaveData.SetUInt16(MetadataAddress + 4, value); }

            public bool Enabled { get => SaveData.GetBit(MetadataAddress + 6, 0); set => SaveData.SetBit(MetadataAddress + 6, 0, value); }

            public byte StorageType { get => SaveData.GetByte(MetadataAddress + 7); set => SaveData.SetByte(MetadataAddress + 7, value); } // Type must match for the area in STGDAT.

            public InventoryItem GetItem(int index)
            {
                if (index < 0 || index >= ITEM_COUNT)
                    throw new IndexOutOfRangeException();

                return new(SaveData, ContentsAddress + index * InventoryItem.LENGTH);
            }
            public IEnumerable<InventoryItem> GetItems(int index = 0, int count = ITEM_COUNT)
            {
                if (index < 0 || index >= ITEM_COUNT)
                    throw new IndexOutOfRangeException();

                for (int i = 0; i < count && i + index < ITEM_COUNT; i++)
                    yield return GetItem(i + index);
            }

            public override void Clear()
            {
                SaveData.Fill(0, MetadataAddress, LENGTH_METADATA);
                SaveData.Fill(0, ContentsAddress, LENGTH_CONTENTS);
            }
        }
        public Storage GetChest(int index)
        {
            if (index < 0 || index >= Storage.CHEST_MAXIMUM)
                throw new IndexOutOfRangeException();
            
            return new Storage(this,
                Storage.START_ADDRESS_CHEST_METADATA + Storage.LENGTH_METADATA * index,
                Storage.START_ADDRESS_CHEST_CONTENTS + Storage.LENGTH_CONTENTS * index);
        }
        public IEnumerable<Storage> GetChests(int index = 0, int count = Storage.CHEST_MAXIMUM)
        {
            if (index < 0 || index >= Storage.CHEST_MAXIMUM)
                throw new IndexOutOfRangeException();
            
            for (int i = 0; i < count && i + index < Storage.CHEST_MAXIMUM; i++)
                yield return GetChest(i + index);
        }
        public Storage GetStorageType2(int index)
        {
            if (index < 0 || index >= Storage.STORAGE_TYPE_2_MAXIMUM)
                throw new IndexOutOfRangeException();
            
            return new Storage(this,
                Storage.START_ADDRESS_STORAGE_TYPE_2_METADATA + Storage.LENGTH_METADATA * index,
                Storage.START_ADDRESS_STORAGE_TYPE_2_CONTENTS + Storage.LENGTH_CONTENTS * index);
        }
        public IEnumerable<Storage> GetStoragesType2(int index = 0, int count = Storage.STORAGE_TYPE_2_MAXIMUM)
        {
            if (index < 0 || index >= Storage.STORAGE_TYPE_2_MAXIMUM)
                throw new IndexOutOfRangeException();
            
            for (int i = 0; i < count && i + index < Storage.STORAGE_TYPE_2_MAXIMUM; i++)
                yield return GetStorageType2(i + index);
        }
        public Storage GetStorageType3(int index)
        {
            if (index < 0 || index >= Storage.STORAGE_TYPE_3_MAXIMUM)
                throw new IndexOutOfRangeException();
            
            return new Storage(this,
                Storage.START_ADDRESS_STORAGE_TYPE_3_METADATA + Storage.LENGTH_METADATA * index,
                Storage.START_ADDRESS_STORAGE_TYPE_3_CONTENTS + Storage.LENGTH_CONTENTS * index);
        }
        public IEnumerable<Storage> GetStoragesType3(int index = 0, int count = Storage.STORAGE_TYPE_3_MAXIMUM)
        {
            if (index < 0 || index >= Storage.STORAGE_TYPE_3_MAXIMUM)
                throw new IndexOutOfRangeException();
            
            for (int i = 0; i < count && i + index < Storage.STORAGE_TYPE_3_MAXIMUM; i++)
                yield return GetStorageType3(i + index);
        }
        public Storage GetWardrobe(int index)
        {
            if (index < 0 || index >= Storage.WARDROBE_MAXIMUM)
                throw new IndexOutOfRangeException();
            
            return new Storage(this,
                Storage.START_ADDRESS_WARDROBE_METADATA + Storage.LENGTH_METADATA * index,
                Storage.START_ADDRESS_WARDROBE_CONTENTS + Storage.LENGTH_CONTENTS * index);
        }
        public IEnumerable<Storage> GetWardrobes(int index = 0, int count = Storage.WARDROBE_MAXIMUM)
        {
            if (index < 0 || index >= Storage.WARDROBE_MAXIMUM)
                throw new IndexOutOfRangeException();
            
            for (int i = 0; i < count && i + index < Storage.WARDROBE_MAXIMUM; i++)
                yield return GetWardrobe(i + index);
        }
        public Storage GetMultiplayerChest()
        {
            return new Storage(this,
                Storage.START_ADDRESS_MULTIPLAYER_CHEST_METADATA,
                Storage.START_ADDRESS_MULTIPLAYER_CHEST_CONTENTS);
        }
        public Storage GetCupboard(int index)
        {
            if (index < 0 || index >= Storage.CUPBOARD_MAXIMUM)
                throw new IndexOutOfRangeException();
            
            return new Storage(this,
                Storage.START_ADDRESS_CUPBOARD_METADATA + Storage.LENGTH_METADATA * index,
                Storage.START_ADDRESS_CUPBOARD_CONTENTS + Storage.LENGTH_CONTENTS * index);
        }
        public IEnumerable<Storage> GetCupboards(int index = 0, int count = Storage.CUPBOARD_MAXIMUM)
        {
            if (index < 0 || index >= Storage.CUPBOARD_MAXIMUM)
                throw new IndexOutOfRangeException();
            
            for (int i = 0; i < count && i + index < Storage.CUPBOARD_MAXIMUM; i++)
                yield return GetCupboard(i + index);
        }
        public Storage GetDrawer(int index)
        {
            if (index < 0 || index >= Storage.DRAWER_MAXIMUM)
                throw new IndexOutOfRangeException();
            
            return new Storage(this,
                Storage.START_ADDRESS_DRAWER_METADATA + Storage.LENGTH_METADATA * index,
                Storage.START_ADDRESS_DRAWER_CONTENTS + Storage.LENGTH_CONTENTS * index);
        }
        public IEnumerable<Storage> GetDrawers(int index = 0, int count = Storage.DRAWER_MAXIMUM)
        {
            if (index < 0 || index >= Storage.DRAWER_MAXIMUM)
                throw new IndexOutOfRangeException();
            
            for (int i = 0; i < count && i + index < Storage.DRAWER_MAXIMUM; i++)
                yield return GetDrawer(i + index);
        }
        public IEnumerable<Storage> GetAllStorage()
        {
            foreach (Storage storage in GetChests())
                yield return storage;
            foreach (Storage storage in GetStoragesType2())
                yield return storage;
            foreach (Storage storage in GetStoragesType3())
                yield return storage;
            foreach (Storage storage in GetWardrobes())
                yield return storage;
            yield return GetMultiplayerChest();
            foreach (Storage storage in GetCupboards())
                yield return storage;
            foreach (Storage storage in GetDrawers())
                yield return storage;
        }
        public Storage GetStorageAtPosition(Vector3I position)
        {
            return GetAllStorage().FirstOrDefault(storage => storage.Enabled && storage.GetPosition() == position);
        }

        public class ItemDisplay(StageData saveData, int metadataAddress, int contentsAddress) : BlockEntity(saveData)
        {
            public const int LENGTH_METADATA = 8;
            public const int LENGTH_CONTENTS = 4;

            public const int START_ADDRESS_ITEM_DISPLAY_METADATA = 0xF775;
            public const int START_ADDRESS_ITEM_DISPLAY_CONTENTS = 0x2486BC;
            public const int ITEM_DISPLAY_MAXIMUM = 128;
            public const byte ITEM_DISPLAY_TYPE = 4;

            public const int START_ADDRESS_EQUIPMENT_DISPLAY_METADATA = 0xFB75;
            public const int START_ADDRESS_EQUIPMENT_DISPLAY_CONTENTS = 0x2488BC;
            public const int EQUIPMENT_DISPLAY_MAXIMUM = 32;
            public const byte EQUIPMENT_DISPLAY_TYPE = 5;

            public const int START_ADDRESS_FOOD_DISPLAY_METADATA = 0xFC75;
            public const int START_ADDRESS_FOOD_DISPLAY_CONTENTS = 0x24893C;
            public const int FOOD_DISPLAY_MAXIMUM = 32;
            public const byte FOOD_DISPLAY_TYPE = 6;

            public const int START_ADDRESS_DECORATIVE_FOOD_METADATA = 0xFD75;
            public const int START_ADDRESS_DECORATIVE_FOOD_CONTENTS = 0x2489BC;
            public const int DECORATIVE_FOOD_MAXIMUM = 32;
            public const byte DECORATIVE_FOOD_TYPE = 7;

            public const int START_ADDRESS_PET_BOWL_METADATA = 0xFE75;
            public const int START_ADDRESS_PET_BOWL_CONTENTS = 0x248A3C;
            public const int PET_BOWL_MAXIMUM = 32;
            public const byte PET_BOWL_TYPE = 8;

            public const int START_ADDRESS_PRICE_TAG_METADATA = 0x10175;
            public const int START_ADDRESS_PRICE_TAG_CONTENTS = 0x24A8BC;
            public const int PRICE_TAG_MAXIMUM = 32;
            public const byte PRICE_TAG_TYPE = 0xA;

            public const int START_ADDRESS_BEVERAGE_METADATA = 0x103FD;
            public const int START_ADDRESS_BEVERAGE_CONTENTS = 0x24C034;
            public const int BEVERAGE_MAXIMUM = 32;
            public const byte BEVERAGE_TYPE = 0xE;

            public readonly int MetadataAddress = metadataAddress;
            public readonly int ContentsAddress = contentsAddress;

            public InventoryItem Item => new(SaveData, ContentsAddress);

            public override ushort X { get { return SaveData.GetUInt16(MetadataAddress); } set { SaveData.SetUInt16(MetadataAddress, value); } }
            public override byte Y { get { return SaveData.GetByte(MetadataAddress + 2); } set { SaveData.SetUInt16(MetadataAddress + 2, value); } }
            public override ushort Z { get { return SaveData.GetUInt16(MetadataAddress + 4); } set { SaveData.SetUInt16(MetadataAddress + 4, value); } }

            public bool Enabled { get => SaveData.GetBit(MetadataAddress + 6, 0); set => SaveData.SetBit(MetadataAddress + 6, 0, value); }

            public byte StorageType { get => SaveData.GetByte(MetadataAddress + 7); set => SaveData.SetByte(MetadataAddress + 7, value); } // Type must match for the area in STGDAT.
        
            public override void Clear()
            {
                SaveData.Fill(0, MetadataAddress, LENGTH_METADATA);
                SaveData.Fill(0, ContentsAddress, LENGTH_CONTENTS);
            }
        }
        public ItemDisplay GetItemDisplayGeneric(int index)
        {
            if (index < 0 || index >= ItemDisplay.ITEM_DISPLAY_MAXIMUM)
                throw new IndexOutOfRangeException();

            return new ItemDisplay(this,
                ItemDisplay.START_ADDRESS_ITEM_DISPLAY_METADATA + ItemDisplay.LENGTH_METADATA * index,
                ItemDisplay.START_ADDRESS_ITEM_DISPLAY_CONTENTS + ItemDisplay.LENGTH_CONTENTS * index);
        }
        public IEnumerable<ItemDisplay> GetItemDisplaysGeneric(int index = 0, int count = ItemDisplay.ITEM_DISPLAY_MAXIMUM)
        {
            if (index < 0 || index >= ItemDisplay.ITEM_DISPLAY_MAXIMUM)
                throw new IndexOutOfRangeException();
            
            for (int i = 0; i < count && i + index < ItemDisplay.ITEM_DISPLAY_MAXIMUM; i++)
                yield return GetItemDisplayGeneric(i + index);
        }
        public ItemDisplay GetEquipmentDisplay(int index)
        {
            if (index < 0 || index >= ItemDisplay.EQUIPMENT_DISPLAY_MAXIMUM)
                throw new IndexOutOfRangeException();

            return new ItemDisplay(this,
                ItemDisplay.START_ADDRESS_EQUIPMENT_DISPLAY_METADATA + ItemDisplay.LENGTH_METADATA * index,
                ItemDisplay.START_ADDRESS_EQUIPMENT_DISPLAY_CONTENTS + ItemDisplay.LENGTH_CONTENTS * index);
        }
        public IEnumerable<ItemDisplay> GetEquipmentDisplays(int index = 0, int count = ItemDisplay.EQUIPMENT_DISPLAY_MAXIMUM)
        {
            if (index < 0 || index >= ItemDisplay.EQUIPMENT_DISPLAY_MAXIMUM)
                throw new IndexOutOfRangeException();
            
            for (int i = 0; i < count && i + index < ItemDisplay.EQUIPMENT_DISPLAY_MAXIMUM; i++)
                yield return GetEquipmentDisplay(i + index);
        }
        public ItemDisplay GetFoodDisplay(int index)
        {
            if (index < 0 || index >= ItemDisplay.FOOD_DISPLAY_MAXIMUM)
                throw new IndexOutOfRangeException();

            return new ItemDisplay(this,
                ItemDisplay.START_ADDRESS_FOOD_DISPLAY_METADATA + ItemDisplay.LENGTH_METADATA * index,
                ItemDisplay.START_ADDRESS_FOOD_DISPLAY_CONTENTS + ItemDisplay.LENGTH_CONTENTS * index);
        }
        public IEnumerable<ItemDisplay> GetFoodDisplays(int index = 0, int count = ItemDisplay.FOOD_DISPLAY_MAXIMUM)
        {
            if (index < 0 || index >= ItemDisplay.FOOD_DISPLAY_MAXIMUM)
                throw new IndexOutOfRangeException();
            
            for (int i = 0; i < count && i + index < ItemDisplay.FOOD_DISPLAY_MAXIMUM; i++)
                yield return GetFoodDisplay(i + index);
        }
        public ItemDisplay GetDecorativeFood(int index)
        {
            if (index < 0 || index >= ItemDisplay.DECORATIVE_FOOD_MAXIMUM)
                throw new IndexOutOfRangeException();

            return new ItemDisplay(this,
                ItemDisplay.START_ADDRESS_DECORATIVE_FOOD_METADATA + ItemDisplay.LENGTH_METADATA * index,
                ItemDisplay.START_ADDRESS_DECORATIVE_FOOD_CONTENTS + ItemDisplay.LENGTH_CONTENTS * index);
        }
        public IEnumerable<ItemDisplay> GetDecorativeFoods(int index = 0, int count = ItemDisplay.DECORATIVE_FOOD_MAXIMUM)
        {
            if (index < 0 || index >= ItemDisplay.DECORATIVE_FOOD_MAXIMUM)
                throw new IndexOutOfRangeException();
            
            for (int i = 0; i < count && i + index < ItemDisplay.DECORATIVE_FOOD_MAXIMUM; i++)
                yield return GetDecorativeFood(i + index);
        }
        public ItemDisplay GetPetBowl(int index)
        {
            if (index < 0 || index >= ItemDisplay.PET_BOWL_MAXIMUM)
                throw new IndexOutOfRangeException();

            return new ItemDisplay(this,
                ItemDisplay.START_ADDRESS_PET_BOWL_METADATA + ItemDisplay.LENGTH_METADATA * index,
                ItemDisplay.START_ADDRESS_PET_BOWL_CONTENTS + ItemDisplay.LENGTH_CONTENTS * index);
        }
        public IEnumerable<ItemDisplay> GetPetBowls(int index = 0, int count = ItemDisplay.PET_BOWL_MAXIMUM)
        {
            if (index < 0 || index >= ItemDisplay.PET_BOWL_MAXIMUM)
                throw new IndexOutOfRangeException();
            
            for (int i = 0; i < count && i + index < ItemDisplay.PET_BOWL_MAXIMUM; i++)
                yield return GetPetBowl(i + index);
        }
        public ItemDisplay GetBeverage(int index)
        {
            if (index < 0 || index >= ItemDisplay.BEVERAGE_MAXIMUM)
                throw new IndexOutOfRangeException();

            return new ItemDisplay(this,
                ItemDisplay.START_ADDRESS_BEVERAGE_METADATA + ItemDisplay.LENGTH_METADATA * index,
                ItemDisplay.START_ADDRESS_BEVERAGE_CONTENTS + ItemDisplay.LENGTH_CONTENTS * index);
        }
        public IEnumerable<ItemDisplay> GetBeverages(int index = 0, int count = ItemDisplay.BEVERAGE_MAXIMUM)
        {
            if (index < 0 || index >= ItemDisplay.BEVERAGE_MAXIMUM)
                throw new IndexOutOfRangeException();
            
            for (int i = 0; i < count && i + index < ItemDisplay.BEVERAGE_MAXIMUM; i++)
                yield return GetBeverage(i + index);
        }
        public ItemDisplay GetPriceTag(int index)
        {
            if (index < 0 || index >= ItemDisplay.PRICE_TAG_MAXIMUM)
                throw new IndexOutOfRangeException();

            return new ItemDisplay(this,
                ItemDisplay.START_ADDRESS_PRICE_TAG_METADATA + ItemDisplay.LENGTH_METADATA * index,
                ItemDisplay.START_ADDRESS_PRICE_TAG_CONTENTS + ItemDisplay.LENGTH_CONTENTS * index);
        }
        public IEnumerable<ItemDisplay> GetPriceTags(int index = 0, int count = ItemDisplay.PRICE_TAG_MAXIMUM)
        {
            if (index < 0 || index >= ItemDisplay.PRICE_TAG_MAXIMUM)
                throw new IndexOutOfRangeException();
            
            for (int i = 0; i < count && i + index < ItemDisplay.PRICE_TAG_MAXIMUM; i++)
                yield return GetPriceTag(i + index);
        }
        public IEnumerable<ItemDisplay> GetAllItemDisplays()
        {
            foreach (ItemDisplay display in GetItemDisplaysGeneric())
                yield return display;
            foreach (ItemDisplay display in GetEquipmentDisplays())
                yield return display;
            foreach (ItemDisplay display in GetFoodDisplays())
                yield return display;
            foreach (ItemDisplay display in GetDecorativeFoods())
                yield return display;
            foreach (ItemDisplay display in GetPetBowls())
                yield return display;
            foreach (ItemDisplay display in GetBeverages())
                yield return display;
            foreach (ItemDisplay display in GetPriceTags())
                yield return display;
        }
        public ItemDisplay GetItemDisplayAtPosition(Vector3I position)
        {
            return GetAllItemDisplays().FirstOrDefault(display => display.Enabled && display.GetPosition() == position);
        }

        public class CraftingStation(StageData saveData, int address) : BlockEntity(saveData)
        {
            public const int START_ADDRESS = 0x10EA5;
            public const int LENGTH = 0x34;
            public const int MAXIMUM = 128;

            public readonly int Address = address;

            public override ushort X { get { return SaveData.GetUInt16(Address); } set { SaveData.SetUInt16(Address, value); } }
            public override byte Y { get { return SaveData.GetByte(Address + 2); } set { SaveData.SetUInt16(Address + 2, value); } }
            public override ushort Z { get { return SaveData.GetUInt16(Address + 4); } set { SaveData.SetUInt16(Address + 4, value); } }

            public override void Clear()
            {
                SaveData.Fill(0, Address, LENGTH);
            }

            public bool Exists()
            {
                return SaveData.GetBytes(Address, LENGTH).ToArray().All(b => b == 0);
            }
        }
        public CraftingStation GetCraftingStation(int index)
        {
            if (index < 0 || index >= CraftingStation.MAXIMUM)
                throw new IndexOutOfRangeException();

            return new CraftingStation(this, CraftingStation.START_ADDRESS + index * CraftingStation.LENGTH);
        }
        public IEnumerable<CraftingStation> GetCraftingStations(int index = 0, int count = CraftingStation.MAXIMUM)
        {
            if (index < 0 || index >= CraftingStation.MAXIMUM)
                throw new IndexOutOfRangeException();

            for (int i = 0; i < count && i + index < CraftingStation.MAXIMUM; i++)
                yield return GetCraftingStation(i + index);
        }
        public CraftingStation GetCraftingStationAtPosition(Vector3I position)
        {
            return GetCraftingStations().FirstOrDefault(craft => craft.GetPosition() == position);
        }

        public class Signpost(StageData saveData, int address) : BlockEntity(saveData)
        {
            public const int START_ADDRESS = 0x148FE;
            public const int LENGTH = 0x86; // 134
            public const int MAXIMUM = 20;

            public readonly int Address = address;

            public string Text { get { return SaveData.GetString(Address + 6, 127); } set { SaveData.SetString(Address + 6, value, 127); } }

            public override ushort X { get { return SaveData.GetUInt16(Address); } set { SaveData.SetUInt16(Address, value); } }
            public override ushort Z { get { return SaveData.GetUInt16(Address + 2); } set { SaveData.SetUInt16(Address + 2, value); } }
            public override byte Y { get { return SaveData.GetByte(Address + 4); } set { SaveData.SetByte(Address + 4, value); } }

            public bool Enabled { get { return SaveData.GetBit(Address + 5, 0); } set { SaveData.SetBit(Address + 5, 0, value); } }
            public bool Written { get { return SaveData.GetBit(Address + 5, 1); } set { SaveData.SetBit(Address + 5, 1, value); } }

            public override void Clear()
            {
                SaveData.Fill(0, Address, LENGTH);
            }
        }
        public Signpost GetSignpost(int index)
        {
            if (index < 0 || index >= Signpost.MAXIMUM)
                throw new IndexOutOfRangeException();

            return new Signpost(this, Signpost.START_ADDRESS + index * Signpost.LENGTH);
        }
        public IEnumerable<Signpost> GetSignposts(int index = 0, int count = Signpost.MAXIMUM)
        {
            if (index < 0 || index >= Signpost.MAXIMUM)
                throw new IndexOutOfRangeException();

            for (int i = 0; i < count && i + index < Signpost.MAXIMUM; i++)
                yield return GetSignpost(i + index);
        }
        public Signpost GetSignpostAtPosition(Vector3I position)
        {
            return GetSignposts().FirstOrDefault(signpost => signpost.Enabled && signpost.GetPosition() == position);
        }

        public class SalutationStation(StageData saveData, int address) : BlockEntity(saveData)
        {
            public const int START_ADDRESS = 0x16B36;
            public const int LENGTH = 0xE4;
            public const int MAXIMUM = 64;

            public readonly int Address = address;

            public string Text { get { return SaveData.GetString(Address + 7, 0xD8); } set { SaveData.SetString(Address + 7, value, 0xD8); } }

            public override ushort X { get { return SaveData.GetUInt16(Address); } set { SaveData.SetUInt16(Address, value); } }
            public override byte Y { get { return SaveData.GetByte(Address + 2); } set { SaveData.SetUInt16(Address + 2, value); } }
            public override ushort Z { get { return SaveData.GetUInt16(Address + 4); } set { SaveData.SetUInt16(Address + 4, value); } }

            public bool Enabled { get { return SaveData.GetByte(Address + 6) == 1; } set { SaveData.SetByte(Address + 6, (byte)(value ? 1 : 0)); } }

            public ushort ResidentID { get { return SaveData.GetUInt16(Address + 0xE0); } set { SaveData.SetUInt16(Address + 0xE0, value); SaveData.SetUInt16(Address + 0xE2, value); } } // Duplicated. Why?
            // effectively acts as "Written" flag

            public override void Clear()
            {
                SaveData.Fill(0, Address, LENGTH);
            }    
        }
        public SalutationStation GetSalutationStation(int index)
        {
            if (index < 0 || index >= SalutationStation.MAXIMUM)
                throw new IndexOutOfRangeException();

            return new SalutationStation(this, SalutationStation.START_ADDRESS + index * SalutationStation.LENGTH);
        }
        public IEnumerable<SalutationStation> GetSalutationStations(int index = 0, int count = SalutationStation.MAXIMUM)
        {
            if (index < 0 || index >= SalutationStation.MAXIMUM)
                throw new IndexOutOfRangeException();

            for (int i = 0; i < count && i + index < SalutationStation.MAXIMUM; i++)
                yield return GetSalutationStation(i + index);
        }
        public SalutationStation GetSalutationStationAtPosition(Vector3I position)
        {
            return GetSalutationStations().FirstOrDefault(station => station.Enabled && station.GetPosition() == position);
        }

        public ushort CropCount { get { return GetUInt16(0xF560); } set { SetUInt16(0xF560, value); } }
        public class Crop(StageData saveData, int address) : BlockEntity(saveData)
        {
            public const int START_ADDRESS = 0xB560;
            public const int LENGTH = 0x10;
            public const int MAXIMUM = 1024;

            public readonly int Address = address;

            public override ushort X { get { return SaveData.GetUInt16(Address + 4); } set { SaveData.SetUInt16(Address + 4, value); } }
            public override byte Y { get { return SaveData.GetByte(Address + 8); } set { SaveData.SetUInt16(Address + 8, value); } }
            public override ushort Z { get { return SaveData.GetUInt16(Address + 6); } set { SaveData.SetUInt16(Address + 6, value); } }

            public DQB2Crop CropType { get { return (DQB2Crop)SaveData.GetByte(Address + 9); } set { SaveData.SetByte(Address + 9, (byte)value); } }

            public Span<byte> GetBytes() => SaveData.GetBytes(Address, LENGTH);
            public void SetBytes(byte[] bytes) => SaveData.SetBytes(Address, bytes, LENGTH);

            public override void Clear()
            {
                SaveData.Fill(0, Address, LENGTH);
            }
        }
        public Crop GetCrop(int index)
        {
            if (index < 0 || index >= Crop.MAXIMUM)
                throw new IndexOutOfRangeException();

            return new Crop(this, Crop.START_ADDRESS + index * Crop.LENGTH);
        }
        public IEnumerable<Crop> GetCrops(int index = 0, int count = Crop.MAXIMUM)
        {
            if (index < 0 || index >= Crop.MAXIMUM)
                throw new IndexOutOfRangeException();

            for (int i = 0; i < count && i + index < Crop.MAXIMUM; i++)
                yield return GetCrop(i + index);
        }
        public Crop GetCropAtPosition(Vector3I position)
        {
            return GetCrops().FirstOrDefault(crop => crop.GetPosition() == position);
        }
        public void DefragmentCrops()
        {
            List<byte[]> cropsdata = [];
            for (int i = 0; i < CropCount && i < Crop.MAXIMUM; i++)
            {
                Crop crop = GetCrop(i);
                cropsdata.Add([.. crop.GetBytes()]);
            }

            for (int i = 0; i < cropsdata.Count; i++)
            {
                Crop crop = GetCrop(i);
                crop.SetBytes(cropsdata[i]);
            }

            CropCount = (ushort)cropsdata.Count;
        }

        public class Scarecrow(StageData saveData, int address) : BlockEntity(saveData)
        {
            public const int START_ADDRESS = 0x15376;
            public const int MAXIMUM = 64;
            public const int LENGTH = 7;

            public readonly int Address = address;

            public override ushort X { get { return SaveData.GetUInt16(Address); } set { SaveData.SetUInt16(Address, value); } }
            public override ushort Z { get { return SaveData.GetUInt16(Address + 2); } set { SaveData.SetUInt16(Address + 2, value); } }
            public override byte Y { get { return SaveData.GetByte(Address + 4); } set { SaveData.SetByte(Address + 4, value); } }

            public bool Enabled { get { return SaveData.GetByte(Address + 6) == 1; } set { SaveData.SetByte(Address + 6, (byte)(value ? 1 : 0)); } }

            public DQB2Crop CropType { get { return (DQB2Crop)SaveData.GetByte(Address + 5); } set { SaveData.SetByte(Address + 5, (byte)value); } }

            public override void Clear()
            {
                SaveData.Fill(0, Address, LENGTH);
            }
        }
        public Scarecrow GetScarecrow(int index)
        {
            if (index < 0 || index >= Scarecrow.MAXIMUM)
                throw new IndexOutOfRangeException();

            return new Scarecrow(this, Scarecrow.START_ADDRESS + index * Scarecrow.LENGTH);
        }
        public IEnumerable<Scarecrow> GetScarecrows(int index = 0, int count = Scarecrow.MAXIMUM)
        {
            if (index < 0 || index >= Scarecrow.MAXIMUM)
                throw new IndexOutOfRangeException();

            for (int i = 0; i < count && i + index < Scarecrow.MAXIMUM; i++)
                yield return GetScarecrow(i + index);
        }
        public Scarecrow GetScarecrowAtPosition(Vector3I position)
        {
            return GetScarecrows().FirstOrDefault(scarecrow => scarecrow.Enabled && scarecrow.GetPosition() == position);
        }

        public class Instrument(StageData saveData, int address) : BlockEntity(saveData)
        {
            public const int START_ADDRESS = 0x287E8;
            public const int MAXIMUM = 137;
            public const int LENGTH = 8;

            public readonly int Address = address;

            public override ushort X { get => SaveData.GetUInt16(Address); set => SaveData.SetUInt16(Address, value); }
            public override ushort Z { get => SaveData.GetUInt16(Address + 2); set => SaveData.SetUInt16(Address + 2, value); }
            public override byte Y { get => SaveData.GetByte(Address + 4); set => SaveData.SetByte(Address + 4, value); }

            public byte Song { get => SaveData.GetByte(Address + 5); set => SaveData.SetByte(Address + 5, value); }

            public bool IsPlaying() => Song > 0;
            public bool Exists() => !SaveData.GetBytes(Address, LENGTH).ToArray().All(b => b == 0);

            public override void Clear()
            {
                SaveData.Fill(0, Address, LENGTH);
            }
        }
        public Instrument GetInstrument(int index)
        {
            if (index < 0 || index >= Instrument.MAXIMUM)
                throw new IndexOutOfRangeException();
            
            return new Instrument(this, Instrument.START_ADDRESS + Instrument.LENGTH * index);
        }
        public IEnumerable<Instrument> GetInstruments(int index = 0, int count = Instrument.MAXIMUM)
        {
            if (index < 0 || index >= Instrument.MAXIMUM)
                throw new IndexOutOfRangeException();
            
            for (int i = 0; i < count && i + index < Instrument.MAXIMUM; i++)
                yield return GetInstrument(i + index);
        }
        public Instrument GetInstrumentAtPosition(Vector3I position)
        {
            return GetInstruments().FirstOrDefault(instrument => instrument.GetPosition() == position);
        }

        public class MagneticBlock(StageData saveData, int address) : BlockEntity(saveData)
        {
            public const int START_ADDRESS = 0x1310D;
            public const int LENGTH = 9;
            public const int MAXIMUM = 256;

            public readonly int Address = address;

            public override ushort X { get { return SaveData.GetUInt16(Address); } set { SaveData.SetUInt16(Address, value); } }
            public override byte Y { get { return SaveData.GetByte(Address + 2); } set { SaveData.SetUInt16(Address + 2, value); } }
            public override ushort Z { get { return SaveData.GetUInt16(Address + 4); } set { SaveData.SetUInt16(Address + 4, value); } }

            public ushort Camouflage { get { return (ushort)SaveData.GetNumberBitwise(Address + 7, 0, 13); } set { SaveData.SetNumberBitwise(Address + 7, 0, 13, value); } }
            public bool BGPartsCamouflaged { get { return SaveData.GetBit(Address + 8, 7); } set { SaveData.SetBit(Address + 8, 7, value); } }
            
            public bool Enabled { get { return SaveData.GetByte(Address + 6) == 1; } set { SaveData.SetByte(Address + 6, (byte)(value ? 1 : 0)); } }

            public override void Clear()
            {
                SaveData.Fill(0, Address, LENGTH);
            }
        }
        public MagneticBlock GetMagneticBlock(int index)
        {
            if (index < 0 || index >= MagneticBlock.MAXIMUM)
                throw new IndexOutOfRangeException();

            return new MagneticBlock(this, MagneticBlock.START_ADDRESS + index * MagneticBlock.LENGTH);
        }
        public IEnumerable<MagneticBlock> GetMagneticBlocks(int index = 0, int count = MagneticBlock.MAXIMUM)
        {
            if (index < 0 || index >= MagneticBlock.MAXIMUM)
                throw new IndexOutOfRangeException();

            for (int i = 0; i < count && i + index < MagneticBlock.MAXIMUM; i++)
                yield return GetMagneticBlock(i + index);
        }
        public MagneticBlock GetMagneticBlockAtPosition(Vector3I position)
        {
            return GetMagneticBlocks().FirstOrDefault(block => block.Enabled && block.GetPosition() == position);
        }

        public class MagicPencil(StageData saveData, int address) : BlockEntity(saveData)
        {
            public const int START_ADDRESS = 0x13DB5;
            public const int LENGTH = 7;
            public const int MAXIMUM = 2;

            public readonly int Address = address;

            public override ushort X { get => SaveData.GetUInt16(Address); set => SaveData.SetUInt16(Address, value); }
            public override byte Y { get => SaveData.GetByte(Address + 2); set => SaveData.SetByte(Address + 2, value); }  
            public override ushort Z { get => SaveData.GetUInt16(Address + 4); set => SaveData.SetUInt16(Address + 4, value); }

            public bool Enabled { get => SaveData.GetByte(Address + 6) == 1; set => SaveData.SetByte(Address + 6, (byte)(value ? 1 : 0)); }

            public override void Clear()
            {
                SaveData.Fill(0, Address, LENGTH);
            }
        }
        public MagicPencil GetMagicPencil(int index)
        {
            if (index < 0 || index >= MagicPencil.MAXIMUM)
                throw new IndexOutOfRangeException();

            return new MagicPencil(this, MagicPencil.START_ADDRESS + index * MagicPencil.LENGTH);
        }
        public IEnumerable<MagicPencil> GetMagicPencils(int index = 0, int count = MagicPencil.MAXIMUM)
        {
            if (index < 0 || index >= MagicPencil.MAXIMUM)
                throw new IndexOutOfRangeException();

            for (int i = 0; i < count && i + index < MagicPencil.MAXIMUM; i++)
                yield return GetMagicPencil(i + index);
        }
        public MagicPencil GetMagicPencilAtPosition(Vector3I position)
        {
            return GetMagicPencils().FirstOrDefault(pencil => pencil.Enabled && pencil.GetPosition() == position);
        }

        public class FireworkCannon(StageData saveData, int address) : BlockEntity(saveData)
        {
            public const int START_ADDRESS = 0x12DCD;
            public const int LENGTH = 9;
            public const int MAXIMUM = 64;

            public readonly int Address = address;

            public override ushort X { get { return SaveData.GetUInt16(Address); } set { SaveData.SetUInt16(Address, value); } } // This might be signed, but even if it is it's kind of pointless.
            public override byte Y { get { return SaveData.GetByte(Address + 2); } set { SaveData.SetUInt16(Address + 2, value); } }
            public override ushort Z { get { return SaveData.GetUInt16(Address + 4); } set { SaveData.SetUInt16(Address + 4, value); } }

            public bool Enabled { get { return SaveData.GetByte(Address + 6) == 1; } set { SaveData.SetByte(Address + 6, (byte)(value ? 1 : 0)); } }

            public ushort Color { get { return (ushort)SaveData.GetNumberBitwise(Address + 7, 0, 15); } set { SaveData.SetNumberBitwise(Address + 7, 0, 15, value); } }

            public override void Clear()
            {
                SaveData.Fill(0, Address, LENGTH);
                Color = 0x7FFF;
            }
        }
        public FireworkCannon GetFireworkCannon(int index)
        {
            if (index < 0 || index >= FireworkCannon.MAXIMUM)
                throw new IndexOutOfRangeException();

            return new FireworkCannon(this, FireworkCannon.START_ADDRESS + index * FireworkCannon.LENGTH);
        }
        public IEnumerable<FireworkCannon> GetFireworkCannons(int index = 0, int count = FireworkCannon.MAXIMUM)
        {
            if (index < 0 || index >= FireworkCannon.MAXIMUM)
                throw new IndexOutOfRangeException();

            for (int i = 0; i < count && i + index < FireworkCannon.MAXIMUM; i++)
                yield return GetFireworkCannon(i + index);
        }
        public FireworkCannon GetFireworkCannonAtPosition(Vector3I position)
        {
            return GetFireworkCannons().FirstOrDefault(cannon => cannon.Enabled && cannon.GetPosition() == position);
        }
        
        public class PictureFrame(StageData saveData, int address) : BlockEntity(saveData)
        {
            public const int START_ADDRESS = 0x1300D;
            public const int LENGTH = 8;
            public const int MAXIMUM = 10; // There's slots for 32 but everything after 10 is non-functional

            public readonly int Address = address;

            public override ushort X { get { return SaveData.GetUInt16(Address); } set { SaveData.SetUInt16(Address, value); } }
            public override byte Y { get { return SaveData.GetByte(Address + 2); } set { SaveData.SetUInt16(Address + 2, value); } }
            public override ushort Z { get { return SaveData.GetUInt16(Address + 4); } set { SaveData.SetUInt16(Address + 4, value); } }

            public bool Enabled { get { return SaveData.GetByte(Address + 6) == 1; } set { SaveData.SetByte(Address + 6, (byte)(value ? 1 : 0)); } }

            public byte Screenshot { get { return SaveData.GetByte(Address + 7); } set { SaveData.SetByte(Address + 7, value); } }

            public override void Clear()
            {
                SaveData.Fill(0, Address, LENGTH);
                Screenshot = byte.MaxValue;
            }
        }
        public PictureFrame GetPictureFrame(int index)
        {
            if (index < 0 || index >= PictureFrame.MAXIMUM)
                throw new IndexOutOfRangeException();

            return new PictureFrame(this, PictureFrame.START_ADDRESS + index * PictureFrame.LENGTH);
        }
        public IEnumerable<PictureFrame> GetPictureFrames(int index = 0, int count = PictureFrame.MAXIMUM)
        {
            if (index < 0 || index >= PictureFrame.MAXIMUM)
                throw new IndexOutOfRangeException();

            for (int i = 0; i < count && i + index < PictureFrame.MAXIMUM; i++)
                yield return GetPictureFrame(i + index);
        }
        public PictureFrame GetPictureFrameAtPosition(Vector3I position)
        {
            return GetPictureFrames().FirstOrDefault(frame => frame.Enabled && frame.GetPosition() == position);
        }
        
        public class Watchfire(StageData saveData, int address) : BlockEntity(saveData)
        {
            public const int START_ADDRESS = 0x13A0D;
            public const int LENGTH = 7;
            public const int MAXIMUM = 64;

            public readonly int Address = address;

            public override ushort X { get { return SaveData.GetUInt16(Address); } set { SaveData.SetUInt16(Address, value); } }
            public override byte Y { get { return SaveData.GetByte(Address + 2); } set { SaveData.SetUInt16(Address + 2, value); } }
            public override ushort Z { get { return SaveData.GetUInt16(Address + 4); } set { SaveData.SetUInt16(Address + 4, value); } }

            public bool Enabled { get { return SaveData.GetByte(Address + 6) == 1; } set { SaveData.SetByte(Address + 6, (byte)(value ? 1 : 0)); } }

            public override void Clear()
            {
                SaveData.Fill(0, Address, LENGTH);
            }
        }
        public Watchfire GetWatchfire(int index)
        {
            if (index < 0 || index >= Watchfire.MAXIMUM)
                throw new IndexOutOfRangeException();

            return new Watchfire(this, Watchfire.START_ADDRESS + index * Watchfire.LENGTH);
        }
        public IEnumerable<Watchfire> GetWatchfires(int index = 0, int count = Watchfire.MAXIMUM)
        {
            if (index < 0 || index >= Watchfire.MAXIMUM)
                throw new IndexOutOfRangeException();

            for (int i = 0; i < count && i + index < Watchfire.MAXIMUM; i++)
                yield return GetWatchfire(i + index);
        }
        public Watchfire GetWatchfireAtPosition(Vector3I position)
        {
            return GetWatchfires().FirstOrDefault(watchfire => watchfire.Enabled && watchfire.GetPosition() == position);
        }
        
        public class WardOfErdrick(StageData saveData, int address) : BlockEntity(saveData)
        {
            public const int START_ADDRESS = 0x145258;
            public const int LENGTH = 6;
            public const int MAXIMUM = 4;

            public readonly int Address = address;

            public override ushort X { get { return SaveData.GetUInt16(Address); } set { SaveData.SetUInt16(Address, value); } }
            public override byte Y { get { return SaveData.GetByte(Address + 4); } set { SaveData.SetUInt16(Address + 4, value); } }
            public override ushort Z { get { return SaveData.GetUInt16(Address + 2); } set { SaveData.SetUInt16(Address + 2, value); } }

            public bool Enabled { get { return SaveData.GetByte(Address + 5) == 1; } set { SaveData.SetByte(Address + 5, (byte)(value ? 1 : 0)); } }

            public override void Clear()
            {
                SaveData.Fill(0, Address, LENGTH);
            }
        }
        public WardOfErdrick GetWardOfErdrick(int index)
        {
            if (index < 0 || index >= WardOfErdrick.MAXIMUM)
                throw new IndexOutOfRangeException();

            return new WardOfErdrick(this, WardOfErdrick.START_ADDRESS + index * WardOfErdrick.LENGTH);
        }
        public IEnumerable<WardOfErdrick> GetWardsOfErdrick(int index = 0, int count = WardOfErdrick.MAXIMUM)
        {
            if (index < 0 || index >= WardOfErdrick.MAXIMUM)
                throw new IndexOutOfRangeException();

            for (int i = 0; i < count && i + index < WardOfErdrick.MAXIMUM; i++)
                yield return GetWardOfErdrick(i + index);
        }
        public WardOfErdrick GetWardOfErdrickAtPosition(Vector3I position)
        {
            return GetWardsOfErdrick().FirstOrDefault(ward => ward.Enabled &&  ward.GetPosition() == position);
        }
        
        public class Buggy(StageData saveData, int address) : BlockEntity(saveData)
        {
            public const int START_ADDRESS = 0x13D8D;
            public const int LENGTH = 10;
            public const int MAXIMUM = 4;

            public readonly int Address = address;

            public override ushort X { get { return SaveData.GetUInt16(Address); } set { SaveData.SetUInt16(Address, value); } }
            public override byte Y { get { return SaveData.GetByte(Address + 2); } set { SaveData.SetUInt16(Address + 2, value); } }
            public override ushort Z { get { return SaveData.GetUInt16(Address + 4); } set { SaveData.SetUInt16(Address + 4, value); } }

            public bool Enabled { get { return SaveData.GetByte(Address + 6) == 1; } set { SaveData.SetByte(Address + 5, (byte)(value ? 1 : 0)); } }

            // Bytes 7, 8, and 9 appear to always be 0xC3, 0x0A, 0x02 while the prop is Enabled.

            public override void Clear()
            {
                SaveData.Fill(0, Address, LENGTH);
            }
        }
        public Buggy GetBuggy(int index)
        {
            if (index < 0 || index >= Buggy.MAXIMUM)
                throw new IndexOutOfRangeException();

            return new Buggy(this, Buggy.START_ADDRESS + index * Buggy.LENGTH);
        }
        public IEnumerable<Buggy> GetBuggies(int index = 0, int count = Buggy.MAXIMUM)
        {
            if (index < 0 || index >= Buggy.MAXIMUM)
                throw new IndexOutOfRangeException();

            for (int i = 0; i < count && i + index < Buggy.MAXIMUM; i++)
                yield return GetBuggy(i + index);
        }
        public Buggy GetBuggyAtPosition(Vector3I position)
        {
            return GetBuggies().FirstOrDefault(buggy => buggy.Enabled && buggy.GetPosition() == position);
        }
        
        public class Toilet(StageData saveData, int address) : BlockEntity(saveData)
        {
            public const int START_ADDRESS = 0x12A65;
            public const int LENGTH = 8;
            public const int MAXIMUM = 64;

            public readonly int Address = address;

            public override ushort X { get { return SaveData.GetUInt16(Address); } set { SaveData.SetUInt16(Address, value); } }
            public override byte Y { get { return SaveData.GetByte(Address + 2); } set { SaveData.SetUInt16(Address + 2, value); } }
            public override ushort Z { get { return SaveData.GetUInt16(Address + 4); } set { SaveData.SetUInt16(Address + 4, value); } }

            public bool Enabled { get { return SaveData.GetByte(Address + 6) == 1; } set { SaveData.SetByte(Address + 5, (byte)(value ? 1 : 0)); } }

            public override void Clear()
            {
                SaveData.Fill(0, Address, LENGTH);
            }
        }
        public Toilet GetToilet(int index)
        {
            if (index < 0 || index >= Toilet.MAXIMUM)
                throw new IndexOutOfRangeException();

            return new Toilet(this, Toilet.START_ADDRESS + index * Toilet.LENGTH);
        }
        public IEnumerable<Toilet> GetToilets(int index = 0, int count = Toilet.MAXIMUM)
        {
            if (index < 0 || index >= Toilet.MAXIMUM)
                throw new IndexOutOfRangeException();

            for (int i = 0; i < count && i + index < Toilet.MAXIMUM; i++)
                yield return GetToilet(i + index);
        }
        public Toilet GetToiletAtPosition(Vector3I position)
        {
            return GetToilets().FirstOrDefault(toilet => toilet.Enabled && toilet.GetPosition() == position);
        }
        #endregion

        #region Other Classes
        public class Room(StageData saveData, int index)
        {
            public const int START_ADDRESS = 0x10;
            public const int LENGTH = 0x150;
            public const int MAXIMUM = 100; // Maybe?

            public readonly StageData SaveData = saveData;
            public readonly int Index = index;

            public int GetAddress() => START_ADDRESS + LENGTH * Index;

            public ushort X { get => SaveData.GetUInt16(GetAddress() + 0x14); set => SaveData.SetUInt16(GetAddress() + 0x14, value); }
            public ushort Z { get => SaveData.GetUInt16(GetAddress() + 0x16); set => SaveData.SetUInt16(GetAddress() + 0x16, value); }
            public byte Y { get => SaveData.GetByte(GetAddress() + 0x1C); set => SaveData.SetByte(GetAddress() + 0x1C, value); }

            public Vector3I GetPosition() => new(X, Y, Z);

            public byte SizeX { get => SaveData.GetByte(GetAddress() + 0x1D); set => SaveData.SetByte(GetAddress() + 0x1D, value); }
            public byte SizeZ { get => SaveData.GetByte(GetAddress() + 0x1F); set => SaveData.SetByte(GetAddress() + 0x1F, value); }

            public ushort RoomType { get => SaveData.GetUInt16(GetAddress() + 2); set => SaveData.SetUInt16(GetAddress() + 2, value); }

            public uint Fanciness { get => SaveData.GetUInt32(GetAddress() + 4); set => SaveData.SetUInt32(GetAddress() + 4, value); }

            public ushort Cuteness { get => SaveData.GetUInt16(GetAddress() + 0x8); set => SaveData.SetUInt16(GetAddress() + 0x8, value); }
            public ushort Coolness { get => SaveData.GetUInt16(GetAddress() + 0xA); set => SaveData.SetUInt16(GetAddress() + 0xA, value); }
            public ushort Naturalness { get => SaveData.GetUInt16(GetAddress() + 0xC); set => SaveData.SetUInt16(GetAddress() + 0xC, value); }
            public ushort Flamboyantness { get => SaveData.GetUInt16(GetAddress() + 0xE); set => SaveData.SetUInt16(GetAddress() + 0xE, value); }
            public ushort Cheekiness { get => SaveData.GetUInt16(GetAddress() + 0x10); set => SaveData.SetUInt16(GetAddress() + 0x10, value); }
            public ushort Normalness { get => SaveData.GetUInt16(GetAddress() + 0x12); set => SaveData.SetUInt16(GetAddress() + 0x12, value); }
        }
        public Room GetRoom(int index)
        {
            if (index < 0 || index >= Room.MAXIMUM)
                throw new IndexOutOfRangeException();

            return new Room(this, index);
        }
        public IEnumerable<Room> GetRooms(int index = 0, int count = Room.MAXIMUM)
        {
            if (index < 0 || index >= Room.MAXIMUM)
                throw new IndexOutOfRangeException();

            for (int i = 0; i < count && i + index < Room.MAXIMUM; i++)
                yield return GetRoom(i + index);
        }
        
        public class BlueprintInstance(StageData saveData, int address)
        {
            public const int START_ADDRESS = 0x2CA3C;
            public const int LENGTH = 12;
            public const int MAXIMUM = 5;

            public readonly StageData SaveData = saveData;
            public readonly int Address = address;

            public ushort ID { get { return SaveData.GetUInt16(Address); } set { SaveData.SetUInt16(Address, value); } } // TODO: Make list of blueprint IDs

            public ushort X { get { return SaveData.GetUInt16(Address + 2); } set { SaveData.SetUInt16(Address + 2, value); } }
            public ushort Z { get { return SaveData.GetUInt16(Address + 4); } set { SaveData.SetUInt16(Address + 4, value); } }
            public byte Y { get { return SaveData.GetByte(Address + 6); } set { SaveData.SetUInt16(Address + 6, value); } }
            // No clue what determines the pivot point.....
        }
        public BlueprintInstance GetBlueprintInstance(int index)
        {
            if (index < 0 || index >= BlueprintInstance.MAXIMUM)
                throw new IndexOutOfRangeException();
            
            return new BlueprintInstance(this, BlueprintInstance.START_ADDRESS + BlueprintInstance.LENGTH * index);
        }
        public IEnumerable<BlueprintInstance> GetBlueprintInstances(int index = 0, int count = BlueprintInstance.MAXIMUM)
        {
            if (index < 0 || index >= BlueprintInstance.MAXIMUM)
                throw new IndexOutOfRangeException();
            
            for (int i = 0; i < count && i + index < BlueprintInstance.MAXIMUM; i++)
                yield return GetBlueprintInstance(i + index);
        }

        public class Field(StageData saveData, int address)
        {
            public const int START_ADDRESS = 0x2CA9C;
            public const int LENGTH = 0xE;
            public const int MAXIMUM = 64;

            public readonly StageData SaveData = saveData;
            public readonly int Address = address;

            public ushort X { get { return SaveData.GetUInt16(Address); } set { SaveData.SetUInt16(Address, value); } } // Northernmost corner
            public ushort Z { get { return SaveData.GetUInt16(Address + 2); } set { SaveData.SetUInt16(Address + 2, value); } } // Westernmost corner

            public ushort ScarecrowX { get { return SaveData.GetUInt16(Address + 4); } set { SaveData.SetUInt16(Address + 4, value); } }
            public ushort ScarecrowZ { get { return SaveData.GetUInt16(Address + 6); } set { SaveData.SetUInt16(Address + 6, value); } }
            public byte ScarecrowY { get { return SaveData.GetByte(Address + 8); } set { SaveData.SetByte(Address + 8, value); } }

            public byte SizeX { get { return SaveData.GetByte(Address + 9); } set { SaveData.SetByte(Address + 9, value); } } // East-West (X)
            public byte SizeZ { get { return SaveData.GetByte(Address + 9); } set { SaveData.SetByte(Address + 9, value); } } // North-South (Z)

            public ushort Crop { get { return SaveData.GetUInt16(Address + 0xC); } set { SaveData.SetUInt16(Address + 0xC, value); } }

            // It seems like it's perfectly fine to delete the scarecrow entry without deleting the corresponding field. The field automatically goes away if there's no scarecrow in its place.
        }
        public Field GetField(int index)
        {
            if (index < 0 || index >= Field.MAXIMUM)
                throw new IndexOutOfRangeException();
            
            return new Field(this, Field.START_ADDRESS + Field.LENGTH * index);
        }
        public IEnumerable<Field> GetFields(int index = 0, int count = Field.MAXIMUM)
        {
            if (index < 0 || index >= Field.MAXIMUM)
                throw new IndexOutOfRangeException();
            
            for (int i = 0; i < count && i + index < Field.MAXIMUM; i++)
                yield return GetField(i + index);
        }

        public class Drop(StageData saveData, int address)
        {
            public const int START_ADDRESS = 0x24334C;
            public const int LENGTH = 0x15;
            public const int MAXIMUM = 500;

            public readonly StageData SaveData = saveData;
            public readonly int Address = address;

            public InventoryItem Item { get { return new InventoryItem(SaveData, Address); } }

            public float X { get { return SaveData.GetSingle(Address + 0x4); } set { SaveData.SetSingle(Address + 0x4, value); } }
            public float Y { get { return SaveData.GetSingle(Address + 0x8); } set { SaveData.SetSingle(Address + 0x8, value); } }
            public float Z { get { return SaveData.GetSingle(Address + 0xC); } set { SaveData.SetSingle(Address + 0xC, value); } }

            public void Clear()
            {
                InventoryItem item = Item;
                item.ItemID = 0;
                item.Count = 0;
            }
        }
        public Drop GetDrop(int index = 0)
        {
            if (index < 0 || index >= Drop.MAXIMUM)
                throw new IndexOutOfRangeException();
            
            return new Drop(this, Drop.START_ADDRESS + Drop.LENGTH * index);
        }
        public IEnumerable<Drop> GetDrops(int index = 0, int count = Drop.MAXIMUM)
        {
            if (index < 0 || index >= Drop.MAXIMUM)
                throw new IndexOutOfRangeException();
            
            for (int i = 0; i < count && i + index < Drop.MAXIMUM; i++)
                yield return GetDrop(i + index);
        }
        
        public class Fish(StageData saveData, int address)
        {
            public const int START_ADDRESS = 0x28DEC;
            public const int LENGTH = 0x10;
            public const int MAXIMUM = 50;

            public readonly StageData SaveData = saveData;
            public readonly int Address = address;

            public ushort FishType { get { return SaveData.GetUInt16(Address); } set { SaveData.SetUInt16(Address, value); } }

            public float X { get { return SaveData.GetSingle(Address + 2); } set { SaveData.SetSingle(Address + 2, value); } }
            public float Y { get { return SaveData.GetSingle(Address + 6); } set { SaveData.SetSingle(Address + 6, value); } }
            public float Z { get { return SaveData.GetSingle(Address + 10); } set { SaveData.SetSingle(Address + 10, value); } }

            public void Clear()
            {
                SaveData.Fill(0, Address, LENGTH);
            }
        }
        public Fish GetFish(int index = 0)
        {
            if (index < 0 || index >= Fish.MAXIMUM)
                throw new IndexOutOfRangeException();
            
            return new Fish(this, Fish.START_ADDRESS + Fish.LENGTH * index);
        }
        public IEnumerable<Fish> GetFishes(int index = 0, int count = Fish.MAXIMUM)
        {
            if (index < 0 || index >= Fish.MAXIMUM)
                throw new IndexOutOfRangeException();
            
            for (int i = 0; i < count && i + index < Fish.MAXIMUM; i++)
                yield return GetFish(i + index);
        }
        #endregion

        #region Biome Map Data
        public class BiomeMapData(StageData saveData, int index)
        {
            public const int START_ADDRESS = 0x34EC8;
            public const int LENGTH = 35;
            public const int MAXIMUM = 0x4000;

            public readonly StageData SaveData = saveData;
            public readonly int Index = index;

            public int GetAddress() => START_ADDRESS + Index * LENGTH;

            public ushort Biome { get { return SaveData.GetUInt16(GetAddress()); } set { SaveData.SetUInt16(GetAddress(), value); } }
            public ushort Diorama { get { return SaveData.GetUInt16(GetAddress() + 0x10); } set { SaveData.SetUInt16(GetAddress() + 0x10, value); } }
            public byte Area { get { return SaveData.GetByte(GetAddress() + 0x22); } set { SaveData.SetByte(GetAddress() + 0x22, value); } }
        }

        public BiomeMapData GetBiomeMapData(Vector3I position)
        {
            int x = position.X / 16;
            int z = position.Z / 16;
            int index = z + x * 128;
            return new BiomeMapData(this, index);
        }
        #endregion
    }
}