using System;
using Godot;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Linq;
using System.ComponentModel;

namespace EyeOfRubiss
{
    public class ParamData : SaveData
    {
        public const int HEADER_LENGTH = 0;

        public const int HOTBAR_ADDRESS = 0x45020;
        public const int HOTBAR_ITEM_COUNT = 15;

        public const int EQUIPMENT_ADDRESS = 0x4505C;
        public const int EQUIPMENT_COUNT = 16;

        public const int BAG_ADDRESS = 0x450AC;
        public const int BAG_ITEM_COUNT = 192;

        public const int EQUIPMENT_BAG_ADDRESS = 0x4539C;
        public const int EQUIPMENT_BAG_COUNT = 64;

        public float PlayerPositionX { get => GetSingle(0x1E350); set => SetSingle(0x1E350, value); }
        public float PlayerPositionY { get => GetSingle(0x1E354); set => SetSingle(0x1E354, value); }
        public float PlayerPositionZ { get => GetSingle(0x1E358); set => SetSingle(0x1E358, value); }
        public Vector3 GetPlayerPosition() => new(PlayerPositionX, PlayerPositionY, PlayerPositionZ);

        // public float Time { get => GetSingle(0x1F518); set => SetSingle(0x1F518, value); }

        public static bool TryLoad(string path, out ParamData result)
        {
            result = null;
            ParamData paramData = new();
            if (paramData._TryLoad(path, HEADER_LENGTH))
            {
                result = paramData;
                return true;
            }
            else return false;
        }

        #region Inventory
        public InventoryItem GetHotbarItem(int index)
        {
            if (index < 0 || index >= HOTBAR_ITEM_COUNT)
                throw new IndexOutOfRangeException();

            return new InventoryItem(this, HOTBAR_ADDRESS + index * InventoryItem.LENGTH);
        }
        public IEnumerable<InventoryItem> GetHotbarItems(int index = 0, int count = HOTBAR_ITEM_COUNT)
        {
            if (index < 0 || index >= HOTBAR_ITEM_COUNT)
                throw new IndexOutOfRangeException();

            for (int i = 0; i < count && i + index < HOTBAR_ITEM_COUNT; i++)
                yield return GetHotbarItem(i + index);
        }

        public InventoryItem GetBagItem(int index)
        {
            if (index < 0 || index >= BAG_ITEM_COUNT)
                throw new IndexOutOfRangeException();

            return new InventoryItem(this, BAG_ADDRESS + index * InventoryItem.LENGTH);
        }
        public IEnumerable<InventoryItem> GetBagItems(int index = 0, int count = BAG_ITEM_COUNT)
        {
            if (index < 0 || index >= BAG_ITEM_COUNT)
                throw new IndexOutOfRangeException();

            for (int i = 0; i < count && i + index < BAG_ITEM_COUNT; i++)
                yield return GetBagItem(i + index);
        }

        public Equipment GetEquipment(int index)
        {
            if (index < 0 || index >= EQUIPMENT_COUNT)
                throw new IndexOutOfRangeException();

            return new Equipment(this, EQUIPMENT_ADDRESS + index * Equipment.LENGTH);
        }
        public IEnumerable<Equipment> GetEquipments(int index = 0, int count = EQUIPMENT_COUNT)
        {
            if (index < 0 || index >= EQUIPMENT_COUNT)
                throw new IndexOutOfRangeException();

            for (int i = 0; i < count && i + index < EQUIPMENT_COUNT; i++)
                yield return GetEquipment(i + index);
        }
        
        public Equipment GetBagEquipment(int index)
        {
            if (index < 0 || index >= EQUIPMENT_BAG_COUNT)
                throw new IndexOutOfRangeException();

            return new Equipment(this, EQUIPMENT_BAG_ADDRESS + index * Equipment.LENGTH);
        }
        public IEnumerable<Equipment> GetBagEquipments(int index = 0, int count = EQUIPMENT_BAG_COUNT)
        {
            if (index < 0 || index >= EQUIPMENT_BAG_COUNT)
                throw new IndexOutOfRangeException();

            for (int i = 0; i < count && i + index < EQUIPMENT_BAG_COUNT; i++)
                yield return GetBagEquipment(i + index);
        }

        public class InventoryItem(ParamData saveData, int address)
        {
            public const int LENGTH = 4;

            public readonly ParamData SaveData = saveData;
            public readonly int Address = address;

            public ushort ID { get => SaveData.GetUInt16(Address); set => SaveData.SetUInt16(Address, value); }
            public byte Count { get => SaveData.GetByte(Address + 2); set => SaveData.SetByte(Address + 2, value); }
        }
        public class Equipment(ParamData saveData, int address)
        {
            public const int LENGTH = 4;

            public readonly ParamData SaveData = saveData;
            public readonly int Address = address;

            public ushort ID { get => SaveData.GetUInt16(Address); set => SaveData.SetUInt16(Address, value); }
            public ushort Durability { get => SaveData.GetUInt16(Address + 2); set => SaveData.SetUInt16(Address + 2, value); }
        }
        #endregion
        
        #region Residents
        public Resident GetResident(int index)
        {
            if (index < 0 || index >= Resident.MAXIMUM)
                throw new IndexOutOfRangeException();

            return new Resident(this, index);
        }
        public IEnumerable<Resident> GetResidents(int index = 0, int count = Resident.MAXIMUM)
        {
            if (index < 0 || index >= Resident.MAXIMUM)
                throw new IndexOutOfRangeException();

            for (int i = 0; i < count && i + index < Resident.MAXIMUM; i++)
                yield return GetResident(i + index);   
        }

        public class Resident(ParamData saveData, int index)
        {
            public const int START_ADDRESS = 0x2620;
            public const int LENGTH = 0x40;
            public const int MAXIMUM = 13; // TODO check

            public readonly ParamData SaveData = saveData;
            public readonly int Index = index;

            public int GetAddress() => START_ADDRESS + Index * LENGTH;

            public float PositionX { get => SaveData.GetSingle(GetAddress()); set => SaveData.SetSingle(GetAddress(), value); }
            public float PositionY { get => SaveData.GetSingle(GetAddress() + 4); set => SaveData.SetSingle(GetAddress() + 4, value); }
            public float PositionZ { get => SaveData.GetSingle(GetAddress() + 8); set => SaveData.SetSingle(GetAddress() + 8, value); }

            public Vector3 GetPosition() => new(PositionX, PositionY, PositionZ);

            public ushort ResidentID { get => SaveData.GetUInt16(GetAddress() + 0x14); set => SaveData.SetUInt16(GetAddress() + 0x14, value); }
        }
        #endregion
    }
}