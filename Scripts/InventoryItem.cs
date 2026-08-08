using Godot;
using System;
using EyeOfRubiss.Info;

namespace EyeOfRubiss
{
    public class InventoryItem(SaveData saveData, int address, bool littleEndian = true)
    {
        public const int LENGTH = 4;

        public readonly SaveData SaveData = saveData;
        public readonly int Address = address;
        public readonly bool LittleEndian = littleEndian;

        public ushort ItemID { get => SaveData.GetUInt16(Address, littleEndian: LittleEndian); set => SaveData.SetUInt16(Address, value, littleEndian: LittleEndian); }
        public ushort Count { get => SaveData.GetUInt16(Address + 2, littleEndian: LittleEndian); set => SaveData.SetUInt16(Address + 2, value, littleEndian: LittleEndian); }

        public void Clear()
        {
            SaveData.Fill(0, Address, LENGTH);
        }
    }
}