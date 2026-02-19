using System;
using Godot;
using System.Collections.Generic;

namespace EyeOfRubiss
{
    public class WorldData : SaveData
    {
        public const int HEADER_LENGTH = 0;

        public const int CHUNK_SIZE = 32;

        public const int WORLD_SIZE_CHUNKS = 44;
        public const int WORLD_SIZE_BLOCKS = WORLD_SIZE_CHUNKS * CHUNK_SIZE;
        public const int WORLD_HEIGHT_BLOCKS = 32;

        public static bool TryLoad(string path, out WorldData result)
        {
            result = null;
            WorldData stageData = new();
            if (stageData._TryLoad(path, HEADER_LENGTH))
            {
                result = stageData;
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
        public class Chunk(WorldData saveData, int index)
        {
            public const int START_ADDRESS_METADATA = 0;
            public const int START_ADDRESS_BLOCKDATA = LENGTH_METADATA * MAXIMUM;
            public const int LENGTH_METADATA = 2;
            public const int MAXIMUM = WORLD_SIZE_CHUNKS * WORLD_SIZE_CHUNKS;

            public const int LENGTH_BLOCKDATA = LAYER_LENGTH * WORLD_HEIGHT_BLOCKS;
            public const int LAYER_LENGTH = CHUNK_SIZE * CHUNK_SIZE;

            public WorldData SaveData { get; set; } = saveData;
            public int Index { get; set; } = index;

            public ushort BlockDataIndex { get => SaveData.GetUInt16(START_ADDRESS_METADATA + Index * LENGTH_METADATA); set => SaveData.SetUInt16(START_ADDRESS_METADATA + Index * LENGTH_METADATA, value); }// => SaveData.GetChunkIdByIndex(Index);

            public Vector3I GetOrigin() => ChunkIndexToPosition(Index);

            public bool IsUsed() => BlockDataIndex != ushort.MaxValue;

            public int GetMetadataAddress() => START_ADDRESS_METADATA + LENGTH_METADATA * Index;
            public int GetBlockAddress() => START_ADDRESS_BLOCKDATA + LENGTH_BLOCKDATA * BlockDataIndex;

            public Span<byte> GetData() => SaveData.GetBytes(GetBlockAddress(), LENGTH_BLOCKDATA);

            public byte GetBlock(int tile)
            {
                return IsUsed() ? SaveData.GetByte(GetBlockAddress() + tile) : (byte)0;
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

            public void Clear()
            {
                for (int i = GetBlockAddress(); i < GetBlockAddress() + LENGTH_BLOCKDATA; i++)
                {
                    SaveData.SetByte(i, 0); // TODO replace with fill
                }
            }
        }
        public Chunk GetChunk(int index)
        {
            if (index < 0 || index >= Chunk.MAXIMUM)
                throw new IndexOutOfRangeException();

            return new Chunk(this, index);
        }
        
    }
}