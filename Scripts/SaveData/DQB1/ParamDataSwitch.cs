using System;
using Godot;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Linq;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace EyeOfRubiss
{
    public class ParamDataSwitch : ParamData
    {
        public const int HOTBAR_ADDRESS = 0x45020;
        public const int EQUIPMENT_ADDRESS = 0x4505C;
        public const int BAG_ADDRESS = 0x4509C;
        public const int EQUIPMENT_BAG_ADDRESS = 0x4539C;

        public override uint WorldChecksum { get { return GetUInt32(0x8); } set { SetUInt32(0x8, value); } }

        public override byte StageID { get => GetByte(0x1E37A); set => SetByte(0x1E37A, value); }

        public override ushort PlayerMaximumHP { get => GetUInt16(0x1E366); set => SetUInt16(0x1E366, value); }
        public override ushort PlayerHP { get => GetUInt16(0x1E368); set => SetUInt16(0x1E368, value); }

        public override ushort PlayerMaxSatiety { get => GetUInt16(0x1E36A); set => SetUInt16(0x1E36A, value); }
        public override ushort PlayerSatiety { get => GetUInt16(0x1E36C); set => SetUInt16(0x1E36C, value); }

        public override ushort PlayerStrength { get => GetUInt16(0x1E36E); set => SetUInt16(0x1E36E, value); }
        public override ushort PlayerResilience { get => GetUInt16(0x1E370); set => SetUInt16(0x1E370, value); }

        public override float PlayerPositionX { get => GetSingle(0x1E350); set => SetSingle(0x1E350, value); }
        public override float PlayerPositionY { get => GetSingle(0x1E354); set => SetSingle(0x1E354, value); }
        public override float PlayerPositionZ { get => GetSingle(0x1E358); set => SetSingle(0x1E358, value); }

        public override float PlayerRotation { get => GetSingle(0x1E360); set => SetSingle(0x1E360, value); }

        public override bool YoshiExists { get => GetByte(0x31536) != 0; }

        public override float YoshiPositionX { get => GetSingle(0x31520); set => SetSingle(0x31520, value); }
        public override float YoshiPositionY { get => GetSingle(0x31524); set => SetSingle(0x31524, value); }
        public override float YoshiPositionZ { get => GetSingle(0x31528); set => SetSingle(0x31528, value); }

        public override float YoshiRotation { get => GetSingle(0x31530); set => SetSingle(0x31530, value); }

        public override float Time { get => GetSingle(0x1F518); set => SetSingle(0x1F518, value); }
        public override ushort Weather { get => GetUInt16(0x1F51C); set => SetUInt16(0x1F51C, value); }

        public override uint Score { get => GetUInt32(0x2604); set { SetUInt32(0x2604, value); SetUInt32(0x2608, value); } }
        
        public override ushort ElapsedDays { get => GetUInt16(0x1E364); set => SetUInt16(0x1E364, value); }
        public override ushort CraftCount { get => GetUInt16(0x1E378); set => SetUInt16(0x1E378, value); }
        public override byte DeadCount { get => GetByte(0x1E580); set => SetByte(0x1E580, value); }

        public override bool AcquireAttackSkill { get => GetByte(0x1F50D) != 0; set => SetByte(0x1F50D, (byte)(value ? 1 : 0)); }

        public static bool TryLoad(string path, out ParamDataSwitch result)
        {
            result = null;
            ParamDataSwitch paramData = new();
            if (paramData._TryLoad(path, HEADER_LENGTH))
            {
                result = paramData;
                return true;
            }
            else return false;
        }

        #region Inventory
        public override InventoryItem GetHotbarItem(int index)
        {
            if (index < 0 || index >= HOTBAR_ITEM_COUNT)
                throw new IndexOutOfRangeException();

            return new InventoryItem(this, HOTBAR_ADDRESS + index * InventoryItem.LENGTH);
        }
        public override InventoryItem GetBagItem(int index)
        {
            if (index < 0 || index >= BAG_ITEM_COUNT)
                throw new IndexOutOfRangeException();

            return new InventoryItem(this, BAG_ADDRESS + index * InventoryItem.LENGTH);
        }
        public override InventoryItem GetEquipment(int index)
        {
            if (index < 0 || index >= EQUIPMENT_COUNT)
                throw new IndexOutOfRangeException();

            return new InventoryItem(this, EQUIPMENT_ADDRESS + index * InventoryItem.LENGTH);
        }
        public override InventoryItem GetBagEquipment(int index)
        {
            if (index < 0 || index >= EQUIPMENT_BAG_COUNT)
                throw new IndexOutOfRangeException();

            return new InventoryItem(this, EQUIPMENT_BAG_ADDRESS + index * InventoryItem.LENGTH);
        }
        #endregion
        
        #region Residents
        new public class Resident(ParamDataSwitch saveData, int index) : ParamData.Resident(saveData, index)
        {
            public const int START_ADDRESS = 0x2620;

            public override int GetAddress() => START_ADDRESS + Index * LENGTH;

            public override float PositionX { get => SaveData.GetSingle(GetAddress()); set => SaveData.SetSingle(GetAddress(), value); }
            public override float PositionY { get => SaveData.GetSingle(GetAddress() + 4); set => SaveData.SetSingle(GetAddress() + 4, value); }
            public override float PositionZ { get => SaveData.GetSingle(GetAddress() + 8); set => SaveData.SetSingle(GetAddress() + 8, value); }

            public override float Rotation { get => SaveData.GetSingle(GetAddress() + 0x10); set => SaveData.SetSingle(GetAddress() + 0x10, value); }

            public override ushort ResidentID { get => SaveData.GetUInt16(GetAddress() + 0x14); set => SaveData.SetUInt16(GetAddress() + 0x14, value); }
            public override ushort Type { get => SaveData.GetUInt16(GetAddress() + 0x16); set => SaveData.SetUInt16(GetAddress() + 0x16, value); }

            public override ushort HP { get => SaveData.GetUInt16(GetAddress() + 0x18); set => SaveData.SetUInt16(GetAddress() + 0x18, value); }

            public override byte State1 { get => SaveData.GetByte(GetAddress() + 0x1A); set => SaveData.SetByte(GetAddress() + 0x1A, value); }
            public override byte State2 { get => SaveData.GetByte(GetAddress() + 0x1B); set => SaveData.SetByte(GetAddress() + 0x1B, value); }
        }

        public override Resident GetResident(int index)
        {
            if (index < 0 || index >= ParamData.Resident.MAXIMUM)
                throw new IndexOutOfRangeException();

            return new Resident(this, index);
        }
        #endregion

        #region Block entities
        new public class ColossalCoffer(ParamDataSwitch saveData) : ParamData.ColossalCoffer(saveData)
        {
            public override ushort X { get { return SaveData.GetUInt16(ADDRESS); } set { SaveData.SetUInt16(ADDRESS, value); } }
            public override ushort Z { get { return SaveData.GetUInt16(ADDRESS + 2); } set { SaveData.SetUInt16(ADDRESS + 2, value); } }
            public override byte Y { get { return SaveData.GetByte(ADDRESS + 4); } set { SaveData.SetByte(ADDRESS + 4, value); } }

            public override bool Enabled { get { return SaveData.GetByte(ADDRESS + 5) == 1; } set { SaveData.SetByte(ADDRESS + 5, (byte)(value ? 1 : 0)); } }
            // bytes 7 + 8 are always 01 00 (?)
        }
        public override ColossalCoffer GetColossalCoffer()
        {
            return new ColossalCoffer(this);
        }
        
        new public class Storage(ParamDataSwitch saveData, int metadataAddress, int contentsAddress) : ParamData.Storage(saveData, metadataAddress, contentsAddress)
        {
            public const int START_ADDRESS_CONTENTS = 0x4549C;

            public override ushort X { get { return SaveData.GetUInt16(MetadataAddress); } set { SaveData.SetUInt16(MetadataAddress, value); } }
            public override ushort Z { get { return SaveData.GetUInt16(MetadataAddress + 2); } set { SaveData.SetUInt16(MetadataAddress + 2, value); } }
            public override byte Y { get { return SaveData.GetByte(MetadataAddress + 4); } set { SaveData.SetByte(MetadataAddress + 4, value); } }

            public override bool Enabled { get { return SaveData.GetByte(MetadataAddress + 5) == 1; } set { SaveData.SetByte(MetadataAddress + 5, (byte)(value ? 1 : 0)); } }
            // bytes 7 + 8 are always 02 00 (?)

            public override InventoryItem GetItem(int index)
            {
                if (index < 0 || index >= ITEM_COUNT)
                    throw new IndexOutOfRangeException();

                return new(SaveData, ContentsAddress + index * InventoryItem.LENGTH);
            }
            
            public override void Clear()
            {
                SaveData.Fill(0, MetadataAddress, LENGTH_METADATA);
                SaveData.Fill(0, ContentsAddress, LENGTH_CONTENTS);
            }
        }
        public override Storage GetStorage(int index)
        {
            if (index < 0 || index >= ParamData.Storage.MAXIMUM)
                throw new IndexOutOfRangeException();
            
            return new Storage(this,
                ParamData.Storage.START_ADDRESS_METADATA + ParamData.Storage.LENGTH_METADATA * index,
                Storage.START_ADDRESS_CONTENTS + ParamData.Storage.LENGTH_CONTENTS * index);
        }
        
        new public class ItemDisplay(ParamDataSwitch saveData, int metadataAddress, int contentsAddress) : ParamData.ItemDisplay(saveData, metadataAddress, contentsAddress)
        {
            public const int START_ADDRESS_ITEM_DISPLAY_CONTENTS = 0x4649C;
            public const int START_ADDRESS_EQUIPMENT_DISPLAY_CONTENTS = 0x4651C;

            public override ushort X { get { return SaveData.GetUInt16(MetadataAddress); } set { SaveData.SetUInt16(MetadataAddress, value); } }
            public override ushort Z { get { return SaveData.GetUInt16(MetadataAddress + 2); } set { SaveData.SetUInt16(MetadataAddress + 2, value); } }
            public override byte Y { get { return SaveData.GetByte(MetadataAddress + 4); } set { SaveData.SetByte(MetadataAddress + 4, value); } }

            public override bool Enabled { get { return SaveData.GetByte(MetadataAddress + 5) == 1; } set { SaveData.SetByte(MetadataAddress + 5, (byte)(value ? 1 : 0)); } }
            // bytes 7 + 8 are always 03 00 if item display, 04 00 if equip display

            public override InventoryItem Item => new(SaveData, ContentsAddress);
        }
        public override ItemDisplay GetItemDisplayGeneric(int index)
        {
            if (index < 0 || index >= ParamData.ItemDisplay.ITEM_DISPLAY_MAXIMUM)
                throw new IndexOutOfRangeException();
            
            return new ItemDisplay(this,
                ParamData.ItemDisplay.START_ADDRESS_ITEM_DISPLAY_METADATA + ParamData.ItemDisplay.LENGTH_METADATA * index,
                ItemDisplay.START_ADDRESS_ITEM_DISPLAY_CONTENTS + ParamData.ItemDisplay.LENGTH_CONTENTS * index);
        }
        public override ItemDisplay GetEquipmentDisplay(int index)
        {
            if (index < 0 || index >= ParamData.ItemDisplay.EQUIPMENT_DISPLAY_MAXIMUM)
                throw new IndexOutOfRangeException();
            
            return new ItemDisplay(this,
                ParamData.ItemDisplay.START_ADDRESS_EQUIPMENT_DISPLAY_METADATA + ParamData.ItemDisplay.LENGTH_METADATA * index,
                ItemDisplay.START_ADDRESS_EQUIPMENT_DISPLAY_CONTENTS + ParamData.ItemDisplay.LENGTH_CONTENTS * index);
        }

        new public class Signpost(ParamDataSwitch saveData, int address) : ParamData.Signpost(saveData, address)
        {
            public override ushort X { get { return SaveData.GetUInt16(Address); } set { SaveData.SetUInt16(Address, value); } }
            public override ushort Z { get { return SaveData.GetUInt16(Address + 2); } set { SaveData.SetUInt16(Address + 2, value); } }
            public override byte Y { get { return SaveData.GetByte(Address + 4); } set { SaveData.SetByte(Address + 4, value); } }

            public override string Text { get { return SaveData.GetString(Address + 8, 0x80); } set { SaveData.SetString(Address + 8, value, 0x80); } }

            public override bool Enabled { get { return SaveData.GetByte(Address + 5) == 1; } set { SaveData.SetByte(Address + 5, (byte)(value ? 1 : 0)); } }
            public override bool Written { get { return SaveData.GetByte(Address + 6) == 1; } set { SaveData.SetByte(Address + 6, (byte)(value ? 1 : 0)); } }

            public override byte Language { get { return SaveData.GetByte(Address + 7); } set { SaveData.SetByte(Address + 7, value); } } // 3 is English I think, 0 might be japanese?
        }
        public override Signpost GetSignpost(int index)
        {
            if (index < 0 || index >= ParamData.Signpost.MAXIMUM)
                throw new IndexOutOfRangeException();
            
            return new Signpost(this, ParamData.Signpost.START_ADDRESS + ParamData.Signpost.LENGTH * index);
        }
        
        new public class Teleportal(ParamDataSwitch saveData, int address) : ParamData.Teleportal(saveData, address)
        {
            public override ushort X { get { return SaveData.GetUInt16(Address); } set { SaveData.SetUInt16(Address, value); } }
            public override ushort Z { get { return SaveData.GetUInt16(Address + 2); } set { SaveData.SetUInt16(Address + 2, value); } }
            public override byte Y { get { return SaveData.GetByte(Address + 4); } set { SaveData.SetByte(Address + 4, value); } }
            
            public override bool Enabled { get { return SaveData.GetByte(Address + 5) == 1; } set { SaveData.SetByte(Address + 5, (byte)(value ? 1 : 0)); } }
        }
        public override Teleportal GetTeleportal(int index)
        {
            if (index < 0 || index >= ParamData.Teleportal.MAXIMUM)
                throw new IndexOutOfRangeException();
            
            return new Teleportal(this, ParamData.Teleportal.START_ADDRESS + ParamData.Teleportal.LENGTH * index);
        }
        
        new public class Naviglobe(ParamDataSwitch saveData, int address) : ParamData.Naviglobe(saveData, address)
        {
            public override ushort X { get { return SaveData.GetUInt16(Address); } set { SaveData.SetUInt16(Address, value); } }
            public override ushort Z { get { return SaveData.GetUInt16(Address + 2); } set { SaveData.SetUInt16(Address + 2, value); } }
            public override byte Y { get { return SaveData.GetByte(Address + 4); } set { SaveData.SetByte(Address + 4, value); } }
            
            public override bool Enabled { get { return SaveData.GetByte(Address + 5) == 1; } set { SaveData.SetByte(Address + 5, (byte)(value ? 1 : 0)); } }
        }
        public override Naviglobe GetNaviglobe(int index)
        {
            if (index < 0 || index >= ParamData.Naviglobe.MAXIMUM)
                throw new IndexOutOfRangeException();
            
            return new Naviglobe(this, ParamData.Naviglobe.START_ADDRESS + ParamData.Naviglobe.LENGTH * index);
        }
        
        new public class SharingStone(ParamDataSwitch saveData) : ParamData.SharingStone(saveData)
        {
            public const int ADDRESS = 0x12078;
            public const int LENGTH = 0x134;

            public override ushort X { get { return SaveData.GetUInt16(ADDRESS); } set { SaveData.SetUInt16(ADDRESS, value); } }
            public override ushort Z { get { return SaveData.GetUInt16(ADDRESS + 2); } set { SaveData.SetUInt16(ADDRESS + 2, value); } }
            public override byte Y { get { return SaveData.GetByte(ADDRESS + 4); } set { SaveData.SetByte(ADDRESS + 4, value); } }

            public override bool Enabled { get { return SaveData.GetBit(ADDRESS + 6, 0); } set { SaveData.SetBit(ADDRESS + 6, 0, value); } }

            public override void Clear()
            {
                SaveData.Fill(0, ADDRESS, LENGTH);
            }
        }
        public override SharingStone GetSharingStone()
        {
            return new SharingStone(this);
        }
        
        new public class SummoningStone(ParamDataSwitch saveData, int address) : ParamData.SummoningStone(saveData, address)
        {
            public const int START_ADDRESS = 0x121AC;
            public const int LENGTH = 0x138;

            public override ushort X { get { return SaveData.GetUInt16(Address); } set { SaveData.SetUInt16(Address, value); } }
            public override ushort Z { get { return SaveData.GetUInt16(Address + 2); } set { SaveData.SetUInt16(Address + 2, value); } }
            public override byte Y { get { return SaveData.GetByte(Address + 4); } set { SaveData.SetByte(Address + 4, value); } }

            public override bool Enabled { get { return SaveData.GetBit(Address + 7, 0); } set { SaveData.SetBit(Address + 7, 0, value); } }

            public override void Clear()
            {
                SaveData.Fill(0, Address, LENGTH);
            }
        }
        public override SummoningStone GetSummoningStone(int index)
        {
            if (index < 0 || index >= ParamData.SummoningStone.MAXIMUM)
                throw new IndexOutOfRangeException();
            
            return new SummoningStone(this, SummoningStone.START_ADDRESS + SummoningStone.LENGTH * index);
        }
        
        new public class AncientTeleportal(ParamDataSwitch saveData) : ParamData.AncientTeleportal(saveData)
        {
            public override ushort X { get { return SaveData.GetUInt16(ADDRESS); } set { SaveData.SetUInt16(ADDRESS, value); } }
            public override ushort Z { get { return SaveData.GetUInt16(ADDRESS + 2); } set { SaveData.SetUInt16(ADDRESS + 2, value); } }
            public override byte Y { get { return SaveData.GetByte(ADDRESS + 4); } set { SaveData.SetByte(ADDRESS + 4, value); } }

            public override bool Enabled { get { return SaveData.GetByte(ADDRESS + 5) == 1; } set { SaveData.SetByte(ADDRESS + 5, (byte)(value ? 1 : 0)); } }
        }
        public override AncientTeleportal GetAncientTeleportal()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}