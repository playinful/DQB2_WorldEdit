using System;
using Godot;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Linq;
using System.Runtime.CompilerServices;
using EyeOfRubiss.Info.DQB1;

namespace EyeOfRubiss
{
    public class WorldDataSwitch : WorldData
    {
        public static bool TryLoad(string path, out WorldDataSwitch result)
        {
            result = null;
            WorldDataSwitch worldData = new();
            if (worldData._TryLoad(path, HEADER_LENGTH))
            {
                result = worldData;
                worldData.CreateBGPartsPositionDictionary();
                worldData.CreateBGPartsOverlapDictionary();
                return true;
            }
            else return false;
        }

        public override Chunk GetChunk(int index)
        {
            if (index < 0 || index >= Chunk.MAXIMUM)
                throw new IndexOutOfRangeException();

            return new Chunk(this, index);
        }

        new public class Chunk(WorldDataSwitch saveData, int index) : WorldData.Chunk(saveData, index)
        {
            public override ushort ChunkID { get => SaveData.GetUInt16(START_ADDRESS_METADATA + Index * LENGTH_METADATA); set => SaveData.SetUInt16(START_ADDRESS_METADATA + Index * LENGTH_METADATA, value); }// => SaveData.GetChunkIdByIndex(Index);
            public override ushort BGPartsCount { get => SaveData.GetUInt16(GetBGPartsAddress() - 4); set => SaveData.SetUInt16(GetBGPartsAddress() - 4, value); }

            public override BGParts GetBGParts(int index)
            {
                if (!IsUsed())
                    return null;
                
                if (index < 0 || index >= WorldData.BGParts.MAXIMUM)
                    return null;
                
                return new BGParts(SaveData, this, index);
            }
        }
        
        new public class BGParts(WorldData saveData, Chunk chunk, int index) : WorldData.BGParts(saveData, chunk, index)
        {
            public override ushort BGPartsID 
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
            public override byte X 
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
            public override byte Y 
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
            public override byte Z
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
            public override byte Direction
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

            public override bool Collision { get => SaveData.GetBit(GetAddress() + 3, 4); set => SaveData.SetBit(GetAddress() + 3, 4, value);  }
            public override bool Unbreakable { get => SaveData.GetBit(GetAddress() + 3, 3); set => SaveData.SetBit(GetAddress() + 3, 3, value);  }
            public override bool Effects { get => SaveData.GetBit(GetAddress() + 3, 2); set => SaveData.SetBit(GetAddress() + 3, 2, value); }
        }
    }
}