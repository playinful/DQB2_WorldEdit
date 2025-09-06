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
using EyeOfRubiss.Info;
using EyeOfRubiss.Nodes;
using System.Data.SqlTypes;

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

        private const int HEADER_LENGTH = 0x110;

        /*
            The chunk grid is a 64x64 grid of chunk IDs. Each chunk ID is a 16-bit integer. The grid is laid out from left to right, and then from top to bottom.
            Each integer ID starting with 0 points to the position of the chunk's blocks in the data. So, the formula to find out the position of a chunk's block data is as follows:
                BlockAddress + (ChunkSize * {ID})
            For chunks without block data, their ID is instead set to 0xFFFF.
        */

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

        public static StageData Instance { get; private set; }
        public static bool HasInstance() => Instance is not null && Instance.IsLoaded;

        public byte IslandID { get => GetByte(0xC0ED6); set => SetByte(0xC0ED6, value); }
        public int Gratitude { get => GetInt32(0xC0ECC); set => SetInt32(0xC0ECC, value); }
        public float Time { get => GetSingle(0xC0F50); set => SetSingle(0xC0F50, value); }
        public byte Weather { get => GetByte(0xC0F54); set => SetByte(0xC0F54, value); }

        public ushort ChunkCount { get => GetUInt16(0x1451AF); set => SetUInt16(0x1451AF, value); }
        public int PropCount { get => GetInt32(0x24E7CD); set => SetInt32(0x24E7CD, value); }

        public static StageData TryLoadAndSet(string path)
        {
            if (TryLoad(path) is StageData stageData)
            {
                return Instance = stageData;
            }
            else return null;
        }
        public static StageData TryLoad(string path)
        {
            StageData stageData = new();
            if (stageData._TryLoad(path, HEADER_LENGTH))
            {
                return stageData;
            }
            else return null;
        }

        public override void Save(string path = null)
        {
            DefragmentChunks();
            base.Save(path);
        }

        public static void Close()
        {
            Instance = null;
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
        public static Vector3I PositionToBlockPosition(Vector3I position)
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

            Vector3I blockPosition = PositionToBlockPosition(position);

            Chunk chunk = GetChunk(blockPosition.X);

            if (chunk is null || !chunk.IsUsed())
                return null;

            return chunk.GetBlock(blockPosition.Y, blockPosition.Z);
        }
        public bool SetBlockAtPosition(Vector3I position, ushort blockId, BlockInstance.ChiselType? chisel = null, bool? playerPlaced = null, bool createChunk = false)
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
            if (chisel is BlockInstance.ChiselType _chisel)
                block.Chisel = _chisel;
            if (playerPlaced is bool _playerPlaced)
                block.PlayerPlaced = _playerPlaced;

            return true;
        }
        public bool SetBlockAtPosition(Vector3I position, BlockInstance block, bool createChunk = false)
        {
            return SetBlockAtPosition(position, block.BlockID, block.Chisel, block.PlayerPlaced, createChunk);
        }

        public int GetSeaLevel()
        {
            if (IslandID == 12 || IslandID == 13 || IslandID == 16)
            {
                if (!CommonData.HasInstance())
                    return -1;

                var buildertopiaType = IslandID switch
                {
                    12 => CommonData.Instance.Buildertopia1Type,
                    13 => CommonData.Instance.Buildertopia2Type,
                    16 => CommonData.Instance.Buildertopia3Type,
                    _ => 0,
                };

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
                4 => 19, // Moonbrooke
                5 => -1, // Malhalla
                9 => 31, // Angler's Isle
                10 => 11, // Skelkatraz
                _ => -1,
            };
        }

        public void MakeSuperflat(List<ushort> layers, bool deleteProps = true)
        {
            if (deleteProps)
                DeleteAllProps();

            foreach (Chunk chunk in GetUsedChunks())
            {
                chunk.Clear();
                for (int i = 0; i < layers.Count; i++)
                {
                    chunk.SetLayer(i, layers[i]);
                }
                // BIG NOTE: if there's a block with additional data (like a lore book generated with the world that u can read) removing it will break the superflat
                // TODO FIXME
                //if (chunk.ID >= 163)
                //    break;
            }

            ClearRoomData();
        }
        public void ClearRoomData()
        {
            // I'm not sure entirely what this does. I don't think I really need to know.
            for (int i = 0; i < 100; i++)
            {
                int address = 0x10 + i * 336;
                Fill(0, address, 36);
                Fill(0xFF, address + 36, 300);
            }
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
            public BlockInstance GetBlock(int layer, int tile)
            {
                return GetBlock(layer * LAYER_LENGTH / 2 + tile);
            }
            public IEnumerable<BlockInstance> GetAllBlocks()
            {
                for (int i = 0; i * BlockInstance.LENGTH < LAYER_LENGTH; i++)
                    yield return GetBlock(i);
            }

            public void SetBlock(int layer, int tile, ushort blockId, bool? playerPlaced = null, BlockInstance.ChiselType? chisel = null)
            {
                if (!IsUsed())
                    return;

                BlockInstance block = GetBlock(layer, tile);
                block.BlockID = blockId;
                if (playerPlaced is not null)
                    block.PlayerPlaced = (bool)playerPlaced;
                if (chisel is not null)
                    block.Chisel = (BlockInstance.ChiselType)chisel;
            }
            public void SetLayer(int layer, ushort block)
            {
                for (int i = 0; i < LAYER_LENGTH / BlockInstance.LENGTH; i++)
                {
                    SetBlock(layer, i, block);
                }
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

            public readonly StageData SaveData = saveData;
            public readonly int Address = address;

            public ushort BlockID { get => (ushort)SaveData.GetNumberBitwise(Address, 0, 11); set => SaveData.SetNumberBitwise(Address, 0, 11, value); }
            public bool PlayerPlaced { get => SaveData.GetBit(Address + 1, 3); set => SaveData.SetBit(Address + 1, 3, value); }
            public ChiselType Chisel { get => (ChiselType)SaveData.GetNumberBitwise(Address + 1, 4, 4); set => SaveData.SetNumberBitwise(Address + 1, 4, 4, (byte)value); }

            public BlockInfo GetInfo() => BlockInfo.Get(BlockID);

            public enum ChiselType : byte
            {
                FullBlock = 0,
                DiagonalNorth = 1,
                DiagonalNorthwest = 2,
                DiagonalWest = 3,
                DiagonalSouthwest = 4,
                DiagonalSouth = 5,
                DiagonalSoutheast = 6,
                DiagonalEast = 7,
                DiagonalNortheast = 8,
                ConcaveNorthwest = 9,
                ConcaveSouthwest = 10,
                ConcaveSoutheast = 11,
                ConcaveNortheast = 12,
                TopHalf = 13,
                BottomHalf = 14,
                UNDEFINED = 15
            }
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

        #region Props
        public class Prop(StageData saveData, int index)
        {
            // NOTE: seems like first (0th) prop is unused
            // TODO: Rewrite this so it's more clear where the info and data is catalogued

            public const int START_ADDRESS = 0x24E7D1;
            public const int START_ADDRESS_METADATA = 0x150E7D1;
            public const int LENGTH = 24;
            public const int LENGTH_METADATA = 4;
            public const int MAXIMUM = 0xC8000;

            public StageData SaveData { get; set; } = saveData;
            public int Index { get; set; } = index; // todo fix

            public int GetAddress() => START_ADDRESS + (int)DataIndex * LENGTH;
            public int GetMetadataAddress() => START_ADDRESS_METADATA + Index * LENGTH_METADATA;

            public Span<byte> GetBytes() => SaveData.GetBytes(GetAddress(), LENGTH);

            public ushort Chunk { get { return (ushort)SaveData.GetNumberBitwise(GetMetadataAddress(), 0, 12); } set { SaveData.SetNumberBitwise(GetMetadataAddress(), 0, 12, value); } }
            public uint DataIndex { get { return SaveData.GetNumberBitwise(GetMetadataAddress(), 12, 20); } set { SaveData.SetNumberBitwise(GetMetadataAddress(), 12, 20, value); } }

            public ushort PropID { get { return (ushort)SaveData.GetNumberBitwise(GetAddress() + 8, 0, 13); } set { SaveData.SetNumberBitwise(GetAddress() + 8, 0, 13, value); } }

            public byte X { get { return (byte)SaveData.GetNumberBitwise(GetAddress() + 9, 5, 5); } set { SaveData.SetNumberBitwise(GetAddress() + 9, 5, 5, value); } }
            public byte Y { get { return (byte)SaveData.GetNumberBitwise(GetAddress() + 10, 2, 7); } set { SaveData.SetNumberBitwise(GetAddress() + 10, 2, 7, value); } }
            public byte Z { get { return (byte)SaveData.GetNumberBitwise(GetAddress() + 11, 1, 5); } set { SaveData.SetNumberBitwise(GetAddress() + 11, 1, 5, value); } }
            public byte Rotation { get { return (byte)SaveData.GetNumberBitwise(GetAddress() + 11, 6, 2); } set { SaveData.SetNumberBitwise(GetAddress() + 11, 6, 2, value); } }

            public Vector3I GetPosition() => new(X + Chunk % 64 * 32, Y, Z + Chunk / 64 * 32);

            /// <summary> It's unclear what this is for. It must always be set to (Index * 16). </summary>
            private uint MagicNumberSixteen { get { return SaveData.GetNumberBitwise(GetAddress() + 12, 0, 24); } set { SaveData.SetNumberBitwise(GetAddress() + 12, 0, 24, value); } }

            public PropInfo GetInfo() => PropInfo.Get(PropID);

            public bool Exists() => Chunk != 0xFFF && PropID != 0 && DataIndex < SaveData.PropCount;

            public int GetGridMapRotation()
            {
                return Rotation switch
                {
                    1 => 16,
                    2 => 10,
                    3 => 22,
                    _ => 0,
                };
            }

            public void Clear()
            {
                Chunk = 0xFFF;
                SaveData.Fill(0, GetAddress(), LENGTH);
                MagicNumberSixteen = DataIndex * 16;
            }
        }

        public Prop GetProp(int index)
        {
            if (index < 0 || index >= Prop.MAXIMUM)
                throw new IndexOutOfRangeException();

            return new Prop(this, index);
        }
        public IEnumerable<Prop> GetProps(int index = 0, int count = Prop.MAXIMUM)
        {
            if (index < 0 || index >= Prop.MAXIMUM)
                throw new IndexOutOfRangeException();

            for (int i = 0; i < count && i + index < Prop.MAXIMUM; i++)
                yield return GetProp(i + index);
        }

        public Prop GetPropAtPosition(Vector3I position)
        {
            return GetProps().FirstOrDefault(prop => { return prop.Exists() && prop.GetPosition() == position; });
        }

        public void AddProp(Vector3I position, ushort propId)
        {
            Prop prop = GetFirstUnusedProp();
            prop.PropID = propId;
            prop.X = (byte)(position.X % 32);
            prop.Y = (byte)position.Y;
            prop.Z = (byte)(position.Z % 32);

            prop.Chunk = PositionToChunkIndex(position);

            if (prop.DataIndex >= PropCount)
                PropCount = (int)prop.DataIndex + 1;
        }
        public Prop GetFirstUnusedProp()
        {
            return GetProps().FirstOrDefault(prop => prop.Index > 0 && !prop.Exists());
        }

        public void DeleteAllProps()
        {
            SetNumberBitwise(0x24E7CD, 0, 12, 1); // I don't know why this is required.

            foreach (Prop prop in GetProps())
            {
                prop.Clear();
            }
        }
        #endregion

        #region Block Entities
        public BlockEntity GetBlockEntityAtPosition(Vector3I position)
        {
            throw new NotImplementedException();
        }

        public abstract class BlockEntity(StageData saveData)
        {
            public readonly StageData SaveData = saveData;

            abstract public ushort X { get; set; }
            abstract public byte Y { get; set; }
            abstract public ushort Z { get; set; }

            public virtual Vector3I GetPosition() => new(X, Y, Z);
        }

        public class Storage(StageData saveData, int info, int inside) : BlockEntity(saveData)
        {
            public int Info = info;
            public int Inside = inside;

            public const int ITEM_COUNT = 30;

            public override ushort X { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public override byte Y { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public override ushort Z { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public InventoryItem GetItem(int index)
            {
                if (index < 0 || index >= ITEM_COUNT)
                    throw new IndexOutOfRangeException();

                return new(SaveData, Inside + index * 4);
            }
            public IEnumerable<InventoryItem> GetItems(int index = 0, int length = ITEM_COUNT)
            {
                if (index < 0 || index >= ITEM_COUNT)
                    throw new IndexOutOfRangeException();

                for (int i = index; i < length; i++)
                    yield return GetItem(i);
            }

            public bool IsActive() => !(SaveData.GetInt32(Info) == 0 && SaveData.GetInt32(Info + 4) == 0);

            // Chests: 0x2467CC - 0x2476CC (30 slots * 4 bytes = 120 bytes * 32 chests = 3840 bytes); Info = F565
            // Wardrobes: 0x248ABC - 0x24A8BC (30 slots * 4 bytes = 120 bytes * 64 chests = 7680 bytes); "Info" for "cabinets" starts at 0xFF75, 8 bytes each
            // Cupboards: 0x24A9B0 - 0x24B130 (30 slots * 4 bytes = 120 bytes * 16 chests = 1920 bytes); "Info" starts at 0xF565, 8 bytes each
            // Drawers: 0x24B134 - 0x24C034 (30 slots * 4 bytes = 120 bytes * 32 chests = 3840 bytes); "Info" starts at 0xF565, 8 bytes each
        }
        public class ItemDisplay(StageData saveData, int info, int inside) : BlockEntity(saveData)
        {
            public const int START_ADDRESS_METADATA = 0xF575 - 8 * (128 + 32);
            public const int START_ADDRESS_INVENTORY = 0x2486BC;
            public const int LENGTH_METADATA = 8;
            public const int LENGTH_INVENTORY = 4;

            public readonly int Info = info;
            public readonly int Inside = inside;

            public InventoryItem Item => new(SaveData, Inside);

            public override ushort X { get { return SaveData.GetUInt16(Info); } set { SaveData.SetUInt16(Info, value); } }
            public override byte Y { get { return SaveData.GetByte(Info + 2); } set { SaveData.SetUInt16(Info + 2, value); } }
            public override ushort Z { get { return SaveData.GetUInt16(Info + 4); } set { SaveData.SetUInt16(Info + 4, value); } }

            // Item Display Stand: 0x2486BC - 0x2488BC (4 bytes * 128 displays) ; Info @ 0xF775 - 0xFB75
            // Arms Display Stand: 0x2488BC - 0x24893C (4 bytes * 32 displays) ; Info @ 0xFB75 - 0xFC75
            // Food Display: 0x24893C - 0x2489BC (4 bytes * 32 displays) ; Info @ 0xFC75 - 0xFD75
            // Decorative Food: 0x2489BC - 0x248A3C (4 bytes * 32 displays) ; Info @ 0xFD75 - 0xFE75
            // Pet Bowl: 0x248A3C - 0x248ABC (4 bytes * 32 displays) ; Info @ 0xFE75 - 0xFF75? see also 0x8AC1, 0xD3BBD
            // Price Tags: 0x24A8BC - 0x24A93C (4 bytes * 32 displays) ; Info @ 0xD39C7
            // Drinks: 0x24C034 - 0x24C0B4 (4 bytes * 32 displays) ; Info @ 0x103FD

            // "Tablewares Info": 0xFC75, 8 bytes each ; this counts crockery.
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
        }
        public class Signpost(StageData saveData, int address) : BlockEntity(saveData)
        {
            public const int START_ADDRESS = 0x148FE;
            public const int LENGTH = 0x86; // 134
            public const int MAXIMUM = 20;

            public readonly int Address = address;

            public string Text { get { return SaveData.GetString(Address + 6, 127); } set { SaveData.SetString(Address + 6, value, 127); SaveData.SetByte(Address + LENGTH - 1, 0); } }

            public override ushort X { get { return SaveData.GetUInt16(Address); } set { SaveData.SetUInt16(Address, value); } }
            public override ushort Z { get { return SaveData.GetUInt16(Address + 2); } set { SaveData.SetUInt16(Address + 2, value); } }
            public override byte Y { get { return SaveData.GetByte(Address + 4); } set { SaveData.SetByte(Address + 4, value); } }
        }
        public class SalutationStation(StageData saveData, int address) : BlockEntity(saveData)
        {
            public const int START_ADDRESS = 0x16B36;
            public const int LENGTH = 0xE4;
            public const int MAXIMUM = 64;

            public readonly int Address = address;

            public string Text { get { return SaveData.GetString(Address + 7, 0xD8); } set { SaveData.SetString(Address + 7, value, 0xD8); SaveData.SetByte(Address + 7 + 0xD9, 0); } }

            public override ushort X { get { return SaveData.GetUInt16(Address); } set { SaveData.SetUInt16(Address, value); } }
            public override byte Y { get { return SaveData.GetByte(Address + 2); } set { SaveData.SetUInt16(Address + 2, value); } }
            public override ushort Z { get { return SaveData.GetUInt16(Address + 4); } set { SaveData.SetUInt16(Address + 4, value); } }

            public bool Active { get { return SaveData.GetByte(Address + 6) == 1; } set { SaveData.SetByte(Address + 6, (byte)(value ? 1 : 0)); } }

            public ushort ResidentID { get { return SaveData.GetUInt16(Address + 0xE0); } set { SaveData.SetUInt16(Address + 0xE0, value); SaveData.SetUInt16(Address + 0xE2, value); } } // Duplicated. Why?
        }
        public class MagnetBlock(StageData saveData, int address) : BlockEntity(saveData)
        {
            public const int START_ADDRESS = 0x1310D;
            public const int Length = 9;
            public const int Maximum = 256;

            public readonly int Address = address;

            public override ushort X { get { return SaveData.GetUInt16(Address); } set { SaveData.SetUInt16(Address, value); } }
            public override byte Y { get { return SaveData.GetByte(Address + 2); } set { SaveData.SetUInt16(Address + 2, value); } }
            public override ushort Z { get { return SaveData.GetUInt16(Address + 4); } set { SaveData.SetUInt16(Address + 4, value); } }

            public ushort Camouflage { get { return (ushort)SaveData.GetNumberBitwise(Address + 7, 0, 13); } set { SaveData.SetNumberBitwise(Address + 7, 0, 13, value); } }

            public bool Active { get { return SaveData.GetByte(Address + 6) == 1; } set { SaveData.SetByte(Address + 6, (byte)(value ? 1 : 0)); } }

            // NOTE: If last bit of Address + 8 is 0, and Camouflage > 0, block is invisible
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

            public bool Active { get { return SaveData.GetByte(Address + 6) == 1; } set { SaveData.SetByte(Address + 6, (byte)(value ? 1 : 0)); } }

            public DQB2Crop Crop { get { return (DQB2Crop)SaveData.GetByte(Address + 5); } set { SaveData.SetByte(Address + 5, (byte)value); } }

            // See also 0x2CA3E
            // I think fields are 0xC bytes long
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

            public bool Active { get { return SaveData.GetByte(Address + 6) == 1; } set { SaveData.SetByte(Address + 6, (byte)(value ? 1 : 0)); } }

            public DQB2Color Color { get { return (DQB2Color)SaveData.GetNumberBitwise(Address + 7, 0, 15); } set { SaveData.SetNumberBitwise(Address + 7, 0, 15, (ushort)value); } }
        }
        public class Instrument(StageData saveData, int address) : BlockEntity(saveData)
        {
            public const int START_ADDRESS = 0x289A0; // Start of overworld stuff = 0x287E8
            public const int LENGTH = 8;
            public const int MAXIMUM = 100; // 101?

            public readonly int Address = address;

            public override ushort X { get => SaveData.GetUInt16(Address); set => SaveData.SetUInt16(Address, value); }
            public override ushort Z { get => SaveData.GetUInt16(Address + 2); set => SaveData.SetUInt16(Address + 2, value); }
            public override byte Y { get => SaveData.GetByte(Address + 4); set => SaveData.SetByte(Address + 4, value); }

            public byte Song { get => SaveData.GetByte(Address + 5); set => SaveData.SetByte(Address + 5, value); }

            public bool IsPlaying() => Song > 0;
            public bool Exists() => !SaveData.GetBytes(Address, LENGTH).ToArray().All(b => b == 0);
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

        public Scarecrow GetScarecrow(int index)
        {
            if (index < 0 || index >= Signpost.MAXIMUM)
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

        public ItemDisplay GetItemDisplay(int index)
        {
            return new ItemDisplay(this, 0xF775 + 8 * index, 0x2486BC + 4 * index);
        }
        public ItemDisplay GetEquipmentDisplay(int index)
        {
            return new ItemDisplay(this, 0xFB75 + 8 * index, 0x2488BC + 4 * index);
        }
        public ItemDisplay GetFoodDisplay(int index)
        {
            return new ItemDisplay(this, 0xFC75 + 8 * index, 0x24893C + 4 * index);
        }
        public ItemDisplay GetDecorativeFood(int index)
        {
            return new ItemDisplay(this, 0xFD75 + 8 * index, 0x2489BC + 4 * index);
        }
        public ItemDisplay GetPetBowl(int index)
        {
            return new ItemDisplay(this, 0xFE75 + 8 * index, 0x248A3C + 4 * index);
        }
        public ItemDisplay GetBeverage(int index)
        {
            return new ItemDisplay(this, 0x103FD + 8 * index, 0x24C034 + 4 * index);
        }
        public ItemDisplay GetPriceTag(int index)
        {
            return new ItemDisplay(this, 0xD39C7 + 8 * index, 0x24A8BC + 4 * index);
        }
        #endregion

        #region Other Classes
        public class Blueprint(StageData saveData, int address)
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
        public class Room(StageData saveData, int address)
        {
            public const int START_ADDRESS = 0x10;
            public const int LENGTH = 0x150;
            public const int MAXIMUM = 100; // Maybe?

            public readonly StageData SaveData = saveData;
            public readonly int Address = address;

            public ushort X { get => SaveData.GetUInt16(Address + 0x14); set => SaveData.SetUInt16(Address + 0x14, value); }
            public ushort Z { get => SaveData.GetUInt16(Address + 0x16); set => SaveData.SetUInt16(Address + 0x16, value); }
            public byte Y { get => SaveData.GetByte(Address + 0x1C); set => SaveData.SetByte(Address + 0x1C, value); }

            public Vector3I GetPosition() => new(X, Y, Z);

            public byte Width { get => SaveData.GetByte(Address + 0x1D); set => SaveData.SetByte(Address + 0x1D, value); }
            public byte Depth { get => SaveData.GetByte(Address + 0x1F); set => SaveData.SetByte(Address + 0x1F, value); }

            public ushort RoomType { get => SaveData.GetUInt16(Address + 2); set => SaveData.SetUInt16(Address + 2, value); }

            public uint Fanciness { get => SaveData.GetUInt32(Address + 4); set => SaveData.SetUInt32(Address + 4, value); }

            public ushort Cuteness { get => SaveData.GetUInt16(Address + 0x8); set => SaveData.SetUInt16(Address + 0x8, value); }
            public ushort Coolness { get => SaveData.GetUInt16(Address + 0xA); set => SaveData.SetUInt16(Address + 0xA, value); }
            public ushort Naturalness { get => SaveData.GetUInt16(Address + 0xC); set => SaveData.SetUInt16(Address + 0xC, value); }
            public ushort Flamboyantness { get => SaveData.GetUInt16(Address + 0xE); set => SaveData.SetUInt16(Address + 0xE, value); }
            public ushort Cheekiness { get => SaveData.GetUInt16(Address + 0x10); set => SaveData.SetUInt16(Address + 0x10, value); }
            public ushort Normalness { get => SaveData.GetUInt16(Address + 0x12); set => SaveData.SetUInt16(Address + 0x12, value); }
        }
        public class Field(StageData saveData, int address)
        {
            public const int START_ADDRESS = 0x2CA9C;
            public const int LENGTH = 0x12;
            public const int MAXIMUM = 64;

            public readonly StageData SaveData = saveData;
            public readonly int Address = address;

            public ushort X { get { return SaveData.GetUInt16(Address); } set { SaveData.SetUInt16(Address, value); } } // Northernmost corner
            public ushort Z { get { return SaveData.GetUInt16(Address + 2); } set { SaveData.SetUInt16(Address + 2, value); } } // Westernmost corner

            public ushort ScarecrowX { get { return SaveData.GetUInt16(Address + 4); } set { SaveData.SetUInt16(Address + 4, value); } }
            public ushort ScarecrowZ { get { return SaveData.GetUInt16(Address + 6); } set { SaveData.SetUInt16(Address + 6, value); } }
            public byte ScarecrowY { get { return SaveData.GetByte(Address + 8); } set { SaveData.SetByte(Address + 8, value); } }

            public byte Width { get { return SaveData.GetByte(Address + 9); } set { SaveData.SetByte(Address + 9, value); } } // East-West (X)
            public byte Depth { get { return SaveData.GetByte(Address + 9); } set { SaveData.SetByte(Address + 9, value); } } // North-South (Z)

            public ushort Crop { get { return SaveData.GetUInt16(Address + 0xC); } set { SaveData.SetUInt16(Address + 0xC, value); } }
        }

        public Room GetRoom(int index)
        {
            if (index < 0 || index >= Room.MAXIMUM)
                throw new IndexOutOfRangeException();

            return new Room(this, Room.START_ADDRESS + index * Room.LENGTH);
        }
        public IEnumerable<Room> GetRooms(int index = 0, int count = Room.MAXIMUM)
        {
            if (index < 0 || index >= Room.MAXIMUM)
                throw new IndexOutOfRangeException();

            for (int i = 0; i < count && i + index < Room.MAXIMUM; i++)
                yield return GetRoom(i + index);
        }

        // Dropped items at 0x24334C
        // ID: +0
        // Count: +2
        // X: +4
        // Y: +8
        // Z: +12
        // Length is like 0x15
        #endregion
    }
}