using System;
using Godot;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Linq;
using System.Runtime.CompilerServices;
using EyeOfRubiss.Info.DQB1;

namespace EyeOfRubiss
{
    public class WorldDataPS3 : WorldData
    {
        public static bool TryLoad(string path, out WorldDataPS3 result)
        {
            result = null;
            WorldDataPS3 worldData = new();
            if (worldData._TryLoad(path, HEADER_LENGTH, decompress: false))
            {
                result = worldData;
                worldData.CreateBGPartsPositionDictionary();
                worldData.CreateBGPartsOverlapDictionary();
                return true;
            }
            else return false;
        }
        public override void Save(string path = null)
        {
            path ??= Path;
            Path = path;

            using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write);
            file.StoreBuffer(_Buffer);

            UnsavedChanges = false;
        }

        public override Chunk GetChunk(int index)
        {
            if (index < 0 || index >= WorldData.Chunk.MAXIMUM)
                throw new IndexOutOfRangeException();

            return new Chunk(this, index);
        }

        new public class Chunk(WorldDataPS3 saveData, int index) : WorldData.Chunk(saveData, index)
        {
            public override ushort ChunkID { get => SaveData.GetUInt16(START_ADDRESS_METADATA + Index * LENGTH_METADATA, littleEndian: false); set => SaveData.SetUInt16(START_ADDRESS_METADATA + Index * LENGTH_METADATA, value, littleEndian: false); }
            public override ushort BGPartsCount { get => SaveData.GetUInt16(GetBGPartsAddress() - 4, littleEndian: false); set => SaveData.SetUInt16(GetBGPartsAddress() - 4, value, littleEndian: false); }

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
                get => SaveData.GetByte(GetAddress());
                set
                {
                    SaveData.RemoveBGPartsFromPositionDictionary(this);
                    SaveData.RemoveBGPartsFromOverlapDictionary(this);
                    SaveData.SetByte(GetAddress(), (byte)value);
                    SaveData.AddBGPartsToPositionDictionary(this);
                    SaveData.AddBGPartsToOverlapDictionary(this);
                }
            }
            public override byte X 
            { 
                get => (byte)SaveData.GetNumberBitwise(GetAddress() + 1, 0, 5, littleEndian: false);
                set
                {
                    SaveData.RemoveBGPartsFromPositionDictionary(this);
                    SaveData.RemoveBGPartsFromOverlapDictionary(this);
                    SaveData.SetNumberBitwise(GetAddress() + 1, 0, 5, value, littleEndian: false);
                    SaveData.AddBGPartsToPositionDictionary(this);
                    SaveData.AddBGPartsToOverlapDictionary(this);
                }
            }
            public override byte Y 
            { 
                get => (byte)SaveData.GetNumberBitwise(GetAddress() + 1, 5, 5, littleEndian: false);
                set
                {
                    SaveData.RemoveBGPartsFromPositionDictionary(this);
                    SaveData.RemoveBGPartsFromOverlapDictionary(this);
                    SaveData.SetNumberBitwise(GetAddress() + 1, 5, 5, value, littleEndian: false);
                    SaveData.AddBGPartsToPositionDictionary(this);
                    SaveData.AddBGPartsToOverlapDictionary(this);
                }
            }
            public override byte Z
            { 
                get => (byte)SaveData.GetNumberBitwise(GetAddress() + 2, 2, 5, littleEndian: false);
                set
                {
                    SaveData.RemoveBGPartsFromPositionDictionary(this);
                    SaveData.RemoveBGPartsFromOverlapDictionary(this);
                    SaveData.SetNumberBitwise(GetAddress() + 2, 2, 5, value, littleEndian: false);
                    SaveData.AddBGPartsToPositionDictionary(this);
                    SaveData.AddBGPartsToOverlapDictionary(this);
                }
            }
            public override byte Direction
            {
                get => (byte)SaveData.GetNumberBitwise(GetAddress() + 2, 7, 2, littleEndian: false); 
                set
                {
                    SaveData.RemoveBGPartsFromPositionDictionary(this);
                    SaveData.RemoveBGPartsFromOverlapDictionary(this);
                    SaveData.SetNumberBitwise(GetAddress() + 2, 7, 2, value, littleEndian: false);
                    SaveData.AddBGPartsToPositionDictionary(this);
                    SaveData.AddBGPartsToOverlapDictionary(this);
                }
            }

            public override bool Collision { get => SaveData.GetBit(GetAddress() + 3, 4); set => SaveData.SetBit(GetAddress() + 3, 4, value);  }
            public override bool Unbreakable { get => SaveData.GetBit(GetAddress() + 3, 5); set => SaveData.SetBit(GetAddress() + 3, 5, value);  }
            public override bool Effects { get => SaveData.GetBit(GetAddress() + 3, 6); set => SaveData.SetBit(GetAddress() + 3, 6, value); }
        }
    }
}