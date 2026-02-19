using System;
using Godot;

namespace EyeOfRubiss
{
    public class Blueprint(SaveData saveData, int address)
    {
        public const int LENGTH = 0x30008;

        public readonly SaveData SaveData = saveData;
        public readonly int Address = address;

        public Span<byte> GetBytes() => SaveData.GetBytes(Address, LENGTH);

        public ushort SizeX { get { return SaveData.GetUInt16(Address + 0x30000); } set { SaveData.SetUInt16(Address + 0x30000, value); } }
        public ushort SizeY { get { return SaveData.GetUInt16(Address + 0x30002); } set { SaveData.SetUInt16(Address + 0x30002, value); } }
        public ushort SizeZ { get { return SaveData.GetUInt16(Address + 0x30004); } set { SaveData.SetUInt16(Address + 0x30004, value); } }

        public bool Exists { get { return SaveData.GetByte(Address + 0x30006) == 1; } set { SaveData.SetByte(Address + 0x30006, (byte)(value ? 1 : 0)); } }

        public void CopyTo(Blueprint destination)
        {
            GetBytes().CopyTo(destination.GetBytes());
        }

        public BlueprintBlockInstance GetBlock(Vector3I position)
        {
            if (position.X < 0 || position.X >= SizeX || position.Y < 0 || position.Y >= SizeY || position.Z < 0 || position.Z >= SizeZ)
                return null;
            
            return new BlueprintBlockInstance(SaveData, (position.Z * SizeY * SizeX + position.X * SizeY + position.Y) * BlueprintBlockInstance.LENGTH);
        }
        public class BlueprintBlockInstance(SaveData saveData, int address)
        {
            public const int LENGTH = 6;

            public readonly SaveData SaveData = saveData;
            public readonly int Address = address;

            public ushort PropID { get { return SaveData.GetUInt16(Address); } set { SaveData.SetUInt16(Address, value); } }
            public ushort BlockID { get { return SaveData.GetUInt16(Address + 2); } set { SaveData.SetUInt16(Address + 2, value); } }
            public byte Direction { get { return SaveData.GetByte(Address + 4); } set { SaveData.SetByte(Address + 4, value); } }
        }
    }
}